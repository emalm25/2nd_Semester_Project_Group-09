using Avalonia.Controls;
using HeatProductionOptimization.ViewModels;

namespace HeatProductionOptimization.Views;

public partial class SourceDataView : UserControl
{
    public SourceDataView()
    {
        InitializeComponent();
        DataContext = new SourceDataViewModel();
    }
}
