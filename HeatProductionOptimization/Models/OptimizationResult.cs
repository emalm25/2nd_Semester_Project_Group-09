using System.Collections.Generic;

namespace HeatProductionOptimization.Models;

public class OptimizationResult
{
    public enum ObjectiveType
    {
        Cost,
        CO2
    }

    public enum SeasonOption
    {
        Winter,
        Summer
    }

    public enum ScenarioOption
    {
        Scenario1,
        Scenario2
    }

    public class DispatchResult
    {
        public string UnitName { get; set; } = string.Empty;
        public double HeatProduced { get; set; }
        public double Cost { get; set; }
        public double Co2 { get; set; }
        public double ElectricityProduced { get; set; }
        public double ElectricityConsumed { get; set; }
    }

    public ObjectiveType Objective { get; set; }
    public SeasonOption SeasonType { get; set; }
    public ScenarioOption ScenarioType { get; set; }

    public List<DispatchResult> DispatchesByUnit { get; set; } = new();
    public List<OptimizationTimePoint> Timeline { get; set; } = new();

    public double TotalHeat { get; set; }
    public double TotalDemand { get; set; }
    public double TotalCost { get; set; }
    public double TotalCo2 { get; set; }
    public double NetElectricity { get; set; }

    public string SummaryMessage { get; set; } = string.Empty;
    public string StatusMessage { get; set; } = string.Empty;
    public string MaintenanceUnit { get; set; } = string.Empty;
    public int MaintenanceHours { get; set; } = 0;

    
    public string Minimalize
    {
        get => Objective == ObjectiveType.CO2 ? "CO2" : "Cost";
        set => Objective = value?.Trim().Equals("CO2", System.StringComparison.OrdinalIgnoreCase) == true
            ? ObjectiveType.CO2
            : ObjectiveType.Cost;
    }

    public string Season
    {
        get => SeasonType.ToString();
        set => SeasonType = value?.Trim().Equals("Summer", System.StringComparison.OrdinalIgnoreCase) == true
            ? SeasonOption.Summer
            : SeasonOption.Winter;
    }

    public string Scenario
    {
        get => ScenarioType == ScenarioOption.Scenario2 ? "Scenario 2" : "Scenario 1";
        set => ScenarioType = value?.Trim().Equals("Scenario 2", System.StringComparison.OrdinalIgnoreCase) == true
            ? ScenarioOption.Scenario2
            : ScenarioOption.Scenario1;
    }

    public double HeatProduction
    {
        get => TotalHeat;
        set => TotalHeat = value;
    }

    public double ElectricityProduction
    {
        get => NetElectricity > 0 ? NetElectricity : 0;
        set { }
    }

    public double ElectricityConsumption
    {
        get => NetElectricity < 0 ? -NetElectricity : 0;
        set { }
    }

    public double Expenses
    {
        get => TotalCost;
        set => TotalCost = value;
    }

    public double Profit
    {
        get => -TotalCost;
        set { }
    }

    public double PrimaryEnergyConsumption
    {
        get => TotalHeat;
        set { }
    }
    public double CO2Emissions
    {
        get => TotalCo2;
        set => TotalCo2 = value;
    }
}