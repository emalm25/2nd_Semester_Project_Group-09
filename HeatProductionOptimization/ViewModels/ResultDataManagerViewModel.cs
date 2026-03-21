using CommunityToolkit.Mvvm.ComponentModel;
using HeatProductionOptimization.Models;
using HeatProductionOptimization.Services;
using System.Collections.ObjectModel;

namespace HeatProductionOptimization.ViewModels;

public partial class ResultDataManagerViewModel : ObservableObject
{
    private readonly ResultDataManager resultDataManager;

    public ObservableCollection<OptimizationResult> Results => resultDataManager.Results;

    public ResultDataManagerViewModel(ResultDataManager resultDataManager)
    {
        this.resultDataManager = resultDataManager;
    }
}