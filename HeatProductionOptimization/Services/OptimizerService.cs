using System;
using System.Collections.Generic;
using System.Linq;
using HeatProductionOptimization.Models;
using HeatProductionOptimization.Services;
using AssetUnit = global::ProductionUnit;
using SourceRow = global::Data;

namespace HeatProductionOptimization.Services;

public class OptimizerService
{
    private const int DefaultMaintenanceHours = 48;

    private readonly SourceManager sourceManager;
    private readonly AssetManager assetManager;

    public OptimizerService(AssetManager assetManager)
    {
        sourceManager = new SourceManager();
        this.assetManager = assetManager;
    }

    public OptimizationResult Optimize(
        OptimizationResult.ObjectiveType objective,
        OptimizationResult.SeasonOption season,
        OptimizationResult.ScenarioOption scenario)
    {
        var sourceRows = LoadSourceRows(season);
        var scenarioUnitShortNames = GetScenarioUnitShortNames(scenario);
        var maintenanceWindow = BuildMaintenanceWindow(sourceRows, season, scenario, scenarioUnitShortNames);

        var scenarioUnits = assetManager.Units
            .Select((unit, index) => new
            {
                Unit = unit,
                Priority = index + 1,
                CostPerMWh = (double)unit.ProductionCosts,
                Co2PerMWh = unit.CO2Emissions,
                MaxHeatMW = unit.MaxHeat,
                ElectricityPerHeatMWh = GetElectricityPerHeat(unit)
            })
            .Where(unit => scenarioUnitShortNames.Contains(unit.Unit.ShortName))
            .ToList();

        var result = new OptimizationResult
        {
            Objective = objective,
            SeasonType = season,
            ScenarioType = scenario
        };

        foreach (var row in sourceRows)
        {
            var demand = row.HeatDemand;

            var maintenanceActive = maintenanceWindow != null
                && row.StartTime >= maintenanceWindow.Start
                && row.StartTime < maintenanceWindow.End;

            var availableUnits = scenarioUnits
                .Where(unit =>
                    unit.Unit.IsAvailable
                    && (!maintenanceActive || !unit.Unit.ShortName.Equals(maintenanceWindow!.UnitShortName, StringComparison.OrdinalIgnoreCase)))
                .ToList();

            // Rank units according to objective while respecting heat availability priority
            var rankedUnits = objective == OptimizationResult.ObjectiveType.Cost
                ? availableUnits
                    .OrderBy(unit => CalculateEffectiveCostPerMWh(unit.CostPerMWh, unit.ElectricityPerHeatMWh, row.ElectricityPrice))
                    .ThenBy(unit => unit.Priority)
                    .ToList()
                : availableUnits
                    .OrderBy(unit => unit.Co2PerMWh ?? 0)
                    .ThenBy(unit => unit.Priority)
                    .ToList();

            var remainingDemand = demand;
            var timeDispatches = new List<OptimizationResult.DispatchResult>();

            // First pass: assign heat to optimal units
            foreach (var unit in rankedUnits)
            {
                if (remainingDemand <= 0.0001)
                {
                    break;
                }

                var maxAllowed = Math.Min(unit.MaxHeatMW, remainingDemand);
                if (maxAllowed <= 0)
                {
                    continue;
                }

                var heat = maxAllowed;
                remainingDemand -= heat;

                var rawCost = heat * unit.CostPerMWh;
                var electricityFlow = heat * unit.ElectricityPerHeatMWh;
                var electricityRevenue = electricityFlow > 0 ? electricityFlow * row.ElectricityPrice : 0;
                var electricityCost = electricityFlow < 0 ? -electricityFlow * row.ElectricityPrice : 0;
                var netCost = rawCost - electricityRevenue + electricityCost;

                timeDispatches.Add(new OptimizationResult.DispatchResult
                {
                    UnitName = unit.Unit.ShortName,
                    HeatProduced = heat,
                    Cost = netCost,
                    Co2 = heat * (unit.Co2PerMWh ?? 0),
                    ElectricityProduced = electricityFlow > 0 ? electricityFlow : 0,
                    ElectricityConsumed = electricityFlow < 0 ? -electricityFlow : 0
                });
            }

            var delivered = timeDispatches.Sum(dispatch => dispatch.HeatProduced);
            var timeCost = timeDispatches.Sum(dispatch => dispatch.Cost);
            var timeCo2 = timeDispatches.Sum(dispatch => dispatch.Co2);
            var timeElectricityProduced = timeDispatches.Sum(dispatch => dispatch.ElectricityProduced);
            var timeElectricityConsumed = timeDispatches.Sum(dispatch => dispatch.ElectricityConsumed);
            var timeNetElectricity = timeDispatches.Sum(dispatch => dispatch.ElectricityProduced - dispatch.ElectricityConsumed);

            result.Timeline.Add(new OptimizationTimePoint
            {
                StartTime = row.StartTime,
                EndTime = row.EndTime,
                HeatDemand = demand,
                HeatDelivered = delivered,
                ElectricityPrice = row.ElectricityPrice,
                Cost = timeCost,
                Co2 = timeCo2,
                ElectricityProduced = timeElectricityProduced,
                ElectricityConsumed = timeElectricityConsumed,
                NetElectricity = timeNetElectricity,
                Dispatches = timeDispatches
                    .Select(dispatch => new OptimizationResult.DispatchResult
                    {
                        UnitName = dispatch.UnitName,
                        HeatProduced = dispatch.HeatProduced,
                        Cost = dispatch.Cost,
                        Co2 = dispatch.Co2,
                        ElectricityProduced = dispatch.ElectricityProduced,
                        ElectricityConsumed = dispatch.ElectricityConsumed
                    })
                    .ToList()
            });
        }

        result.TotalDemand = result.Timeline.Sum(point => point.HeatDemand);
        result.TotalHeat = result.Timeline.Sum(point => point.HeatDelivered);
        result.TotalCost = result.Timeline.Sum(point => point.Cost);
        result.TotalCo2 = result.Timeline.Sum(point => point.Co2);
        result.NetElectricity = result.Timeline.Sum(point => point.NetElectricity);

        var allTimeDispatches = result.Timeline
            .SelectMany(point => point.Dispatches)
            .ToList();

        result.DispatchesByUnit = allTimeDispatches
            .GroupBy(dispatch => dispatch.UnitName)
            .Select(group => new OptimizationResult.DispatchResult
            {
                UnitName = group.Key,
                HeatProduced = group.Sum(item => item.HeatProduced),
                Cost = group.Sum(item => item.Cost),
                Co2 = group.Sum(item => item.Co2),
                ElectricityProduced = group.Sum(item => item.ElectricityProduced),
                ElectricityConsumed = group.Sum(item => item.ElectricityConsumed)
            })
            .OrderByDescending(dispatch => dispatch.HeatProduced)
            .ToList();

        result.StatusMessage = "Optimization completed.";

        var scenarioSummary = BuildScenarioSummary(scenario, scenarioUnitShortNames, maintenanceWindow);
        result.SummaryMessage =
            $"Objective: {objective} | Season: {season} | Scenario: {scenario} | {scenarioSummary}";

        return result;
    }

    private List<SourceRow> LoadSourceRows(OptimizationResult.SeasonOption season)
    {
        return season == OptimizationResult.SeasonOption.Winter
            ? sourceManager.GetWinterData()
            : sourceManager.GetSummerData();
    }

    private HashSet<string> GetScenarioUnitShortNames(
        OptimizationResult.ScenarioOption scenario)
    {
        var scenarioName = scenario.ToString();

        var units = assetManager.Units
            .Where(u => u.Scenarios != null && u.Scenarios.Any(s =>
                s.Equals("all", StringComparison.OrdinalIgnoreCase) ||
                s.Equals(scenarioName, StringComparison.OrdinalIgnoreCase)))
            .Select(u => u.ShortName)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return units.Any() ? units : new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    }

    private static string BuildScenarioSummary(
        OptimizationResult.ScenarioOption scenario,
        HashSet<string> scenarioUnitShortNames,
        MaintenanceWindow? maintenanceWindow)
    {
        var units = string.Join(", ", scenarioUnitShortNames.OrderBy(name => name));
        var maintenanceText = maintenanceWindow != null
            ? $"{maintenanceWindow.UnitShortName} ({maintenanceWindow.DurationHours}h)"
            : "none";

        return $"Units: {units} | Maintenance: {maintenanceText}";
    }

    private MaintenanceWindow? BuildMaintenanceWindow(
        IReadOnlyList<SourceRow> sourceRows,
        OptimizationResult.SeasonOption season,
        OptimizationResult.ScenarioOption scenario,
        HashSet<string> scenarioUnitShortNames)
    {
        var windowHours = Math.Clamp(DefaultMaintenanceHours, 30, 60);
        if (sourceRows.Count == 0)
        {
            return null;
        }

        var orderedRows = sourceRows.OrderBy(row => row.StartTime).ToList();
        var hoursInPeriod = orderedRows.Count;
        var effectiveHours = Math.Min(windowHours, hoursInPeriod);
        var maxStartIndex = Math.Max(0, hoursInPeriod - effectiveHours);

        // Place maintenance around the middle of the period.
        var startIndex = Math.Min(maxStartIndex, Math.Max(0, (hoursInPeriod - effectiveHours) / 2));

        var scenarioName = scenario.ToString();
        var scenarioData = assetManager.Scenarios
            .FirstOrDefault(s => s.Name.Equals(scenarioName, StringComparison.OrdinalIgnoreCase));

        var maintenanceUnit = GetMaintenanceUnitShortName(season, scenarioUnitShortNames, scenarioData);
        if (string.IsNullOrWhiteSpace(maintenanceUnit))
        {
            return null;
        }

        var start = orderedRows[startIndex].StartTime;
        var end = start.AddHours(effectiveHours);

        return new MaintenanceWindow(maintenanceUnit, start, end, effectiveHours);
    }

    private static string GetMaintenanceUnitShortName(
        OptimizationResult.SeasonOption season,
        HashSet<string> scenarioUnitShortNames,
        ScenarioData? scenarioData)
    {
        if (scenarioData != null
            && scenarioData.MaintenanceUnits.TryGetValue(season.ToString(), out var preferred)
            && !string.IsNullOrWhiteSpace(preferred)
            && scenarioUnitShortNames.Contains(preferred))
        {
            return preferred;
        }

        return scenarioUnitShortNames.OrderBy(name => name).FirstOrDefault() ?? string.Empty;
    }

    private sealed class MaintenanceWindow
    {
        public string UnitShortName { get; }
        public DateTime Start { get; }
        public DateTime End { get; }
        public int DurationHours { get; }

        public MaintenanceWindow(string unitShortName, DateTime start, DateTime end, int durationHours)
        {
            UnitShortName = unitShortName;
            Start = start;
            End = end;
            DurationHours = durationHours;
        }
    }

    private static double GetElectricityPerHeat(AssetUnit assetUnit)
    {
        if (assetUnit.MaxElectricity.HasValue && assetUnit.MaxHeat > 0)
        {
            return assetUnit.MaxElectricity.Value / assetUnit.MaxHeat;
        }

        return 0;
    }

    private static double CalculateEffectiveCostPerMWh(double costPerMWh, double electricityPerHeatMWh, double electricityPricePerMWh)
    {
        var electricityRevenuePerMWhHeat = electricityPerHeatMWh > 0 ? electricityPerHeatMWh * electricityPricePerMWh : 0;
        var electricityCostPerMWhHeat = electricityPerHeatMWh < 0 ? -electricityPerHeatMWh * electricityPricePerMWh : 0;
        return costPerMWh - electricityRevenuePerMWhHeat + electricityCostPerMWhHeat;
    }
}
