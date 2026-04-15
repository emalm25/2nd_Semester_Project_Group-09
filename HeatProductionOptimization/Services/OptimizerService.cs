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
            var demand = row.HeatDemand * scenarioFactor;
            
            // Validate: Check if total available capacity can meet demand
            var totalAvailableCapacity = availableUnits.Sum(unit => unit.MaxHeatMW);
            if (totalAvailableCapacity < demand - 0.0001)
            {
                // Cannot meet demand - return error result
                result.StatusMessage = $"CRITICAL: Insufficient capacity to meet demand. Available: {totalAvailableCapacity:F1} MWh, Demand: {demand:F1} MWh";
                result.Timeline.Add(new OptimizationTimePoint
                {
                    StartTime = row.StartTime,
                    EndTime = row.EndTime,
                    HeatDemand = demand,
                    HeatDelivered = 0,
                    ElectricityPrice = row.ElectricityPrice,
                    Cost = 0,
                    Co2 = 0,
                    ElectricityProduced = 0,
                    ElectricityConsumed = 0,
                    NetElectricity = 0,
                    Dispatches = new List<OptimizationResult.DispatchResult>(),
                    DemandMet = false
                });
                continue;
            }

            // Rank units according to objective while respecting heat availability priority
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
                    UnitName = unit.Unit.Name,
                    HeatProduced = heat,
                    Cost = netCost,
                    Co2 = heat * (unit.Co2PerMWh ?? 0),
                    ElectricityProduced = electricityFlow > 0 ? electricityFlow : 0,
                    ElectricityConsumed = electricityFlow < 0 ? -electricityFlow : 0
                });
            }

            // Second pass: if demand not fully met, use any remaining unit capacity
            if (remainingDemand > 0.0001)
            {
                foreach (var unit in rankedUnits)
                {
                    if (remainingDemand <= 0.0001)
                    {
                        break;
                    }

                    var alreadyUsed = timeDispatches
                        .Where(d => d.UnitName == unit.Unit.Name)
                        .Sum(d => d.HeatProduced);
                    
                    var remainingCapacity = unit.MaxHeatMW - alreadyUsed;
                    if (remainingCapacity <= 0)
                    {
                        continue;
                    }

                    var additionalHeat = Math.Min(remainingCapacity, remainingDemand);
                    remainingDemand -= additionalHeat;

                    var existing = timeDispatches.FirstOrDefault(d => d.UnitName == unit.Unit.Name);
                    if (existing != null)
                    {
                        existing.HeatProduced += additionalHeat;
                        var electricityFlow = additionalHeat * unit.ElectricityPerHeatMWh;
                        var electricityRevenue = electricityFlow > 0 ? electricityFlow * row.ElectricityPrice : 0;
                        var electricityCost = electricityFlow < 0 ? -electricityFlow * row.ElectricityPrice : 0;
                        existing.Cost += additionalHeat * unit.CostPerMWh - electricityRevenue + electricityCost;
                        existing.Co2 += additionalHeat * (unit.Co2PerMWh ?? 0);
                        existing.ElectricityProduced += electricityFlow > 0 ? electricityFlow : 0;
                        existing.ElectricityConsumed += electricityFlow < 0 ? -electricityFlow : 0;
                    }
                }
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
                    .ToList(),
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
            $"Objective: {objective} | Season: {season} | Scenario: {scenario}";

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
