using System;
using System.IO;
using System.Linq;
using Xunit;
using Avalonia.Headless.XUnit;
using HeatProductionOptimization.ViewModels;
using System.Reflection;
using System.Reflection.Metadata;
using System.Runtime.CompilerServices;

namespace ProjectTesting;

public class SourceDataManagerTests
{
    private readonly SourceDataViewModel _viewModel;

    public SourceDataManagerTests()
    {
        _viewModel = new SourceDataViewModel();
    }

    // Verifies that the winter file exists before any loading logic runs.
    // This catches missing build outputs or incorrect file paths early.
    // The test only checks file availability, not content.
    [Fact]
    public void WinterFile_ShouldExist()
    {
        string winterPath = Path.Combine(AppContext.BaseDirectory, "SourceWinterData.json");
        
        bool exists = File.Exists(winterPath);

        Assert.True(exists);
    }

    // Verifies that the summer file exists and is available for reading.
    // This ensures the Summer dataset can be accessed in the test runtime.
    // If this fails, the loading tests will not be able to run reliably.
    [Fact]
    public void SummerFile_ShouldExist()
    {
        string summerPath = Path.Combine(AppContext.BaseDirectory, "SourceSummerData.json");

        bool exists = File.Exists(summerPath);

        Assert.True(exists);
    }

    // Confirms that the ViewModel loads valid data for the default Winter period.
    // The constructor starts the ViewModel in Winter, so this checks the default state.
    // It validates that the loaded collection is not empty.
    [Fact]
    public void GetWinterData_ShouldLoadData()
    {
        Assert.NotNull(_viewModel.Data);

        Assert.True(_viewModel.Data.Count > 0);
    }

    // Confirms that switching to Summer replaces the displayed data in the ViewModel.
    // This proves the SelectedPeriod setter triggers a new load.
    // The test also verifies that the Summer collection is not empty.
    [Fact]
    public void GetSummerData_ShouldLoadData()
    {
        _viewModel.SelectedPeriod = "Summer";

        Assert.NotNull(_viewModel.Data);

        Assert.True(_viewModel.Data.Count > 0);
    }

    // Checks that each loaded record contains valid and consistent values.
    // Electricity price must stay positive because negative values are invalid here.
    // Heat demand can be zero, but it should never be negative.
    [Fact]
    public void Data_ShouldContainValidValues()
    {
        Assert.All(_viewModel.Data, item =>
        {
            Assert.True(item.ElectricityPrice > 0);
            Assert.True(item.HeatDemand >= 0);
        });
    }

    // Ensures that toggling between Winter and Summer actually changes the visible dataset.
    // This is the most visible behavior in the UI and the main switch to validate.
    // Comparing the first timestamp is a simple way to confirm the data changed.
    [AvaloniaFact]
    public void SelectedPeriod_ShouldSwitchDisplayedData()
    {
        var winterData = _viewModel.Data.ToList();

        _viewModel.SelectedPeriod = "Summer";
        var summerData = _viewModel.Data.ToList();

        Assert.NotEmpty(winterData);
        Assert.NotEmpty(summerData);

        Assert.NotEqual(winterData[0].StartTime, summerData[0].StartTime);
    }
}
