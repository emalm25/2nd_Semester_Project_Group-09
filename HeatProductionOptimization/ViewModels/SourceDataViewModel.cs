using System;
using System.Collections.Generic;
using HeatProductionOptimization.ViewModels;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AvaloniaApplication1.ViewModels;

public class SourceDataViewModel : ViewModelBase
{
SourceManager manager;

private List<Data> data;
private string selectedPeriod;

public List<string> Periods { get; } = new() { "Winter", "Summer" };

public List<Data> Data
{
    get => data;
    set => SetProperty(ref data, value);
}

public string SelectedPeriod
{
    get => selectedPeriod;
    set
    {
        if (SetProperty(ref selectedPeriod, value))
        {
            if (selectedPeriod == Periods[0])
            {
               Data = manager.GetWinterData();
            }
            else if (selectedPeriod == Periods[1])
            {
                Data = manager.GetSummerData();
            }
        }
    }
}

    public SourceDataViewModel()
    {
        manager = new SourceManager();
        data =  new List<Data>();
        selectedPeriod = "Winter";
    }

}

