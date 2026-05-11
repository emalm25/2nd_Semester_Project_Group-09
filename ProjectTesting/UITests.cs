using Avalonia;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using HeatProductionOptimization.Services;
using HeatProductionOptimization.ViewModels;
using HeatProductionOptimization.Views;

[assembly: AvaloniaTestApplication(typeof(ProjectTesting.UITestAppBuilder))]

namespace ProjectTesting;

public static class UITestAppBuilder
{
    /// Provides the Avalonia application builder used by Avalonia headless tests.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<HeatProductionOptimization.App>()
            .UseHeadless(new AvaloniaHeadlessPlatformOptions
            {
                UseHeadlessDrawing = true
            });
}

public class UITests
{
    [AvaloniaFact]
    public void SourceDataView_ShouldSetSourceDataViewModel_OnConstruction()
        /// Verifies that SourceDataView initializes with SourceDataViewModel as its DataContext.
    {
        // Arrange
        var view = new SourceDataView();

        // Act
        var dataContext = Assert.IsType<SourceDataViewModel>(view.DataContext);

        // Assert
        Assert.NotNull(dataContext);
    }

    [AvaloniaFact]
    public void MainWindow_DataContext_ShouldAcceptMainWindowViewModel()
        /// Verifies that MainWindow accepts MainWindowViewModel and exposes required child view models.
    {
        // Arrange
        var window = new MainWindow();
        var viewModel = new MainWindowViewModel();

        // Act
        window.DataContext = viewModel;

        // Assert
        var dataContext = Assert.IsType<MainWindowViewModel>(window.DataContext);
        Assert.NotNull(dataContext.AssetManagerViewModel);
        Assert.NotNull(dataContext.ResultDataManagerVM);
        Assert.NotNull(dataContext.OptimizerVM);
    }

    [AvaloniaFact]
    public void OptimizerView_DefaultMaintenanceSeason_ShouldBeSummer()
        /// Verifies that OptimizerViewModel defaults maintenance season selection to Summer.
    {
        // Arrange
        using var viewModel = CreateOptimizerViewModel();
        var view = new OptimizerView { DataContext = viewModel };

        // Act
        var isSummerSelected = viewModel.IsMaintenanceSummerSelected;
        var isWinterSelected = viewModel.IsMaintenanceWinterSelected;

        // Assert
        Assert.True(isSummerSelected);
        Assert.False(isWinterSelected);
        Assert.Equal(viewModel, view.DataContext);
    }

    [AvaloniaFact]
    public void OptimizerView_Optimize_ShouldPopulateMaintenanceInfoText()
        /// Verifies that running optimization populates maintenance information including duration.

    {
        // Arrange
        using var viewModel = CreateOptimizerViewModel();
        var view = new OptimizerView { DataContext = viewModel };

        // Act
        viewModel.SetObjectiveCommand.Execute("CO2");
        viewModel.SetSeasonCommand.Execute("Winter");
        viewModel.SetScenarioCommand.Execute("Scenario 2");
        viewModel.SetMaintenanceSeasonCommand.Execute("Winter");
        viewModel.OptimizeCommand.Execute(null);

        // Assert
        Assert.False(string.IsNullOrWhiteSpace(viewModel.MaintenanceInfoText));
        Assert.Contains("Unit in maintenance:", viewModel.MaintenanceInfoText);
        Assert.Contains("(48h)", viewModel.MaintenanceInfoText);
        Assert.Equal(viewModel, view.DataContext);
    }

    private static OptimizerViewModel CreateOptimizerViewModel()
        /// Creates an OptimizerViewModel instance with its required service and view model dependencies.

    {
        // Arrange
        var assetManager = new AssetManager();
        var assetManagerViewModel = new AssetManagerViewModel(assetManager);
        var resultDataManager = new ResultDataManager();
        var resultDataManagerViewModel = new ResultDataManagerViewModel(resultDataManager);

        // Act
        var viewModel = new OptimizerViewModel(
            new OptimizerService(assetManager),
            resultDataManager,
            resultDataManagerViewModel,
            assetManagerViewModel);

        // Assert
        Assert.NotNull(viewModel);
        return viewModel;
    }
}