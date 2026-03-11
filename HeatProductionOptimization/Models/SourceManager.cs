using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.VisualBasic;

public class Data
{
    public DateTime StartTime {get; private set;}
    public DateTime EndTime {get; private set;}
    public double HeatDemand {get; private set;}
    public double ElectricityPrice {get; private set;}

    public Data(DateTime StartTime, DateTime EndTime, double HeatDemand, double ElectricityPrice)
    {
        this.StartTime = StartTime;
        this.EndTime = EndTime;
        this.HeatDemand = HeatDemand;
        this.ElectricityPrice = ElectricityPrice;
    }


}
