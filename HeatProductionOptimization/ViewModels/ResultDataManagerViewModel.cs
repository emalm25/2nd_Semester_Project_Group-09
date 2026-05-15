using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HeatProductionOptimization.Models;
using HeatProductionOptimization.Services;
using System.Collections.ObjectModel;

namespace HeatProductionOptimization.ViewModels;

public partial class ResultDataManagerViewModel : ObservableObject
{
    private readonly ResultDataManager resultDataManager;

    public ObservableCollection<OptimizationResult> Results => resultDataManager.Results;

    [ObservableProperty]
    private OptimizationResult? selectedResult;

    [ObservableProperty]
    private string statusMessage = "";

    public ResultDataManagerViewModel(ResultDataManager resultDataManager)
    {
        this.resultDataManager = resultDataManager;
    }

    [RelayCommand]
    private void RemoveSelected()
    {
        if (SelectedResult != null)
        {
            resultDataManager.RemoveResult(SelectedResult);
            StatusMessage = "Selected result removed.";
        }
        else
        {
            StatusMessage = "No result selected.";
        }
    }

    [RelayCommand]
    private void ClearAll()
    {
        resultDataManager.ClearResults();
        StatusMessage = "All results removed.";
    }

    [RelayCommand]
    private void Save()
    {
        resultDataManager.SaveResults();
        StatusMessage = "Results saved successfully.";
    }

    [RelayCommand]
    private void Load()
    {
        resultDataManager.LoadResults();
        StatusMessage = "Results loaded successfully.";
    }
}