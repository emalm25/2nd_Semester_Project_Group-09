namespace HeatProductionOptimization.ViewModels;
using HeatProductionOptimization.Services;

public partial class MainWindowViewModel : ViewModelBase
{
    public AssetManagerViewModel AssetManagerViewModel { get; }
    private readonly ResultDataManager resultDataManager;
    public ResultDataManagerViewModel ResultDataManagerVM { get; }
    public OptimizerViewModel OptimizerVM { get; }

    public MainWindowViewModel()
    {
        AssetManagerViewModel = new AssetManagerViewModel();
        resultDataManager = new ResultDataManager();
        ResultDataManagerVM = new ResultDataManagerViewModel(resultDataManager);
        OptimizerVM = new OptimizerViewModel(new OptimizerService(), resultDataManager);
    }

}

