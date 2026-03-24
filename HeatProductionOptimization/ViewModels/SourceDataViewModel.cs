using System.Collections.Generic;

namespace HeatProductionOptimization.ViewModels;

public class SourceDataViewModel : ViewModelBase
{
private readonly SourceManager manager;

private List<Data> data;
<<<<<<< Updated upstream
private string selectedPeriod = string.Empty;
=======
<<<<<<< Updated upstream
private string selectedPeriod;
=======
private string selectedPeriod = "Winter";
>>>>>>> Stashed changes
>>>>>>> Stashed changes

public List<string> Periods { get; } = new() { "Winter", "Summer" };

public List<Data> Data
{
    get => data;
    set => SetProperty(ref data, value);
}

public string SelectedPeriod
{
    get => selectedPeriod;
    set
    {
        if (SetProperty(ref selectedPeriod, value))
        {
            if (selectedPeriod == Periods[0])
            {
               Data = manager.GetWinterData();
            }
            else if (selectedPeriod == Periods[1])
            {
                Data = manager.GetSummerData();
            }
        }
    }
}

    public SourceDataViewModel()
    {
        manager = new SourceManager();
        data =  new List<Data>();
        SelectedPeriod = Periods[0];
    }

}

