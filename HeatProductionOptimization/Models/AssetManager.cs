using System;
using System.Collections.Generic;
using System.ComponentModel;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using HeatProductionOptimization.Services;

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
    public string ShortName {get; set;}
    public string Name {get; set;}
    public bool IsAvailable {get; set;}
    public double MaxHeat {get; set;}
    public decimal ProductionCosts {get; set;}
    public double? CO2Emissions {get; set;}
    
    public virtual double? GasConsumption { get; set; } = null;
    public virtual double? OilConsumption { get; set; } = null;
    public virtual double? Gas2Consumption { get; set; } = null;
    public virtual double? MaxElectricity { get; set; } = null;
    public virtual string ImagePath { get; } = "avares://HeatProductionOptimization/Assets/ProductionUnits/no-image.png";
    public Bitmap Image => new Bitmap(AssetLoader.Open(new Uri(ImagePath)));

    public ProductionUnit(string ShortName, string Name, bool IsAvailable, double MaxHeat, decimal ProductionCosts,double? CO2Emissions )
    {
        this.ShortName = ShortName;
        this.Name = Name;
        this.IsAvailable = IsAvailable;
        this.MaxHeat = MaxHeat;
        this.ProductionCosts = ProductionCosts;
        this.CO2Emissions = CO2Emissions;
    }
}

public class GasBoilersInfo : ProductionUnit
{
    public override string ImagePath { get; } = "avares://HeatProductionOptimization/Assets/ProductionUnits/gas-boiler-image.png";

    public GasBoilersInfo(string ShortName, string Name, bool IsAvailable, double MaxHeat, decimal ProductionCosts, double CO2Emissions, double GasConsumption)
    : base(ShortName, Name, IsAvailable, MaxHeat, ProductionCosts, CO2Emissions)
    {
        this.GasConsumption = GasConsumption;
    }
}

public class OilBoilerInfo : ProductionUnit
{
    public override string ImagePath { get; } = "avares://HeatProductionOptimization/Assets/ProductionUnits/oil-boiler-image.png";

    public OilBoilerInfo(string ShortName, string Name, bool IsAvailable, double MaxHeat, decimal ProductionCosts, double CO2Emissions, double OilConsumption)
    : base(ShortName, Name, IsAvailable, MaxHeat, ProductionCosts, CO2Emissions)
    {
        this.OilConsumption = OilConsumption;
    }
}

public class GasMotorInfo : ProductionUnit
{
    public override string ImagePath { get; } = "avares://HeatProductionOptimization/Assets/ProductionUnits/gas-motor-image.png";

    public GasMotorInfo(string ShortName, string Name, bool IsAvailable, double MaxHeat, double MaxElectricity, decimal ProductionCosts, double CO2Emissions, double Gas2Consumption)
    : base(ShortName, Name, IsAvailable, MaxHeat, ProductionCosts, CO2Emissions)
    {
        this.Gas2Consumption = Gas2Consumption;
        this.MaxElectricity = MaxElectricity;
    }
}

public class ElectricBoilerInfo : ProductionUnit
{
    public override string ImagePath { get; } = "avares://HeatProductionOptimization/Assets/ProductionUnits/electric-boiler-image.png";

    public ElectricBoilerInfo(string ShortName, string Name, bool IsAvailable, double MaxHeat, double MaxElectricity, decimal ProductionCost )
    : base(ShortName, Name, IsAvailable, MaxHeat, ProductionCost, null)
    {
        this.MaxElectricity = MaxElectricity;
    }
}

public class AssetManager
{
    private readonly UnitConfigurationService _configService;

    public List<ProductionUnit> Units { get; }

    public AssetManager()
    {
        _configService = new UnitConfigurationService();
        Units = _configService.LoadUnits();
    }

    public void SaveUnits()
    {
        _configService.SaveUnits(Units);
    }
}

    
