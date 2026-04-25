namespace HeatProductionOptimization.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Timers;
using System.Windows.Input;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HeatProductionOptimization.Services;

public class AssetManagerViewModel : ViewModelBase
{
	private bool _isEditMode;
	private Timer _autoSaveTimer;
	private bool _hasUnsavedChanges;

	public ObservableCollection<ProductionUnitItemViewModel> Units { get; }
	public AssetManager AssetManager { get; }
	public ICommand AddNewUnitCommand { get; }
	public ICommand DeleteUnitCommand { get; }

	public bool IsEditMode
	{
		get => _isEditMode;
		set => SetProperty(ref _isEditMode, value);
	}

	public AssetManagerViewModel(AssetManager? assetManager = null)
	{
		AssetManager = assetManager ?? new AssetManager();
		Units = new ObservableCollection<ProductionUnitItemViewModel>(
			AssetManager.Units.Select(u => new ProductionUnitItemViewModel(u, OnUnitChanged))
		);

		AddNewUnitCommand = new RelayCommand<string?>(type => { if (type != null) AddNewUnit(type); });
		DeleteUnitCommand = new RelayCommand<ProductionUnitItemViewModel?>(unit => { if (unit != null) DeleteUnit(unit); });

		_autoSaveTimer = new Timer(2000);
		_autoSaveTimer.Elapsed += AutoSaveTimer_Elapsed;
		_autoSaveTimer.AutoReset = false;
	}

	public void AddNewUnit(string type)
	{
		ProductionUnit? newUnit = type switch
		{
			"GasBoiler" => new GasBoilersInfo("GB_NEW", "New Gas Boiler", true, 1.0, 500, 100, 1.0),
			"OilBoiler" => new OilBoilerInfo("OB_NEW", "New Oil Boiler", true, 1.0, 600, 110, 1.0),
			"GasMotor" => new GasMotorInfo("GM_NEW", "New Gas Motor", true, 1.0, 1.0, 800, 150, 1.0),
			"ElectricBoiler" => new ElectricBoilerInfo("EB_NEW", "New Electric Boiler", true, 1.0, 1.0, 50),
			_ => null
		};

		if (newUnit != null)
		{
			AssetManager.Units.Add(newUnit);
			Units.Add(new ProductionUnitItemViewModel(newUnit, OnUnitChanged));
			TriggerAutoSave();
		}
	}

	public void DeleteUnit(ProductionUnitItemViewModel unit)
	{
		var productionUnit = AssetManager.Units.FirstOrDefault(u => u.ShortName == unit.ShortName);
		if (productionUnit != null)
		{
			AssetManager.Units.Remove(productionUnit);
			Units.Remove(unit);
			TriggerAutoSave();
		}
	}

	private void OnUnitChanged()
	{
		_hasUnsavedChanges = true;
		TriggerAutoSave();
	}

	private void TriggerAutoSave()
	{
		if (_autoSaveTimer.Enabled)
		{
			_autoSaveTimer.Stop();
		}
		_autoSaveTimer.Start();
	}

	private void AutoSaveTimer_Elapsed(object? sender, ElapsedEventArgs e)
	{
		if (_hasUnsavedChanges && IsEditMode)
		{
			AssetManager.SaveUnits();
			_hasUnsavedChanges = false;
		}
	}
}

public class ProductionUnitItemViewModel : ObservableObject
{
	private readonly ProductionUnit unit;
	private readonly Action _onChanged;
	private string _shortName;
	private string _name;
	private double _maxHeat;
	private decimal _productionCosts;
	private double? _co2Emissions;
	private double? _gasConsumption;
	private double? _oilConsumption;
	private double? _gas2Consumption;
	private double? _maxElectricity;
	private bool _isAvailable;
	private List<string> _scenarios;

	public string ShortName
	{
		get => _shortName;
		set
		{
			if (SetProperty(ref _shortName, value))
			{
				unit.ShortName = value;
				_onChanged?.Invoke();
			}
		}
	}

	public string Name
	{
		get => _name;
		set
		{
			if (SetProperty(ref _name, value))
			{
				unit.Name = value;
				_onChanged?.Invoke();
			}
		}
	}

	public double MaxHeat
	{
		get => _maxHeat;
		set
		{
			if (SetProperty(ref _maxHeat, value))
			{
				unit.MaxHeat = value;
				_onChanged?.Invoke();
			}
		}
	}

	public decimal ProductionCosts
	{
		get => _productionCosts;
		set
		{
			if (SetProperty(ref _productionCosts, value))
			{
				unit.ProductionCosts = value;
				_onChanged?.Invoke();
			}
		}
	}

	public double? CO2Emissions
	{
		get => _co2Emissions;
		set
		{
			if (SetProperty(ref _co2Emissions, value))
			{
				unit.CO2Emissions = value;
				_onChanged?.Invoke();
			}
		}
	}

	public double? GasConsumption
	{
		get => _gasConsumption;
		set
		{
			if (SetProperty(ref _gasConsumption, value))
			{
				unit.GasConsumption = value;
				_onChanged?.Invoke();
			}
		}
	}

	public double? OilConsumption
	{
		get => _oilConsumption;
		set
		{
			if (SetProperty(ref _oilConsumption, value))
			{
				unit.OilConsumption = value;
				_onChanged?.Invoke();
			}
		}
	}

	public double? Gas2Consumption
	{
		get => _gas2Consumption;
		set
		{
			if (SetProperty(ref _gas2Consumption, value))
			{
				unit.Gas2Consumption = value;
				_onChanged?.Invoke();
			}
		}
	}

	public double? MaxElectricity
	{
		get => _maxElectricity;
		set
		{
			if (SetProperty(ref _maxElectricity, value))
			{
				unit.MaxElectricity = value;
				_onChanged?.Invoke();
			}
		}
	}

	public bool IsAvailable
	{
		get => _isAvailable;
		set
		{
			if (SetProperty(ref _isAvailable, value))
			{
				unit.IsAvailable = value;
				_onChanged?.Invoke();
			}
		}
	}

	public List<string> Scenarios
	{
		get => _scenarios;
		set
		{
			if (SetProperty(ref _scenarios, value))
			{
				unit.Scenarios = value;
				_onChanged?.Invoke();
			}
		}
	}

	public Bitmap Image => unit.Image;
	public string UnitType => unit.GetType().Name;

	public ProductionUnitItemViewModel(ProductionUnit unit, Action onChanged)
	{
		this.unit = unit ?? throw new ArgumentNullException(nameof(unit));
		_onChanged = onChanged;

		_shortName = unit.ShortName;
		_name = unit.Name;
		_maxHeat = unit.MaxHeat;
		_productionCosts = unit.ProductionCosts;
		_co2Emissions = unit.CO2Emissions;
		_gasConsumption = unit.GasConsumption;
		_oilConsumption = unit.OilConsumption;
		_gas2Consumption = unit.Gas2Consumption;
		_maxElectricity = unit.MaxElectricity;
		_isAvailable = unit.IsAvailable;
		_scenarios = new List<string>(unit.Scenarios);
	}
}

