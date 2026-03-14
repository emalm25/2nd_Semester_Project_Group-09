using System;
using System.Collections.Generic;
using CsvHandler;
using System.Data;
using Microsoft.VisualBasic;
using System.Globalization;
using System.IO;

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
  public class SourceManager
    {
        public List<Data> GetWinterData()
        {
            return Csvreader.CSVReadHandle("SourceWinterData.csv");
        }

        public List<Data> GetSummerData()
        {
            return Csvreader.CSVReadHandle("SourceSummerData.csv");
        }
        
    }