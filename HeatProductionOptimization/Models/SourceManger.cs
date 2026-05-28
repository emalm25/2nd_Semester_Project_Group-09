using System;
using System.Collections.Generic;
using CsvHandler;
using System.Data;
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
            var winterPath = Path.Combine(AppContext.BaseDirectory, "SourceWinterData.json");
            return JsonReader.JsonReadHandle(winterPath);
        }

        public List<Data> GetSummerData()
        {
            var summerPath = Path.Combine(AppContext.BaseDirectory, "SourceSummerData.json");
            return JsonReader.JsonReadHandle(summerPath);
        }

    }