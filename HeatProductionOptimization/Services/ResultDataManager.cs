using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using HeatProductionOptimization.Models;

namespace HeatProductionOptimization.Services;

public class ResultDataManager
{
    private const string FilePath = "results.json";
    public ObservableCollection<OptimizationResult> Results { get; } = new();

    public void AddResult(OptimizationResult result)
    {
        if (result != null)
        {
            Results.Add(result);
        }
    }

    public void RemoveResult(OptimizationResult result)
    {
        if (result != null && Results.Contains(result))
        {
            Results.Remove(result);
        }
    }

    public void ClearResults()
    {
        Results.Clear();
    }
    public void SaveResults()
    {
        var json = JsonSerializer.Serialize(Results);
        File.WriteAllText(FilePath, json);
    }

    public void LoadResults()
    {
        if (!File.Exists(FilePath))
            return;

        var json = File.ReadAllText(FilePath);
        var loadedResults = JsonSerializer.Deserialize<List<OptimizationResult>>(json);

        Results.Clear();

        if (loadedResults != null)
        {
            foreach (var result in loadedResults)
            {
                Results.Add(result);
            }
        }
    }


}