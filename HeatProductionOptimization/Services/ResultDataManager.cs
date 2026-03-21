using System.Collections.ObjectModel;
using HeatProductionOptimization.Models;

namespace HeatProductionOptimization.Services;

public class ResultDataManager
{
    public ObservableCollection<OptimizationResult> Results { get; } = new();

    public void AddResult(OptimizationResult result)
    {
        Results.Add(result);
    }

    public void ClearResults()
    {
        Results.Clear();
    }
}