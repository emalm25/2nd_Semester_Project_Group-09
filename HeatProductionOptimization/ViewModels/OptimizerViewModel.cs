using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Text;
using Avalonia.Media;
using CommunityToolkit.Mvvm.Input;
using HeatProductionOptimization.Models;
using HeatProductionOptimization.Services;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;

namespace HeatProductionOptimization.ViewModels;

public class OptimizerViewModel : ViewModelBase, IDisposable
{
    private const double ChartHeight = 120;
    private const int MaxChartPoints = 24;

    private readonly OptimizerService optimizerService;
    private readonly ResultDataManager resultDataManager;
    private readonly ResultDataManagerViewModel resultDataManagerViewModel;
    private readonly AssetManagerViewModel assetManagerViewModel;
    
    private OptimizationResult.ObjectiveType selectedObjective = OptimizationResult.ObjectiveType.Cost;
    private OptimizationResult.SeasonOption selectedSeason = OptimizationResult.SeasonOption.Winter;
    private OptimizationResult.ScenarioOption selectedScenario = OptimizationResult.ScenarioOption.Scenario1;

    private string optimizationSummary = "Current setup: Cost, Winter, Scenario 1";
    private string lastOptimizationMessage = "Ready to optimize.";
    private ISeries[] netProductionCostSeries = Array.Empty<ISeries>();
    private Axis[] netProductionCostXAxes = Array.Empty<Axis>();
    private Axis[] netProductionCostYAxes = Array.Empty<Axis>();
    
    private bool suppressResultSync;
    private OptimizationResult? selectedResult;

    // Data Visualization collections
    public ObservableCollection<OptimizationResult> Results => resultDataManagerViewModel.Results;
    public ObservableCollection<ChartPointViewModel> HeatDemandSeries { get; } = new();
    public ObservableCollection<ChartPointViewModel> HeatProductionSeries { get; } = new();
    public ObservableCollection<ChartPointViewModel> ElectricityPriceSeries { get; } = new();
    public ObservableCollection<ElectricityBalancePointViewModel> ElectricityBalanceSeries { get; } = new();
    public ObservableCollection<MetricCardViewModel> SummaryMetrics { get; } = new();
    public ObservableCollection<UnitSettingRowViewModel> ProductionUnitSettings { get; } = new();
    public ObservableCollection<UnitPriorityRowViewModel> PriorityRanking { get; } = new();

    public OptimizerViewModel(
        OptimizerService optimizerService,
        ResultDataManager resultDataManager,
        ResultDataManagerViewModel resultDataManagerViewModel,
        AssetManagerViewModel assetManagerViewModel)
    {
        this.optimizerService = optimizerService;
        this.resultDataManager = resultDataManager;
        this.resultDataManagerViewModel = resultDataManagerViewModel;
        this.assetManagerViewModel = assetManagerViewModel;

        SetObjectiveCommand = new RelayCommand<string>(SetObjective);
        SetSeasonCommand = new RelayCommand<string>(SetSeason);
        SetScenarioCommand = new RelayCommand<string>(SetScenario);
        OptimizeCommand = new RelayCommand(RunOptimization);

        resultDataManagerViewModel.PropertyChanged += OnResultDataManagerPropertyChanged;
        resultDataManagerViewModel.Results.CollectionChanged += OnResultsCollectionChanged;
        SubscribeToAssetAvailabilityChanges();

        BuildProductionUnitSettings();
        SelectedResult = resultDataManagerViewModel.SelectedResult ?? resultDataManagerViewModel.Results.LastOrDefault();

        UpdateSummaryFromSelection();
        RefreshDashboard();
    }

    public bool IsCostSelected => selectedObjective == OptimizationResult.ObjectiveType.Cost;
    public bool IsCo2Selected => selectedObjective == OptimizationResult.ObjectiveType.CO2;

    public bool IsWinterSelected => selectedSeason == OptimizationResult.SeasonOption.Winter;
    public bool IsSummerSelected => selectedSeason == OptimizationResult.SeasonOption.Summer;

    public bool IsScenario1Selected => selectedScenario == OptimizationResult.ScenarioOption.Scenario1;
    public bool IsScenario2Selected => selectedScenario == OptimizationResult.ScenarioOption.Scenario2;

    public bool HasSelectedResult => SelectedResult != null;

    public string SelectedResultDescription => SelectedResult is null
        ? "Select or create a result to populate the visualization dashboard."
        : $"Objective: {SelectedResult.Minimalize} | Season: {SelectedResult.Season} | Scenario: {SelectedResult.Scenario}";

    public OptimizationResult? SelectedResult
    {
        get => selectedResult;
        set
        {
            if (!SetProperty(ref selectedResult, value))
            {
                return;
            }

            if (!suppressResultSync)
            {
                resultDataManagerViewModel.SelectedResult = value;
            }

            OnPropertyChanged(nameof(HasSelectedResult));
            OnPropertyChanged(nameof(SelectedResultDescription));
            RefreshDashboard();
        }
    }

    public string OptimizationSummary
    {
        get => optimizationSummary;
        private set => SetProperty(ref optimizationSummary, value);
    }

    public string LastOptimizationMessage
    {
        get => lastOptimizationMessage;
        private set => SetProperty(ref lastOptimizationMessage, value);
    }

    public IRelayCommand<string> SetObjectiveCommand { get; }
    public IRelayCommand<string> SetSeasonCommand { get; }
    public IRelayCommand<string> SetScenarioCommand { get; }
    public IRelayCommand OptimizeCommand { get; }

    public ISeries[] NetProductionCostSeries
    {
        get => netProductionCostSeries;
        private set => SetProperty(ref netProductionCostSeries, value);
    }

    public Axis[] NetProductionCostXAxes
    {
        get => netProductionCostXAxes;
        private set => SetProperty(ref netProductionCostXAxes, value);
    }

    public Axis[] NetProductionCostYAxes
    {
        get => netProductionCostYAxes;
        private set => SetProperty(ref netProductionCostYAxes, value);
    }

    private void SetObjective(string? objective)
    {
        if (string.IsNullOrWhiteSpace(objective))
        {
            return;
        }

        selectedObjective = objective.Trim().Equals("CO2", StringComparison.OrdinalIgnoreCase)
            ? OptimizationResult.ObjectiveType.CO2
            : OptimizationResult.ObjectiveType.Cost;

        OnPropertyChanged(nameof(IsCostSelected));
        OnPropertyChanged(nameof(IsCo2Selected));
        UpdateSummaryFromSelection();
    }

    private void SetSeason(string? season)
    {
        if (string.IsNullOrWhiteSpace(season))
        {
            return;
        }

        selectedSeason = season.Trim().Equals("Summer", StringComparison.OrdinalIgnoreCase)
            ? OptimizationResult.SeasonOption.Summer
            : OptimizationResult.SeasonOption.Winter;

        OnPropertyChanged(nameof(IsWinterSelected));
        OnPropertyChanged(nameof(IsSummerSelected));
        UpdateSummaryFromSelection();
    }

    private void SetScenario(string? scenario)
    {
        if (string.IsNullOrWhiteSpace(scenario))
        {
            return;
        }

        selectedScenario = scenario.Trim().Equals("Scenario 2", StringComparison.OrdinalIgnoreCase)
            ? OptimizationResult.ScenarioOption.Scenario2
            : OptimizationResult.ScenarioOption.Scenario1;

        OnPropertyChanged(nameof(IsScenario1Selected));
        OnPropertyChanged(nameof(IsScenario2Selected));
        UpdateSummaryFromSelection();
    }

    private void RunOptimization()
    {
        var result = optimizerService.Optimize(selectedObjective, selectedSeason, selectedScenario);
        resultDataManager.AddResult(result);

        OptimizationSummary =
            $"Selected: {FormatObjective(selectedObjective)}, {selectedSeason}, {FormatScenario(selectedScenario)} | " +
            $"Points: {result.Timeline.Count} | Heat: {result.TotalHeat:F1} MWh | Cost: {result.TotalCost:F1} | CO2: {result.TotalCo2:F1} | Net el.: {result.NetElectricity:F1} MWh";

        LastOptimizationMessage =
            $"{result.StatusMessage}{Environment.NewLine}{BuildDispatchReport(result)}";

        // Refresh visualization for the new result
        SelectedResult = result;
    }

    private void UpdateSummaryFromSelection()
    {
        OptimizationSummary =
            $"Current setup: {FormatObjective(selectedObjective)}, {selectedSeason}, {FormatScenario(selectedScenario)}";
    }

    private static string BuildDispatchReport(OptimizationResult result)
    {
        if (result.Dispatches.Count == 0)
        {
            return "No units were dispatched.";
        }

        var reportBuilder = new StringBuilder();
        reportBuilder.Append("Dispatch: ");

        var rows = result.Dispatches
            .Select(dispatch =>
                $"{dispatch.UnitName}: {dispatch.HeatProduced:F1} MW (Cost {dispatch.Cost:F1}, CO2 {dispatch.Co2:F2})");

        reportBuilder.Append(string.Join(" | ", rows));
        return reportBuilder.ToString();
    }

    private static string FormatObjective(OptimizationResult.ObjectiveType objective)
    {
        return objective == OptimizationResult.ObjectiveType.CO2 ? "CO2" : "Cost";
    }

    private static string FormatScenario(OptimizationResult.ScenarioOption scenario)
    {
        return scenario == OptimizationResult.ScenarioOption.Scenario2 ? "Scenario 2" : "Scenario 1";
    }

    // Data Visualization methods
    private void OnResultDataManagerPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(ResultDataManagerViewModel.SelectedResult))
        {
            return;
        }

        suppressResultSync = true;
        SelectedResult = resultDataManagerViewModel.SelectedResult;
        suppressResultSync = false;
    }

    private void OnResultsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (SelectedResult == null && resultDataManagerViewModel.Results.Count > 0)
        {
            SelectedResult = resultDataManagerViewModel.Results.Last();
        }

        OnPropertyChanged(nameof(SelectedResultDescription));
    }

    private void BuildProductionUnitSettings()
    {
        ProductionUnitSettings.Clear();

        foreach (var unit in assetManagerViewModel.Units)
        {
            ProductionUnitSettings.Add(new UnitSettingRowViewModel
            {
                ShortName = unit.ShortName,
                Name = unit.Name,
                IsActive = unit.IsAvailable,
                MaxHeat = unit.MaxHeat,
                CostPerMWh = (double)unit.ProductionCosts,
                Co2PerMWh = unit.CO2Emissions ?? 0,
                ElectricityPerHeatMWh = GetElectricityPerHeat(unit),
                StatusBrush = unit.IsAvailable ? Brushes.LimeGreen : Brushes.Gray
            });
        }
    }

    private void RefreshDashboard()
    {
        HeatDemandSeries.Clear();
        HeatProductionSeries.Clear();
        ElectricityPriceSeries.Clear();
        ElectricityBalanceSeries.Clear();
        SummaryMetrics.Clear();
        PriorityRanking.Clear();

        if (SelectedResult == null)
        {
            NetProductionCostSeries = Array.Empty<ISeries>();
            NetProductionCostXAxes = Array.Empty<Axis>();
            NetProductionCostYAxes = Array.Empty<Axis>();
            OnPropertyChanged(nameof(SelectedResultDescription));
            return;
        }

        var sampledTimeline = SampleTimeline(SelectedResult.Timeline, MaxChartPoints).ToList();
        BuildSeries(HeatDemandSeries, sampledTimeline.Select(point => (point.Label, point.HeatDemand)), Brushes.IndianRed);
        BuildSeries(HeatProductionSeries, sampledTimeline.Select(point => (point.Label, point.HeatDelivered)), Brushes.SeaGreen);
        BuildSeries(ElectricityPriceSeries, sampledTimeline.Select(point => (point.Label, point.ElectricityPrice)), Brushes.Gold);
        BuildElectricityBalanceSeries(ElectricityBalanceSeries, sampledTimeline);
        BuildNetProductionCostChart();

        var totalProduced = SelectedResult.Dispatches.Sum(dispatch => dispatch.HeatProduced);
        var totalElectricityProduced = SelectedResult.Dispatches.Sum(dispatch => dispatch.ElectricityProduced);
        var totalElectricityConsumed = SelectedResult.Dispatches.Sum(dispatch => dispatch.ElectricityConsumed);
        var averageElectricityPrice = SelectedResult.Timeline.Any()
            ? SelectedResult.Timeline.Average(point => point.ElectricityPrice)
            : 0;

        SummaryMetrics.Add(new MetricCardViewModel("Heat demand", SelectedResult.TotalDemand, "MWh", Brushes.IndianRed));
        SummaryMetrics.Add(new MetricCardViewModel("Heat production", totalProduced, "MWh", Brushes.SeaGreen));
        SummaryMetrics.Add(new MetricCardViewModel("Electricity production", totalElectricityProduced, "MWh", Brushes.Gold));
        SummaryMetrics.Add(new MetricCardViewModel("Electricity consumption", totalElectricityConsumed, "MWh", Brushes.OrangeRed));
        SummaryMetrics.Add(new MetricCardViewModel("Electricity price", averageElectricityPrice, "€/MWh", Brushes.MediumPurple));
        SummaryMetrics.Add(new MetricCardViewModel("Expenses", SelectedResult.TotalCost, "€", Brushes.CornflowerBlue));
        SummaryMetrics.Add(new MetricCardViewModel("Primary energy", SelectedResult.PrimaryEnergyConsumption, "MWh", Brushes.DarkCyan));

        foreach (var row in BuildPriorityRanking(SelectedResult))
        {
            PriorityRanking.Add(row);
        }

        OnPropertyChanged(nameof(SelectedResultDescription));
    }

    private static IEnumerable<ChartSamplePoint> SampleTimeline(IReadOnlyCollection<OptimizationTimePoint> timeline, int maxPoints)
    {
        if (timeline.Count == 0)
        {
            yield break;
        }

        var points = timeline.ToList();
        if (points.Count <= maxPoints)
        {
            foreach (var point in points)
            {
                yield return ToSamplePoint(point);
            }

            yield break;
        }

        var step = Math.Max(1, points.Count / maxPoints);
        for (var index = 0; index < points.Count; index += step)
        {
            yield return ToSamplePoint(points[index]);
        }
    }

    private static ChartSamplePoint ToSamplePoint(OptimizationTimePoint point)
    {
        return new ChartSamplePoint
        {
            Label = point.StartTime.ToString("dd/MM HH:mm"),
            HeatDemand = point.HeatDemand,
            HeatDelivered = point.HeatDelivered,
            ElectricityPrice = point.ElectricityPrice,
            ElectricityProduced = point.ElectricityProduced,
            ElectricityConsumed = point.ElectricityConsumed,
            NetElectricity = point.NetElectricity
        };
    }

    private static void BuildElectricityBalanceSeries(
        ObservableCollection<ElectricityBalancePointViewModel> target,
        IEnumerable<ChartSamplePoint> values)
    {
        var source = values.ToList();
        var maxValue = source
            .SelectMany(item => new[] { item.ElectricityProduced, item.ElectricityConsumed })
            .DefaultIfEmpty(1)
            .Max();

        if (maxValue <= 0)
        {
            maxValue = 1;
        }

        foreach (var item in source)
        {
            var productionHeight = item.ElectricityProduced <= 0 ? 4 : Math.Max(4, item.ElectricityProduced / maxValue * ChartHeight);
            var consumptionHeight = item.ElectricityConsumed <= 0 ? 4 : Math.Max(4, item.ElectricityConsumed / maxValue * ChartHeight);

            target.Add(new ElectricityBalancePointViewModel(
                item.Label,
                item.ElectricityProduced,
                item.ElectricityConsumed,
                productionHeight,
                consumptionHeight));
        }
    }

    private static void BuildSeries(
        ObservableCollection<ChartPointViewModel> target,
        IEnumerable<(string Label, double Value)> values,
        IBrush positiveBrush,
        bool allowNegative = false)
    {
        var source = values.ToList();
        var maxValue = source.Select(item => Math.Abs(item.Value)).DefaultIfEmpty(1).Max();
        if (maxValue <= 0)
        {
            maxValue = 1;
        }

        foreach (var item in source)
        {
            var height = Math.Max(4, Math.Abs(item.Value) / maxValue * ChartHeight);
            var brush = allowNegative && item.Value < 0 ? Brushes.OrangeRed : positiveBrush;
            target.Add(new ChartPointViewModel(item.Label, item.Value, height, brush));
        }
    }

    private static IEnumerable<UnitPriorityRowViewModel> BuildPriorityRanking(OptimizationResult result)
    {
        return result.Dispatches
            .Where(dispatch => dispatch.HeatProduced > 0)
            .Select(dispatch => new UnitPriorityRowViewModel
            {
                UnitName = dispatch.UnitName,
                HeatProduced = dispatch.HeatProduced,
                NetCost = dispatch.Cost,
                NetCostPerMWh = dispatch.Cost / dispatch.HeatProduced,
                Co2 = dispatch.Co2
            })
            .OrderBy(row => row.NetCostPerMWh)
            .ThenBy(row => row.UnitName);
    }

    private static double GetElectricityPerHeat(ProductionUnitItemViewModel unit)
    {
        if (unit.MaxElectricity.HasValue && unit.MaxHeat > 0)
        {
            return unit.MaxElectricity.Value / unit.MaxHeat;
        }

        return 0;
    }

    private void BuildNetProductionCostChart()
    {
        if (SelectedResult == null || SelectedResult.Timeline.Count == 0)
        {
            NetProductionCostSeries = Array.Empty<ISeries>();
            NetProductionCostXAxes = Array.Empty<Axis>();
            NetProductionCostYAxes = Array.Empty<Axis>();
            return;
        }

        var points = SelectedResult.Timeline;
        var labels = points
            .Select(point => point.StartTime.Hour % 6 == 0
                ? point.StartTime.ToString("dd/MM HH:mm")
                : string.Empty)
            .ToArray();
        var priceValues = points.Select(point => point.ElectricityPrice).ToArray();

        var availableUnits = assetManagerViewModel.Units.Where(unit => unit.IsAvailable).ToList();
        var series = new List<ISeries>
        {
            new LineSeries<double>
            {
                Name = "Electricity Price",
                Values = priceValues,
                Stroke = new SolidColorPaint(SKColors.Black) { StrokeThickness = 2 },
                Fill = null,
                GeometrySize = 0
            }
        };

        foreach (var unit in availableUnits)
        {
            var electricityPerHeat = GetElectricityPerHeat(unit);
            var unitValues = points
                .Select(point =>
                {
                    var wasDispatched = point.Dispatches
                        .Any(dispatch => dispatch.UnitName == unit.Name && dispatch.HeatProduced > 0.0001);

                    if (!wasDispatched)
                    {
                        return double.NaN;
                    }

                    return CalculateEffectiveCostPerMWh((double)unit.ProductionCosts, electricityPerHeat, point.ElectricityPrice);
                })
                .ToArray();

            series.Add(new LineSeries<double>
            {
                Name = $"{unit.ShortName} net prod costs",
                Values = unitValues,
                Stroke = new SolidColorPaint(GetUnitColor(unit.ShortName)) { StrokeThickness = 2 },
                Fill = null,
                GeometrySize = 0
            });
        }

        NetProductionCostSeries = series.ToArray();
        NetProductionCostXAxes = new Axis[]
        {
            new Axis
            {
                Labels = labels,
                LabelsRotation = 90,
                TextSize = 10,
                LabelsPaint = new SolidColorPaint(SKColors.White),
                NamePaint = new SolidColorPaint(SKColors.White)
            }
        };
        NetProductionCostYAxes = new Axis[]
        {
            new Axis
            {
                Name = "€/MWh",
                TextSize = 11,
                LabelsPaint = new SolidColorPaint(SKColors.White),
                NamePaint = new SolidColorPaint(SKColors.White)
            }
        };
    }

    private static SKColor GetUnitColor(string shortName)
    {
        return shortName.ToUpperInvariant() switch
        {
            "GB1" => SKColors.Goldenrod,
            "GB2" => SKColors.ForestGreen,
            "GB3" => SKColors.SaddleBrown,
            "OB1" => SKColors.DodgerBlue,
            "GM1" => SKColors.LimeGreen,
            "EB1" => SKColors.MediumPurple,
            _ => SKColors.Gray
        };
    }

    private static double CalculateEffectiveCostPerMWh(double costPerMWh, double electricityPerHeatMWh, double electricityPricePerMWh)
    {
        var electricityRevenuePerMWhHeat = electricityPerHeatMWh > 0 ? electricityPerHeatMWh * electricityPricePerMWh : 0;
        var electricityCostPerMWhHeat = electricityPerHeatMWh < 0 ? -electricityPerHeatMWh * electricityPricePerMWh : 0;
        return costPerMWh - electricityRevenuePerMWhHeat + electricityCostPerMWhHeat;
    }

    private void SubscribeToAssetAvailabilityChanges()
    {
        foreach (var unit in assetManagerViewModel.Units)
        {
            unit.PropertyChanged += OnAssetUnitPropertyChanged;
        }
    }

    private void UnsubscribeFromAssetAvailabilityChanges()
    {
        foreach (var unit in assetManagerViewModel.Units)
        {
            unit.PropertyChanged -= OnAssetUnitPropertyChanged;
        }
    }

    private void OnAssetUnitPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(ProductionUnitItemViewModel.IsAvailable))
        {
            return;
        }

        BuildProductionUnitSettings();
        BuildNetProductionCostChart();
    }

    public void Dispose()
    {
        UnsubscribeFromAssetAvailabilityChanges();
        resultDataManagerViewModel.PropertyChanged -= OnResultDataManagerPropertyChanged;
        resultDataManagerViewModel.Results.CollectionChanged -= OnResultsCollectionChanged;
    }
}

public class ChartPointViewModel
{
    public string Label { get; }
    public double Value { get; }
    public string ValueText => Value.ToString("F1");
    public double BarHeight { get; }
    public IBrush BarBrush { get; }

    public ChartPointViewModel(string label, double value, double barHeight, IBrush barBrush)
    {
        Label = label;
        Value = value;
        BarHeight = barHeight;
        BarBrush = barBrush;
    }
}

public class MetricCardViewModel
{
    public string Title { get; }
    public double Value { get; }
    public string Unit { get; }
    public IBrush AccentBrush { get; }
    public string ValueText => $"{Value:F1} {Unit}";

    public MetricCardViewModel(string title, double value, string unit, IBrush accentBrush)
    {
        Title = title;
        Value = value;
        Unit = unit;
        AccentBrush = accentBrush;
    }
}

public class UnitSettingRowViewModel
{
    public string ShortName { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public double MaxHeat { get; set; }
    public double CostPerMWh { get; set; }
    public double Co2PerMWh { get; set; }
    public double ElectricityPerHeatMWh { get; set; }
    public IBrush StatusBrush { get; set; } = Brushes.Gray;
}

public class UnitPriorityRowViewModel
{
    public string UnitName { get; set; } = string.Empty;
    public double HeatProduced { get; set; }
    public double NetCost { get; set; }
    public double NetCostPerMWh { get; set; }
    public double Co2 { get; set; }
}

internal class ChartSamplePoint
{
    public string Label { get; set; } = string.Empty;
    public double HeatDemand { get; set; }
    public double HeatDelivered { get; set; }
    public double ElectricityPrice { get; set; }
    public double ElectricityProduced { get; set; }
    public double ElectricityConsumed { get; set; }
    public double NetElectricity { get; set; }
}

public class ElectricityBalancePointViewModel
{
    public string Label { get; }
    public double ElectricityProduced { get; }
    public double ElectricityConsumed { get; }
    public string ProductionText => $"P {ElectricityProduced:F1}";
    public string ConsumptionText => $"C {ElectricityConsumed:F1}";
    public double ProductionBarHeight { get; }
    public double ConsumptionBarHeight { get; }

    public ElectricityBalancePointViewModel(
        string label,
        double electricityProduced,
        double electricityConsumed,
        double productionBarHeight,
        double consumptionBarHeight)
    {
        Label = label;
        ElectricityProduced = electricityProduced;
        ElectricityConsumed = electricityConsumed;
        ProductionBarHeight = productionBarHeight;
        ConsumptionBarHeight = consumptionBarHeight;
    }
}