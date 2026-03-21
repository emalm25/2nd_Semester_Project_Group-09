namespace HeatProductionOptimization.Models;

public class OptimizationResult
{
    public string Minimalize { get; set; } = "";
    public string Season { get; set; } = "";
    public string Scenario { get; set; } = "";

    public double HeatProduction { get; set; }
    public double ElectricityProduction { get; set; }
    public double ElectricityConsumption { get; set; }

    public double Expenses { get; set; }
    public double Profit { get; set; }

    public double PrimaryEnergyConsumption { get; set; }
    public double CO2Emissions { get; set; }
}