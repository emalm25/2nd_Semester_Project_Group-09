using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace HeatProductionOptimization.Views;

public partial class OptimizerView : UserControl
{
	public OptimizerView()
	{
		InitializeComponent();
	}

	private void InitializeComponent()
	{
		AvaloniaXamlLoader.Load(this);
	}
}
