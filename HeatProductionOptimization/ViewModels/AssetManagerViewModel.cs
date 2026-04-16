namespace HeatProductionOptimization.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;

public class AssetManagerViewModel : ViewModelBase
{
	public List<ProductionUnitItemViewModel> Units { get; }
    public AssetManager AssetManager { get; }

	public AssetManagerViewModel(AssetManager? assetManager = null)
	{
		AssetManager = assetManager ?? new AssetManager();
		Units = AssetManager.Units.Select(u => new ProductionUnitItemViewModel(u)).ToList();
	}
}

public class ProductionUnitItemViewModel : ObservableObject
{
	private readonly ProductionUnit unit;

	public string ShortName => unit.ShortName;
	public string Name => unit.Name;
	public double MaxHeat => unit.MaxHeat;
	public decimal ProductionCosts => unit.ProductionCosts;
	public double? CO2Emissions => unit.CO2Emissions;
	public double? GasConsumption => unit.GasConsumption;
	public double? OilConsumption => unit.OilConsumption;
	public double? Gas2Consumption => unit.Gas2Consumption;
	public double? MaxElectricity => unit.MaxElectricity;
	public Bitmap Image => unit.Image;

	public bool IsAvailable
	{
		get => unit.IsAvailable;
		set
		{
			if (unit.IsAvailable == value)
			{
				return;
			}

			unit.IsAvailable = value;
			OnPropertyChanged(nameof(IsAvailable));
		}
	}

	public ProductionUnitItemViewModel(ProductionUnit unit)
	{
		this.unit = unit ?? throw new ArgumentNullException(nameof(unit));
	}
}
