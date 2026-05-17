using System.Linq;
using Xunit;
using HeatProductionOptimization.ViewModels;

namespace ProjectTesting;

public class AssetManagerTests
{
    private AssetManager CreateAssetManager()
    {
        return new AssetManager();
    }

    [Fact]
    public void AssetManagerLoadUnits()
    {
        var assetManager = CreateAssetManager();

        Assert.NotNull(assetManager.Units);
        Assert.True(assetManager.Units.Count > 0);
    }

    [Fact]
    public void AddNewUnitGasBoilerAdd()
    {
        var assetManager = CreateAssetManager();
        var viewModel = new AssetManagerViewModel(assetManager);

        int modelCountBefore = assetManager.Units.Count;
        int viewModelCountBefore = viewModel.Units.Count;

        viewModel.AddNewUnit("GasBoiler");

        Assert.Equal(modelCountBefore + 1, assetManager.Units.Count);
        Assert.Equal(viewModelCountBefore + 1, viewModel.Units.Count);
        Assert.Contains(assetManager.Units, unit => unit.ShortName == "GB_NEW");
        Assert.Contains(viewModel.Units, unit => unit.ShortName == "GB_NEW");
    }

    [Fact]
    public void AddNewUnitOilBoilerAdd()
    {
        var assetManager = CreateAssetManager();
        var viewModel = new AssetManagerViewModel(assetManager);

        viewModel.AddNewUnit("OilBoiler");

        var addedUnit = assetManager.Units.Last();

        Assert.IsType<OilBoilerInfo>(addedUnit);
        Assert.Equal("OB_NEW", addedUnit.ShortName);
        Assert.Equal("New Oil Boiler", addedUnit.Name);
    }

    [Fact]
    public void AddNewUnitGasMotorAdd()
    {
        var assetManager = CreateAssetManager();
        var viewModel = new AssetManagerViewModel(assetManager);

        viewModel.AddNewUnit("GasMotor");

        var addedUnit = assetManager.Units.Last();

        Assert.IsType<GasMotorInfo>(addedUnit);
        Assert.Equal("GM_NEW", addedUnit.ShortName);
        Assert.True(addedUnit.MaxElectricity > 0);
    }

    [Fact]
    public void AddNewUnitElectricBoilerAdd()
    {
        var assetManager = CreateAssetManager();
        var viewModel = new AssetManagerViewModel(assetManager);

        viewModel.AddNewUnit("ElectricBoiler");

        var addedUnit = assetManager.Units.Last();

        Assert.IsType<ElectricBoilerInfo>(addedUnit);
        Assert.Equal("EB_NEW", addedUnit.ShortName);
        Assert.Equal("New Electric Boiler", addedUnit.Name);
        Assert.True(addedUnit.MaxElectricity > 0);
    }

    [Fact]
    public void DeleteUnitRemoveUnit()
    {
        var assetManager = CreateAssetManager();
        var viewModel = new AssetManagerViewModel(assetManager);

        viewModel.AddNewUnit("GasBoiler");

        var unitToDelete = viewModel.Units.First(unit => unit.ShortName == "GB_NEW");

        viewModel.DeleteUnit(unitToDelete);

        Assert.DoesNotContain(assetManager.Units, unit => unit.ShortName == "GB_NEW");
        Assert.DoesNotContain(viewModel.Units, unit => unit.ShortName == "GB_NEW");
    }

    [Fact]
    public void EditingViewModelUpdateModel()
    {
        var assetManager = CreateAssetManager();
        var viewModel = new AssetManagerViewModel(assetManager);

        var unitViewModel = viewModel.Units.First();
        var modelUnit = assetManager.Units.First(unit => unit.ShortName == unitViewModel.ShortName);

        unitViewModel.Name = "Edited Unit Name";
        unitViewModel.MaxHeat = 99.5;
        unitViewModel.ProductionCosts = 1234;
        unitViewModel.IsAvailable = false;

        Assert.Equal("Edited Unit Name", modelUnit.Name);
        Assert.Equal(99.5, modelUnit.MaxHeat);
        Assert.Equal(1234, modelUnit.ProductionCosts);
        Assert.False(modelUnit.IsAvailable);
    }


}