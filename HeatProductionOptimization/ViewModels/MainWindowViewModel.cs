namespace HeatProductionOptimization.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    public string Greeting { get; } = "Welcome to Avalonia!";
    
    public AssetManager AssetManager { get; } = new AssetManager();
}

