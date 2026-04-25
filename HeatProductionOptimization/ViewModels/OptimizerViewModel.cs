using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
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
    private const int LabelIntervalHours = 4;

    private readonly OptimizerService optimizerService;
    private readonly ResultDataManager resultDataManager;
    private readonly ResultDataManagerViewModel resultDataManagerViewModel;
    private readonly AssetManagerViewModel assetManagerViewModel;
    private Timer refreshTimer;

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
    public ObservableCollection<ElectricityBalancePointViewModel> ElectricityBalanceSeries { get; } = new();
    public ObservableCollection<MetricCardViewModel> SummaryMetrics { get; } = new();
    public ObservableCollection<UnitSettingRowViewModel> ProductionUnitSettings { get; } = new();
    public ObservableCollection<UnitPriorityRowViewModel> PriorityRanking { get; } = new();
    public ObservableCollection<ChartLegendItemViewModel> MainChartLegendItems { get; } = new();

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
        assetManagerViewModel.Units.CollectionChanged += OnAssetUnitsCollectionChanged;
        SubscribeToAssetAvailabilityChanges();

        // Auto-refresh timer to pick up changes from Asset Manager
        refreshTimer = new Timer(RefreshIfNeeded, null, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));

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
        try
        {
            var result = optimizerService.Optimize(selectedObjective, selectedSeason, selectedScenario);
            resultDataManager.AddResult(result);

            OptimizationSummary =
                $"Selected: {FormatObjective(selectedObjective)}, {selectedSeason}, {FormatScenario(selectedScenario)} | " +
                $"Points: {result.Timeline.Count} | Heat: {result.TotalHeat:F1} MWh | Cost: {result.TotalCost:F1} | CO2: {result.TotalCo2:F1} | Net el.: {result.NetElectricity:F1} MWh";

            LastOptimizationMessage = result.StatusMessage;

            // Refresh visualization for the new result
            SelectedResult = result;
        }
        catch (Exception ex)
        {
            LastOptimizationMessage = $"Error during optimization: {ex.Message}";
            System.Diagnostics.Debug.WriteLine($"RunOptimization error: {ex}");
        }
    }

    private void UpdateSummaryFromSelection()
    {
        OptimizationSummary =
            $"Current setup: {FormatObjective(selectedObjective)}, {selectedSeason}, {FormatScenario(selectedScenario)}";
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
        ElectricityBalanceSeries.Clear();
        SummaryMetrics.Clear();
        PriorityRanking.Clear();

        if (SelectedResult == null)
        {
            NetProductionCostSeries = Array.Empty<ISeries>();
            NetProductionCostXAxes = Array.Empty<Axis>();
            NetProductionCostYAxes = Array.Empty<Axis>();
            MainChartLegendItems.Clear();
            OnPropertyChanged(nameof(SelectedResultDescription));
            return;
        }

        var displayedTimeline = GetDisplayedTimeline(SelectedResult.Timeline);
        BuildElectricityBalanceSeries(ElectricityBalanceSeries, displayedTimeline);
        BuildNetProductionCostChart();

        var totalProduced = SelectedResult.Timeline.Sum(point => point.HeatDelivered);
        var totalElectricityProduced = SelectedResult.Timeline.Sum(point => point.ElectricityProduced);
        var totalElectricityConsumed = SelectedResult.Timeline.Sum(point => point.ElectricityConsumed);
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

    private static IReadOnlyList<OptimizationTimePoint> GetDisplayedTimeline(IReadOnlyList<OptimizationTimePoint> timeline)
    {
        if (timeline.Count == 0)
        {
            return Array.Empty<OptimizationTimePoint>();
        }

        var points = timeline
            .Where(point => point.StartTime.Hour % LabelIntervalHours == 0)
            .ToList();

        if (points.Count == 0)
        {
            points = timeline
                .Where((point, index) => index % LabelIntervalHours == 0)
                .ToList();
        }

        return points;
    }

    private static string FormatTimestampLabel(DateTime startTime)
    {
        return startTime.ToString("dd/MM HH:mm");
    }

    private void BuildNetProductionCostChart()
    {
        if (SelectedResult == null || SelectedResult.Timeline.Count == 0)
        {
            NetProductionCostSeries = Array.Empty<ISeries>();
            NetProductionCostXAxes = Array.Empty<Axis>();
            NetProductionCostYAxes = Array.Empty<Axis>();
            MainChartLegendItems.Clear();
            return;
        }

        var points = SelectedResult.Timeline.ToList();

        if (points.Count == 0)
        {
            NetProductionCostSeries = Array.Empty<ISeries>();
            NetProductionCostXAxes = Array.Empty<Axis>();
            NetProductionCostYAxes = Array.Empty<Axis>();
            MainChartLegendItems.Clear();
            return;
        }

        var labels = points
            .Select(point => point.StartTime.Hour % LabelIntervalHours == 0
                ? FormatTimestampLabel(point.StartTime)
                : string.Empty)
            .ToArray();

        var scenarioUnitShortNames = GetScenarioUnitOrderedShortNames(SelectedResult.ScenarioType);
        var availableUnitNames = assetManagerViewModel.Units
            .Where(unit => unit.IsAvailable)
            .Select(unit => unit.ShortName)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var series = new List<ISeries>();
        MainChartLegendItems.Clear();

        foreach (var unitShortName in scenarioUnitShortNames.Where(availableUnitNames.Contains))
        {
            var unitColor = GetUnitColor(unitShortName);
            var unitValues = points
                .Select(point =>
                {
                    var unitDispatch = point.Dispatches
                        .Where(dispatch => dispatch.UnitName == unitShortName)
                        .Sum(dispatch => dispatch.HeatProduced);

                    return unitDispatch;
                })
                .ToArray();

            series.Add(new StackedColumnSeries<double>
            {
                Name = unitShortName,
                Values = unitValues,
                Fill = new SolidColorPaint(unitColor),
                Stroke = null
            });

            MainChartLegendItems.Add(new ChartLegendItemViewModel(
                unitShortName,
                new SolidColorBrush(Color.FromArgb(unitColor.Alpha, unitColor.Red, unitColor.Green, unitColor.Blue))));
        }

        var demandColor = new SKColor(217, 69, 69);
        var demandValues = points.Select(point => point.HeatDemand).ToArray();
        series.Add(new LineSeries<double>
        {
            Name = "Heat Demand",
            Values = demandValues,
            Stroke = new SolidColorPaint(demandColor) { StrokeThickness = 3 },
            Fill = null,
            GeometrySize = 0,
            LineSmoothness = 1,
            GeometryFill = null,
            GeometryStroke = null
        });

        MainChartLegendItems.Add(new ChartLegendItemViewModel(
            "Heat Demand",
            new SolidColorBrush(Color.FromArgb(demandColor.Alpha, demandColor.Red, demandColor.Green, demandColor.Blue))));

        NetProductionCostSeries = series.ToArray();
        NetProductionCostXAxes = new Axis[]
        {
            new Axis
            {
                Labels = labels,
                LabelsRotation = 45,
                TextSize = 11,
                LabelsPaint = new SolidColorPaint(SKColors.White),
                NamePaint = new SolidColorPaint(SKColors.White),
                Name = "Time"
            }
        };
        
        NetProductionCostYAxes = new Axis[]
        {
            new Axis
            {
                Name = "Heat Produced (MWh)",
                MinLimit = 0,
                TextSize = 11,
                LabelsPaint = new SolidColorPaint(SKColors.White),
                NamePaint = new SolidColorPaint(SKColors.White)
            }
        };
    }

    private static void BuildElectricityBalanceSeries(
        ObservableCollection<ElectricityBalancePointViewModel> target,
        IEnumerable<OptimizationTimePoint> points)
    {
        var source = points.ToList();
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
                FormatTimestampLabel(item.StartTime),
                item.ElectricityProduced,
                item.ElectricityConsumed,
                productionHeight,
                consumptionHeight));
        }
    }

    private static IEnumerable<UnitPriorityRowViewModel> BuildPriorityRanking(OptimizationResult result)
    {
        return result.DispatchesByUnit
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

    private string[] GetScenarioUnitOrderedShortNames(OptimizationResult.ScenarioOption scenario)
    {
        var scenarioName = scenario.ToString();

        var scenarioUnits = assetManagerViewModel.Units
            .Where(u => u.IsAvailable && u.Scenarios != null && u.Scenarios.Any(s =>
                s.Equals("all", StringComparison.OrdinalIgnoreCase) ||
                s.Equals(scenarioName, StringComparison.OrdinalIgnoreCase)))
            .Select(u => u.ShortName)
            .OrderBy(x => x)
            .ToArray();

        return scenarioUnits;
    }

    private SKColor GetUnitColor(string shortName)
    {
        var colors = new[]
        {
            SKColors.MediumSeaGreen,
            SKColors.DodgerBlue,
            SKColors.SteelBlue,
            SKColors.Teal,
            SKColors.Goldenrod,
            SKColors.MediumPurple,
            SKColors.OrangeRed,
            SKColors.LimeGreen,
            SKColors.DeepSkyBlue,
            SKColors.Gold
        };

        return shortName.ToUpperInvariant() switch
        {
            "GB1" => SKColors.MediumSeaGreen,
            "GB2" => SKColors.DodgerBlue,
            "GB3" => SKColors.SteelBlue,
            "OB1" => SKColors.Teal,
            "GM1" => SKColors.Goldenrod,
            "EB1" => SKColors.MediumPurple,
            _ => colors[Math.Abs(shortName.GetHashCode()) % colors.Length]
        };
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
        // Save changes to file
        assetManagerViewModel.AssetManager.SaveUnits();

        // Rebuild UI display
        BuildProductionUnitSettings();

        // Re-run optimization with updated units
        if (SelectedResult != null)
        {
            RunOptimization();
        }
    }

    private void OnAssetUnitsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.NewItems != null)
        {
            foreach (ProductionUnitItemViewModel unit in e.NewItems)
            {
                unit.PropertyChanged += OnAssetUnitPropertyChanged;
            }
        }
        if (e.OldItems != null)
        {
            foreach (ProductionUnitItemViewModel unit in e.OldItems)
            {
                unit.PropertyChanged -= OnAssetUnitPropertyChanged;
            }
        }

        // Save changes
        assetManagerViewModel.AssetManager.SaveUnits();

        // Rebuild UI
        BuildProductionUnitSettings();

        // Re-optimize
        if (SelectedResult != null)
        {
            RunOptimization();
        }
    }

    private void RefreshIfNeeded(object? state)
    {
        if (SelectedResult != null && assetManagerViewModel.IsEditMode)
        {
            // Auto-refresh optimizer when editing units
            RunOptimization();
        }
    }

    public void Dispose()
    {
        refreshTimer?.Dispose();
        UnsubscribeFromAssetAvailabilityChanges();
        assetManagerViewModel.Units.CollectionChanged -= OnAssetUnitsCollectionChanged;
        resultDataManagerViewModel.PropertyChanged -= OnResultDataManagerPropertyChanged;
        resultDataManagerViewModel.Results.CollectionChanged -= OnResultsCollectionChanged;
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
    public bool ShowCo2 => Co2PerMWh > 0.001;
}

public class UnitPriorityRowViewModel
{
    public string UnitName { get; set; } = string.Empty;
    public double HeatProduced { get; set; }
    public double NetCost { get; set; }
    public double NetCostPerMWh { get; set; }
    public double Co2 { get; set; }
    public bool ShowCo2 => Co2 > 0.001;
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

public class ChartLegendItemViewModel
{
    public string Name { get; }
    public IBrush ColorBrush { get; }

    public ChartLegendItemViewModel(string name, IBrush colorBrush)
    {
        Name = name;
        ColorBrush = colorBrush;
    }
}