namespace HeatProductionOptimization.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;

public class AssetManagerViewModel : ViewModelBase
{
	public List<ProductionUnitItemViewModel> Units { get; }

	public AssetManagerViewModel()
	{
		Units = new AssetManager().Units.Select(u => new ProductionUnitItemViewModel(u)).ToList();
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
	public Avalonia.Media.Imaging.Bitmap Image => unit.Image;

	public bool IsActive
	{
		get => unit.IsActive;
		set
		{
			if (unit.IsActive == value)
			{
				return;
			}

			unit.IsActive = value;
			OnPropertyChanged(nameof(IsActive));
			OnPropertyChanged(nameof(Status));
			OnPropertyChanged(nameof(StatusColor));
		}
	}

	public string Status => IsActive ? "* ON *" : "* OFF *";
	public IBrush StatusColor => IsActive ? Brushes.LimeGreen : new SolidColorBrush(Color.Parse("#d94545"));

	public ProductionUnitItemViewModel(ProductionUnit unit)
	{
		this.unit = unit ?? throw new ArgumentNullException(nameof(unit));
	}
}
