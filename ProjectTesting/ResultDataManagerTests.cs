using Xunit;
using Avalonia;
using Avalonia.Headless.XUnit;
using HeatProductionOptimization.ViewModels;
using HeatProductionOptimization.Services;
using HeatProductionOptimization.Models;

namespace ProjectTesting;

public class ResultDataManagerTests
{
    private readonly ResultDataManagerViewModel _viewModel;
    private readonly ResultDataManager _resultDataManager;

    public ResultDataManagerTests()
    {
        _resultDataManager = new ResultDataManager();
        _viewModel = new ResultDataManagerViewModel(_resultDataManager);
    }



    [Fact]
    public void InitialResults_ShouldBeEmpty()
    {
        // Assert
        Assert.NotNull(_viewModel.Results);
        Assert.Empty(_viewModel.Results);
    }


    [Fact]
    public void RemoveSelected_ShouldRemoveResult()
    {
        // Arrange
        var result = new OptimizationResult { Objective = OptimizationResult.ObjectiveType.Cost };
        _resultDataManager.AddResult(result);
        _viewModel.SelectedResult = result;

        // Act
        _viewModel.RemoveSelectedCommand.Execute(null);

        // Assert
        Assert.Empty(_viewModel.Results);
        Assert.Equal("Selected result removed.", _viewModel.StatusMessage);
    }


    [Fact]
    public void ClearAll_ShouldRemoveAllResults()
    {
        // Arrange
        _resultDataManager.AddResult(new OptimizationResult { Objective = OptimizationResult.ObjectiveType.Cost });
        _resultDataManager.AddResult(new OptimizationResult { Objective = OptimizationResult.ObjectiveType.CO2 });

        // Act
        _viewModel.ClearAllCommand.Execute(null);

        // Assert
        Assert.Empty(_viewModel.Results);
        Assert.Equal("All results removed.", _viewModel.StatusMessage);
    }


    [Fact]
    public void Save_ShouldSaveAllResults()
    {
        // Arrange
        var result1 = new OptimizationResult { Objective = OptimizationResult.ObjectiveType.Cost };
        var result2 = new OptimizationResult { Objective = OptimizationResult.ObjectiveType.CO2 };
        _resultDataManager.AddResult(result1);
        _resultDataManager.AddResult(result2);

        // Act
        _viewModel.SaveCommand.Execute(null);

        // Assert
        Assert.Equal(2, _viewModel.Results.Count);
        Assert.Contains(result1, _viewModel.Results);
        Assert.Contains(result2, _viewModel.Results);
        Assert.Equal("Results saved successfully.", _viewModel.StatusMessage);
    }


    [Fact]
    public void Load_ShouldLoadSavedResults()
    {
        // Arrange
        var result1 = new OptimizationResult { Objective = OptimizationResult.ObjectiveType.Cost, TotalCost = 100 };
        var result2 = new OptimizationResult { Objective = OptimizationResult.ObjectiveType.CO2, TotalCo2 = 50 };
        _resultDataManager.AddResult(result1);
        _resultDataManager.AddResult(result2);
        _viewModel.SaveCommand.Execute(null);
        _resultDataManager.ClearResults();
        Assert.Empty(_viewModel.Results);

        // Act
        _viewModel.LoadCommand.Execute(null);

        // Assert
        Assert.Equal(2, _viewModel.Results.Count);
        Assert.Equal("Results loaded successfully.", _viewModel.StatusMessage);
    }
}