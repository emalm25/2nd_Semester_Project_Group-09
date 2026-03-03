using System;
using System.Collections.Generic;

public class HeatingGrid
{
    private string Name;
    private string ImagePath;

    public HeatingGrid(string Name, string ImagePath)
    {
        this.Name = Name;
        this.ImagePath = ImagePath;
    }
}

public class ProductionUnit
{
    private string Name;
    private string ImagePath;
    private double ProducedHeat;
    private double ConsumptionEnegry;
    private decimal ProductionCosts;

    private double CO2Emissions;

    public ProductionUnit(string Name, string ImagePath, double ProducedHeat,double ConsumptionEnegry, decimal ProductionCosts,double CO2Emissions )
    {
        this.Name = Name;
        this.ImagePath = ImagePath;
        this.ProducedHeat = ProducedHeat;
        this.ConsumptionEnegry = ConsumptionEnegry;
        this.ProductionCosts = ProductionCosts;
        this.CO2Emissions = CO2Emissions;
    }
}

public class Machines
{ 
    List<string> names = new List<string> {"Gas boiler 1", "Gas boiler 2", "Gas boiler 3", "Oil boiler 1", "Gas motor 1", "Electric boiler 1" }; 

}
