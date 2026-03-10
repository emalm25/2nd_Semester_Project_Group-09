using System;
using System.Collections.Generic;
using System.ComponentModel;

public class HeatingGrid
{
    public string Name {get; private set;}

    public HeatingGrid(string Name)
    {
        this.Name = Name;
    }
}

public class ProductionUnit
{
    public string ShortName {get; private set;}
    public string Name {get; private set;}
    public double MaxHeat {get; private set;}
    public decimal ProductionCosts {get; private set;}
    public double? CO2Emissions {get; private set;}
    
    // Virtual properties for specific unit types
    public virtual double? GasConsumption => null;
    public virtual double? OilConsumption => null;
    public virtual double? Gas2Consumption => null;
    public virtual double? MaxElectricity => null;
    public virtual string Image => "Assets/ProductionUnits/no-image.png";

    public ProductionUnit(string ShortName, string Name, double MaxHeat, decimal ProductionCosts,double? CO2Emissions )
    {
        this.ShortName = ShortName;
        this.Name = Name;
        this.MaxHeat = MaxHeat;
        this.ProductionCosts = ProductionCosts;
        this.CO2Emissions = CO2Emissions;
    }
}

public class GasBoilersInfo : ProductionUnit
{
    private double _gasConsumption;
    public override double? GasConsumption => _gasConsumption;
    public override string Image => "Assets/ProductionUnits/gas-boiler-image.png";

    public GasBoilersInfo(string ShortName, string Name, double MaxHeat, decimal ProductionCosts, double CO2Emissions, double GasConsumption)
    : base(ShortName, Name, MaxHeat, ProductionCosts, CO2Emissions)
    {
        this._gasConsumption = GasConsumption;
    }
}

public class OilBoilerInfo : ProductionUnit
{
    private double _oilConsumption;
    public override double? OilConsumption => _oilConsumption;
    public override string Image => "Assets/ProductionUnits/oil-boiler-image.png";

    public OilBoilerInfo(string ShortName, string Name, double MaxHeat, decimal ProductionCosts, double CO2Emissions, double OilConsumption)
    : base(ShortName, Name, MaxHeat, ProductionCosts, CO2Emissions)
    {
        this._oilConsumption = OilConsumption;
    }
}

public class GasMotorInfo : ProductionUnit
{
    private double _gas2Consumption;
    private double _maxElectricity;
    
    public override double? Gas2Consumption => _gas2Consumption;
    public override double? MaxElectricity => _maxElectricity;
    public override string Image => "Assets/ProductionUnits/gas-motor-image.png";

    public GasMotorInfo(string ShortName, string Name, double MaxHeat, double MaxElectricity, decimal ProductionCosts, double CO2Emissions, double Gas2Consumption)
    : base(ShortName, Name, MaxHeat, ProductionCosts, CO2Emissions)
    {
        this._gas2Consumption = Gas2Consumption;
        this._maxElectricity = MaxElectricity;
    }
}

public class ElectricBoilerInfo : ProductionUnit
{
    private double _maxElectricity;
    public override double? MaxElectricity => _maxElectricity;
    public override string Image => "Assets/ProductionUnits/electric-boiler-image.png";
    
    public ElectricBoilerInfo(string ShortName, string Name, double MaxHeat, double MaxElectricity, decimal ProductionCost )
    : base(ShortName, Name, MaxHeat, ProductionCost, null)
    {
        this._maxElectricity = MaxElectricity;
    }
}

public class AssetManager
{

   public List<ProductionUnit> Units { get; } = new List<ProductionUnit>
{
            new GasBoilersInfo("GB1", "Gas Boiler 1", 3.0 , 510, 132, 1.05),
            new GasBoilersInfo("GB2", "Gas Boiler 2", 2.0 , 540, 134, 1.08),
            new GasBoilersInfo("GB3", "Gas Boiler 3", 4.0 , 580, 136, 1.09),
            new OilBoilerInfo("OB1", "Oil Boiler 1", 6.0, 690, 147, 1.18),
            new GasMotorInfo("GM1", "Gas Motor 1", 5.3, 3.9, 975, 227, 1.82),
            new ElectricBoilerInfo("EB1", "Electric Boiler 1", 6.0, -6.0, 15)
        };


        }

    



