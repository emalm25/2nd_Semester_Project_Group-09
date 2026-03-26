namespace HeatProductionOptimization.ViewModels;
using HeatProductionOptimization.Services;

using CommunityToolkit.Mvvm.Input;

public partial class MainWindowViewModel : ViewModelBase
{
    public AssetManagerViewModel AssetManagerViewModel { get; }
    private readonly ResultDataManager resultDataManager;
    public ResultDataManagerViewModel ResultDataManagerVM { get; }


    private string selectedObjective = "Cost";
    private string selectedSeason = "Winter";
    private string selectedScenario = "Scenario 1";
    private string lastOptimizationMessage = "Ready to optimize.";

    public string SelectedObjective
    {
        get => selectedObjective;
        set
        {
            if (SetProperty(ref selectedObjective, value))
            {
                OnPropertyChanged(nameof(IsCostSelected));
                OnPropertyChanged(nameof(IsCo2Selected));
                OnPropertyChanged(nameof(OptimizationSummary));
                OptimizeCommand.NotifyCanExecuteChanged();
            }
        }
    }

    public string SelectedSeason
    {
        get => selectedSeason;
        set
        {
            if (SetProperty(ref selectedSeason, value))
            {
                OnPropertyChanged(nameof(IsWinterSelected));
                OnPropertyChanged(nameof(IsSummerSelected));
                OnPropertyChanged(nameof(OptimizationSummary));
                OptimizeCommand.NotifyCanExecuteChanged();
            }
        }
    }

    public string SelectedScenario
    {
        get => selectedScenario;
        set
        {
            if (SetProperty(ref selectedScenario, value))
            {
                OnPropertyChanged(nameof(IsScenario1Selected));
                OnPropertyChanged(nameof(IsScenario2Selected));
                OnPropertyChanged(nameof(OptimizationSummary));
                OptimizeCommand.NotifyCanExecuteChanged();
            }
        }
    }

    public bool IsCostSelected => SelectedObjective == "Cost";

    public bool IsCo2Selected => SelectedObjective == "CO2";

    public bool IsWinterSelected => SelectedSeason == "Winter";

    public bool IsSummerSelected => SelectedSeason == "Summer";

    public bool IsScenario1Selected => SelectedScenario == "Scenario 1";

    public bool IsScenario2Selected => SelectedScenario == "Scenario 2";

    public string OptimizationSummary =>
        $"Current setup: {SelectedObjective}, {SelectedSeason}, {SelectedScenario}";

    public string LastOptimizationMessage
    {
        get => lastOptimizationMessage;
        set => SetProperty(ref lastOptimizationMessage, value);
    }

    public IRelayCommand<string> SetObjectiveCommand { get; }

    public IRelayCommand<string> SetSeasonCommand { get; }

    public IRelayCommand<string> SetScenarioCommand { get; }

    public IRelayCommand OptimizeCommand { get; }

    public MainWindowViewModel()
    {
        AssetManagerViewModel = new AssetManagerViewModel();
        resultDataManager = new ResultDataManager();
        ResultDataManagerVM = new ResultDataManagerViewModel(resultDataManager);

        SetObjectiveCommand = new RelayCommand<string>(SetObjective);
        SetSeasonCommand = new RelayCommand<string>(SetSeason);
        SetScenarioCommand = new RelayCommand<string>(SetScenario);
        OptimizeCommand = new RelayCommand(RunOptimization, CanOptimize);
    }

    private void SetObjective(string? objective)
    {
        if (!string.IsNullOrWhiteSpace(objective))
        {
            SelectedObjective = objective;
        }
    }

    private void SetSeason(string? season)
    {
        if (!string.IsNullOrWhiteSpace(season))
        {
            SelectedSeason = season;
        }
    }

    private void SetScenario(string? scenario)
    {
        if (!string.IsNullOrWhiteSpace(scenario))
        {
            SelectedScenario = scenario;
        }
    }

    private bool CanOptimize()
    {
        return !string.IsNullOrWhiteSpace(SelectedObjective)
               && !string.IsNullOrWhiteSpace(SelectedSeason)
               && !string.IsNullOrWhiteSpace(SelectedScenario);
    }

    private void RunOptimization()
    {
        LastOptimizationMessage =
            $"Optimization started with {SelectedObjective}, {SelectedSeason}, {SelectedScenario}.";
    }

}

