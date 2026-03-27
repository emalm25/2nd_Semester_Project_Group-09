using System;
using System.Linq;
using System.Text;
using CommunityToolkit.Mvvm.Input;
using HeatProductionOptimization.Models;
using HeatProductionOptimization.Services;

namespace HeatProductionOptimization.ViewModels;

public class OptimizerViewModel : ViewModelBase
{
    private readonly OptimizerService optimizerService;
    private readonly ResultDataManager resultDataManager;

    private OptimizationResult.ObjectiveType selectedObjective = OptimizationResult.ObjectiveType.Cost;
    private OptimizationResult.SeasonOption selectedSeason = OptimizationResult.SeasonOption.Winter;
    private OptimizationResult.ScenarioOption selectedScenario = OptimizationResult.ScenarioOption.Scenario1;

    private string optimizationSummary = "Current setup: Cost, Winter, Scenario 1";
    private string lastOptimizationMessage = "Ready to optimize.";

    public OptimizerViewModel(OptimizerService optimizerService, ResultDataManager resultDataManager)
    {
        this.optimizerService = optimizerService;
        this.resultDataManager = resultDataManager;

        SetObjectiveCommand = new RelayCommand<string>(SetObjective);
        SetSeasonCommand = new RelayCommand<string>(SetSeason);
        SetScenarioCommand = new RelayCommand<string>(SetScenario);
        OptimizeCommand = new RelayCommand(RunOptimization);

        UpdateSummaryFromSelection();
    }

    public bool IsCostSelected => selectedObjective == OptimizationResult.ObjectiveType.Cost;
    public bool IsCo2Selected => selectedObjective == OptimizationResult.ObjectiveType.CO2;

    public bool IsWinterSelected => selectedSeason == OptimizationResult.SeasonOption.Winter;
    public bool IsSummerSelected => selectedSeason == OptimizationResult.SeasonOption.Summer;

    public bool IsScenario1Selected => selectedScenario == OptimizationResult.ScenarioOption.Scenario1;
    public bool IsScenario2Selected => selectedScenario == OptimizationResult.ScenarioOption.Scenario2;

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
}
