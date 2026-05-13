using Avalonia.Controls;
using Avalonia.VisualTree;
using HeatProductionOptimization.Services;
using HeatProductionOptimization.ViewModels;
using HeatProductionOptimization.Views;

namespace ProjectTesting;

public class OptimizerTests
{
	//Verifies that running the optimizer produces a new result
	//Executes the same commands a user would: set objective/season/scenario/maintenance
	//Calls OptimizeCommand and asserts a result is added and selected
	//Checks that summary and status message are populated
	[AvaloniaFact]
	public void OptimizerViewModel_OptimizeCommand_ShouldCompleteAndAddAResult()
	{
		using var viewModel = CreateOptimizerViewModel();

		var initialResultCount = viewModel.Results.Count;

		viewModel.SetObjectiveCommand.Execute("CO2");
		viewModel.SetSeasonCommand.Execute("Winter");
		viewModel.SetScenarioCommand.Execute("Scenario 2");
		viewModel.SetMaintenanceSeasonCommand.Execute("Winter");

		// Runs optimization, makes sure it doesn't crash and  adds a new result
		viewModel.OptimizeCommand.Execute(null);

		// Verifies that a new result was added and selected
		Assert.Equal(initialResultCount + 1, viewModel.Results.Count);
		Assert.NotNull(viewModel.SelectedResult);

		// Verifies that the view model updated its user-visible messages
		Assert.False(string.IsNullOrWhiteSpace(viewModel.LastOptimizationMessage));
		Assert.Contains("Selected:", viewModel.OptimizationSummary);
		Assert.Contains("Unit in maintenance:", viewModel.MaintenanceInfoText);
	}

	// Inspects the control tree of the OptimizerView and ensures all expected
	// button labels exist. This checks that the UI contains the expected interactive
	// controls (objective, season, maintenance, scenario, and optimize action).
	[AvaloniaFact]
	public void OptimizerView_ShouldContainAllExpectedButtons()
	{
		using var viewModel = CreateOptimizerViewModel();
		var view = new OptimizerView { DataContext = viewModel };

		// Collect textual labels from buttons found in the view's content tree
		var buttonLabels = GetButtonsFromView(view)
			.Select(button => button.Content?.ToString())
			.Where(label => !string.IsNullOrWhiteSpace(label))
			.ToArray();

		// Expectation notes:
		//There are two seasonal buttons for each of Season and Maintenance (Winter/Summer)
		//One button for each Objective option and each Scenario option
		//One primary Optimize button
		Assert.Equal(9, buttonLabels.Length);
		Assert.Equal(1, buttonLabels.Count(label => string.Equals(label, "Cost", StringComparison.Ordinal)));
		Assert.Equal(1, buttonLabels.Count(label => string.Equals(label, "CO2", StringComparison.Ordinal)));
		Assert.Equal(2, buttonLabels.Count(label => string.Equals(label, "Winter", StringComparison.Ordinal)));
		Assert.Equal(2, buttonLabels.Count(label => string.Equals(label, "Summer", StringComparison.Ordinal)));
		Assert.Equal(1, buttonLabels.Count(label => string.Equals(label, "Scenario 1", StringComparison.Ordinal)));
		Assert.Equal(1, buttonLabels.Count(label => string.Equals(label, "Scenario 2", StringComparison.Ordinal)));
		Assert.Equal(1, buttonLabels.Count(label => string.Equals(label, "Optimize", StringComparison.Ordinal)));
	}

	//Verifies that the Optimize button is wired to the view model's command
	//Finds the button by content text and compares the Command reference
	[AvaloniaFact]
	public void OptimizerView_OptimizeButton_ShouldBeBoundToOptimizeCommand()
	{
		using var viewModel = CreateOptimizerViewModel();
		var view = new OptimizerView { DataContext = viewModel };

		var optimizeButton = GetButtonsFromView(view)
			.First(button => string.Equals(button.Content?.ToString(), "Optimize", StringComparison.Ordinal));

		// Ensures the button's Command reference is the same instance used by the VM
		Assert.Same(viewModel.OptimizeCommand, optimizeButton.Command);
	}

	private static IReadOnlyList<Button> GetButtonsFromView(UserControl view)
	{
		var buttons = new List<Button>();
		CollectButtons(view.Content as Control, buttons);
		return buttons;
	}

	private static void CollectButtons(Control? control, ICollection<Button> buttons)
	{
		if (control is null)
		{
			return;
		}

		if (control is Button button)
		{
			buttons.Add(button);
			return;
		}

		// Decorator and ContentControl commonly wrap single child content
		if (control is Decorator decorator && decorator.Child is Control decoratorChild)
		{
			CollectButtons(decoratorChild, buttons);
		}

		if (control is ContentControl contentControl && contentControl.Content is Control contentChild)
		{
			CollectButtons(contentChild, buttons);
		}

		// Panels contain child collections we should scan
		if (control is Panel panel)
		{
			foreach (var child in panel.Children.OfType<Control>())
			{
				CollectButtons(child, buttons);
			}
		}
	}

	// Constructs a minimal OptimizerViewModel with its required
	// dependencies so tests can exercise commands and state.
	private static OptimizerViewModel CreateOptimizerViewModel()
	{
		var assetManager = new AssetManager();
		var assetManagerViewModel = new AssetManagerViewModel(assetManager);
		var resultDataManager = new ResultDataManager();
		var resultDataManagerViewModel = new ResultDataManagerViewModel(resultDataManager);

		return new OptimizerViewModel(
			new OptimizerService(assetManager),
			resultDataManager,
			resultDataManagerViewModel,
			assetManagerViewModel);
	}
}
