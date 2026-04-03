using System;
using System.Collections.Generic;
using System.Linq;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;

namespace HeatProductionOptimization.ViewModels;

public class SourceDataViewModel : ViewModelBase
{
    private readonly SourceManager manager;

    private List<Data> data;
    private string selectedPeriod = "Winter";

    private ISeries[] heatDemandSeries = Array.Empty<ISeries>();
    private ISeries[] electricityPriceSeries = Array.Empty<ISeries>();
    private Axis[] timeAxes = Array.Empty<Axis>();
    private Axis[] heatDemandYAxes = Array.Empty<Axis>();
    private Axis[] electricityPriceYAxes = Array.Empty<Axis>();

    private double averageHeatDemand;
    private double minHeatDemand;
    private double maxHeatDemand;

    private double averageElectricityPrice;
    private double minElectricityPrice;
    private double maxElectricityPrice;

    public List<string> Periods { get; } = new() { "Winter", "Summer" };

    public List<Data> Data
    {
        get => data;
        set => SetProperty(ref data, value);
    }

    public ISeries[] HeatDemandSeries
    {
        get => heatDemandSeries;
        set => SetProperty(ref heatDemandSeries, value);
    }

    public ISeries[] ElectricityPriceSeries
    {
        get => electricityPriceSeries;
        set => SetProperty(ref electricityPriceSeries, value);
    }

    public Axis[] TimeAxes
    {
        get => timeAxes;
        set => SetProperty(ref timeAxes, value);
    }

    public Axis[] HeatDemandYAxes
    {
        get => heatDemandYAxes;
        set => SetProperty(ref heatDemandYAxes, value);
    }

    public Axis[] ElectricityPriceYAxes
    {
        get => electricityPriceYAxes;
        set => SetProperty(ref electricityPriceYAxes, value);
    }

    public double AverageHeatDemand
    {
        get => averageHeatDemand;
        set => SetProperty(ref averageHeatDemand, value);
    }

    public double MinHeatDemand
    {
        get => minHeatDemand;
        set => SetProperty(ref minHeatDemand, value);
    }

    public double MaxHeatDemand
    {
        get => maxHeatDemand;
        set => SetProperty(ref maxHeatDemand, value);
    }

    public double AverageElectricityPrice
    {
        get => averageElectricityPrice;
        set => SetProperty(ref averageElectricityPrice, value);
    }

    public double MinElectricityPrice
    {
        get => minElectricityPrice;
        set => SetProperty(ref minElectricityPrice, value);
    }

    public double MaxElectricityPrice
    {
        get => maxElectricityPrice;
        set => SetProperty(ref maxElectricityPrice, value);
    }

    public string SelectedPeriod
    {
        get => selectedPeriod;
        set
        {
            if (!SetProperty(ref selectedPeriod, value))
                return;

            Data = selectedPeriod == Periods[0]
                ? manager.GetWinterData()
                : manager.GetSummerData();

            UpdateChartsAndKpis();
        }
    }

    public SourceDataViewModel()
    {
        manager = new SourceManager();
        data = new List<Data>();
        SelectedPeriod = Periods[0];
    }

    private void UpdateChartsAndKpis()
    {
        if (Data.Count == 0)
        {
            HeatDemandSeries = Array.Empty<ISeries>();
            ElectricityPriceSeries = Array.Empty<ISeries>();
            TimeAxes = Array.Empty<Axis>();
            HeatDemandYAxes = Array.Empty<Axis>();
            ElectricityPriceYAxes = Array.Empty<Axis>();

            AverageHeatDemand = 0;
            MinHeatDemand = 0;
            MaxHeatDemand = 0;
            AverageElectricityPrice = 0;
            MinElectricityPrice = 0;
            MaxElectricityPrice = 0;
            return;
        }

        var heatValues = Data.Select(d => d.HeatDemand).ToArray();
        var priceValues = Data.Select(d => d.ElectricityPrice).ToArray();
        var labels = Data.Select(d => d.StartTime.ToString("dd/MM HH:mm")).ToArray();

        // Determinar colores según el período
        SKColor heatColor = selectedPeriod == Periods[0] 
            ? SKColors.RoyalBlue      // Winter: Azul
            : SKColors.OrangeRed;     // Summer: Naranjado

        SKColor priceColor = selectedPeriod == Periods[0]
            ? SKColors.ForestGreen    // Winter: Verde
            : SKColors.Gold;          // Summer: Amarillo

        HeatDemandSeries = new ISeries[]
        {
            new LineSeries<double>
            {
                Name = "Heat Demand",
                Values = heatValues,
                Stroke = new SolidColorPaint(heatColor) { StrokeThickness = 2 },
                Fill = null,
                GeometrySize = 0
            }
        };

        ElectricityPriceSeries = new ISeries[]
        {
            new LineSeries<double>
            {
                Name = "Electricity Price",
                Values = priceValues,
                Stroke = new SolidColorPaint(priceColor) { StrokeThickness = 2 },
                Fill = null,
                GeometrySize = 0
            }
        };

        TimeAxes = new Axis[]
        {
            new Axis
            {
                Labels = labels,
                LabelsRotation = 35,
                TextSize = 11,
                LabelsPaint = new SolidColorPaint(SKColors.Black),
                NamePaint = new SolidColorPaint(SKColors.Black)
            }
        };

        HeatDemandYAxes = new Axis[]
        {
            new Axis 
            { 
                Name = "MW", 
                TextSize = 11,
                LabelsPaint = new SolidColorPaint(SKColors.Black),
                NamePaint = new SolidColorPaint(SKColors.Black)
            }
        };

        ElectricityPriceYAxes = new Axis[]
        {
            new Axis 
            { 
                Name = "EUR/MWh", 
                TextSize = 11,
                LabelsPaint = new SolidColorPaint(SKColors.Black),
                NamePaint = new SolidColorPaint(SKColors.Black)
            }
        };

        AverageHeatDemand = Math.Round(heatValues.Average(), 2);
        MinHeatDemand = Math.Round(heatValues.Min(), 2);
        MaxHeatDemand = Math.Round(heatValues.Max(), 2);

        AverageElectricityPrice = Math.Round(priceValues.Average(), 2);
        MinElectricityPrice = Math.Round(priceValues.Min(), 2);
        MaxElectricityPrice = Math.Round(priceValues.Max(), 2);
    }
}
