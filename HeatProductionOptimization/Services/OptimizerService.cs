using System;
using System.Collections.Generic;
using System.Linq;
using HeatProductionOptimization.Models;
using AssetUnit = global::ProductionUnit;
using SourceRow = global::Data;

namespace HeatProductionOptimization.Services;

public class OptimizerService
{
    private readonly SourceManager sourceManager;
    private readonly AssetManager assetManager;

    public OptimizerService()
    {
        sourceManager = new SourceManager();
        assetManager = new AssetManager();
    }

    public OptimizationResult Optimize(
        OptimizationResult.ObjectiveType objective,
        OptimizationResult.SeasonOption season,
        OptimizationResult.ScenarioOption scenario)
    {
        var sourceRows = LoadSourceRows(season);
        var scenarioFactor = scenario == OptimizationResult.ScenarioOption.Scenario2 ? 1.10 : 1.00;
        var maintenanceShortNames = GetMaintenanceUnitShortNames(season, scenario);

        var availableUnits = assetManager.Units
            .Select((unit, index) => new
            {
                Unit = unit,
                Priority = index + 1,
                IsInMaintenance = maintenanceShortNames.Contains(unit.ShortName),
                CostPerMWh = (double)unit.ProductionCosts,
                Co2PerMWh = unit.CO2Emissions,
                MaxHeatMW = unit.MaxHeat,
                ElectricityPerHeatMWh = GetElectricityPerHeat(unit)
            })
            .Where(unit => unit.Unit.IsActive && !unit.IsInMaintenance)
            .ToList();

        var result = new OptimizationResult
        {
            Objective = objective,
            SeasonType = season,
            ScenarioType = scenario
        };

        foreach (var row in sourceRows)
        {
            var rankedUnits = objective == OptimizationResult.ObjectiveType.Cost
                ? availableUnits
                    .OrderBy(unit => CalculateEffectiveCostPerMWh(unit.CostPerMWh, unit.ElectricityPerHeatMWh, row.ElectricityPrice))
                    .ThenBy(unit => unit.Priority)
                    .ToList()
                : availableUnits
                    .OrderBy(unit => unit.Co2PerMWh.HasValue ? 0 : 1)
                    .ThenBy(unit => unit.Co2PerMWh ?? double.MaxValue)
                    .ThenBy(unit => unit.Priority)
                    .ToList();

            var demand = row.HeatDemand * scenarioFactor;
            var remainingDemand = demand;
            var timeDispatches = new List<OptimizationResult.DispatchResult>();

            foreach (var unit in rankedUnits)
            {
                if (remainingDemand <= 0)
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
                    UnitName = unit.Unit.Name,
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
            var timeNetElectricity = timeDispatches.Sum(dispatch => dispatch.ElectricityProduced - dispatch.ElectricityConsumed);

            result.Timeline.Add(new OptimizationTimePoint
            {
                StartTime = row.StartTime,
                EndTime = row.EndTime,
                HeatDemand = demand,
                HeatDelivered = delivered,
                Cost = timeCost,
                Co2 = timeCo2,
                NetElectricity = timeNetElectricity,
                DemandMet = delivered + 0.0001 >= demand
            });

            result.Dispatches.AddRange(timeDispatches);
        }

        result.TotalDemand = result.Timeline.Sum(point => point.HeatDemand);
        result.TotalHeat = result.Dispatches.Sum(dispatch => dispatch.HeatProduced);
        result.TotalCost = result.Dispatches.Sum(dispatch => dispatch.Cost);
        result.TotalCo2 = result.Dispatches.Sum(dispatch => dispatch.Co2);
        result.NetElectricity = result.Dispatches.Sum(dispatch => dispatch.ElectricityProduced - dispatch.ElectricityConsumed);

        result.Dispatches = result.Dispatches
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

        var unmetDemand = Math.Max(0, result.TotalDemand - result.TotalHeat);
        result.StatusMessage = unmetDemand <= 0.0001
            ? "Optimization completed. Demand fully met for all time points."
            : $"Warning: Unable to meet full demand. Unmet heat in period: {unmetDemand:F1} MWh.";

        result.SummaryMessage =
            $"Objective: {objective}, Season: {season}, Scenario: {scenario}, Points: {result.Timeline.Count}, " +
            $"Demand: {result.TotalDemand:F1} MWh, Delivered: {result.TotalHeat:F1} MWh, Cost: {result.TotalCost:F1}, " +
            $"CO2: {result.TotalCo2:F1}, Net electricity: {result.NetElectricity:F1} MWh.";

        return result;
    }

    private List<SourceRow> LoadSourceRows(OptimizationResult.SeasonOption season)
    {
        return season == OptimizationResult.SeasonOption.Winter
            ? sourceManager.GetWinterData()
            : sourceManager.GetSummerData();
    }

    private static HashSet<string> GetMaintenanceUnitShortNames(
        OptimizationResult.SeasonOption season,
        OptimizationResult.ScenarioOption scenario)
    {
        // Sprint 2 placeholder availability setup using short names from AssetManager.
        if (season == OptimizationResult.SeasonOption.Winter && scenario == OptimizationResult.ScenarioOption.Scenario2)
        {
            return new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "OB1" };
        }

        return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
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
