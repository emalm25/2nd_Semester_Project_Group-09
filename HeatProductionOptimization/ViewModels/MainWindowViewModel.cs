namespace HeatProductionOptimization.ViewModels;
using HeatProductionOptimization.Services;

public partial class MainWindowViewModel : ViewModelBase
{
    public AssetManagerViewModel AssetManagerViewModel { get; }
    private readonly ResultDataManager resultDataManager;
    public ResultDataManagerViewModel ResultDataManagerVM { get; }
    public OptimizerViewModel OptimizerVM { get; }
    private readonly AssetManager assetManager;

    public MainWindowViewModel()
    {
        assetManager = new AssetManager();
        AssetManagerViewModel = new AssetManagerViewModel(assetManager);
        resultDataManager = new ResultDataManager();
        ResultDataManagerVM = new ResultDataManagerViewModel(resultDataManager);
        OptimizerVM = new OptimizerViewModel(
            new OptimizerService(assetManager),
            resultDataManager,
            ResultDataManagerVM,
            AssetManagerViewModel);
    }

}

