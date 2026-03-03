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

    public ProductionUnit(string ShortName, string Name, double MaxHeat, decimal ProductionCosts,double CO2Emissions )
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
        public double GasConsumption { get; private set;}

        public GasBoilerInfo(string ShortName, string Name, double MaxHeat, decimal ProductionCosts, double CO2Emissions, double GasConsumption)
        : base(ShortName, Name, MaxHeat, ProductionCosts, CO2Emissions)
        {
            GasConsumption = GasConsumption;
        }
       
    }


    public class OilBoilerInfo : ProductionUnit
    {
        public double OilConsumption { get; private set;}

        public OilBoilerInfo(string ShortName, string Name, double MaxHeat, decimal ProductionCosts, double CO2Emissions, double OilConsumption)
        : base(ShortName, Name, MaxHeat, ProductionCosts, CO2Emissions)
        {
            this.OilConsumption = OilConsumption;
        }


    }

      public class GasMotorInfo : ProductionUnit
    {
        public double Gas2Consumption { get; private set;}
        public double MaxElectricity { get; private set;}

        public GasMotorInfo(string ShortName, string Name, double MaxHeat, double MaxElectricity, decimal ProductionCosts, double CO2Emissions, double Gas2Consumption)
        : base(ShortName, Name, MaxHeat, ProductionCosts, CO2Emissions)
        {
            this.Gas2Consumption = Gas2Consumption;
            this.MaxElectricity = MaxElectricity;
        }


    }

    public class ElectricBoilerInfo : ProductionUnit
    {
        public double MaxElectricity { get; private set; }
        public ElectricBoilerInfo(string ShortName, string Name, double MaxHeat, double MaxElectricity, decimal ProductoinCost )
        : base(ShortName, Name, MaxHeat, ProductionCosts, null)
        {
            this.MaxElectricity = MaxElectricity;
        }
    }
        public class AssetManager
        {
    
           public List<ProductionUnit> Units { get; } = new List<ProductionUnit>
        {
            new GasBoilerInfo("GB1", "Gas Boiler 1", 3.0 , 510, 132, 1.05),
            new GasBoilerInfo("GB2", "Gas Boiler 2", 2.0 , 540, 134, 1.08),
            new GasBoilerInfo("GB3", "Gas Boiler 3", 4.0 , 580, 136, 1.09),
            new OilBoilerInfo("OB1", "Oil Boiler 1", 6.0, 690, 147, 1.18),
            new GasMotorInfo("GM1", "Gas Motor 1", 5.3, 3.9, 975, 227, 1.82),
            new ElectricBoilerInfo("EB1", "Electric Boiler", 6.0, -6.0, 15)
        };


        }

    



