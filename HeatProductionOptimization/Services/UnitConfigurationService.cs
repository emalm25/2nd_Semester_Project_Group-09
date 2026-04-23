using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace HeatProductionOptimization.Services;

public class UnitData
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "";

    [JsonPropertyName("shortName")]
    public string ShortName { get; set; } = "";

    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("isAvailable")]
    public bool IsAvailable { get; set; }

    [JsonPropertyName("maxHeat")]
    public double MaxHeat { get; set; }

    [JsonPropertyName("productionCosts")]
    public decimal ProductionCosts { get; set; }

    [JsonPropertyName("co2Emissions")]
    public double? CO2Emissions { get; set; }

    [JsonPropertyName("gasConsumption")]
    public double? GasConsumption { get; set; }

    [JsonPropertyName("oilConsumption")]
    public double? OilConsumption { get; set; }

    [JsonPropertyName("gas2Consumption")]
    public double? Gas2Consumption { get; set; }

    [JsonPropertyName("maxElectricity")]
    public double? MaxElectricity { get; set; }
}

public class UnitConfigurationService
{
    private readonly string _configPath;
    private readonly JsonSerializerOptions _jsonOptions;

    public UnitConfigurationService()
    {
        _configPath = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            "units-config.json"
        );

        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };
    }

    public async Task<List<ProductionUnit>> LoadUnitsAsync()
    {
        try
        {
            if (!File.Exists(_configPath))
            {
                return GetDefaultUnits();
            }

            var json = await File.ReadAllTextAsync(_configPath);
            var unitsData = JsonSerializer.Deserialize<List<UnitData>>(json, _jsonOptions);

            return ConvertToProductionUnits(unitsData ?? new List<UnitData>());
        }
        catch
        {
            return GetDefaultUnits();
        }
    }

    public List<ProductionUnit> LoadUnits()
    {
        try
        {
            if (!File.Exists(_configPath))
            {
                return GetDefaultUnits();
            }

            var json = File.ReadAllText(_configPath);
            var unitsData = JsonSerializer.Deserialize<List<UnitData>>(json, _jsonOptions);

            return ConvertToProductionUnits(unitsData ?? new List<UnitData>());
        }
        catch
        {
            return GetDefaultUnits();
        }
    }

    public async Task SaveUnitsAsync(List<ProductionUnit> units)
    {
        try
        {
            var unitsData = ConvertToUnitData(units);
            var json = JsonSerializer.Serialize(unitsData, _jsonOptions);
            await File.WriteAllTextAsync(_configPath, json);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error saving units: {ex.Message}");
        }
    }

    public void SaveUnits(List<ProductionUnit> units)
    {
        try
        {
            var unitsData = ConvertToUnitData(units);
            var json = JsonSerializer.Serialize(unitsData, _jsonOptions);
            File.WriteAllText(_configPath, json);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error saving units: {ex.Message}");
        }
    }

    private List<ProductionUnit> ConvertToProductionUnits(List<UnitData> unitsData)
    {
        var units = new List<ProductionUnit>();

        foreach (var data in unitsData)
        {
            ProductionUnit? unit = data.Type switch
            {
                "GasBoiler" => new GasBoilersInfo(
                    data.ShortName, data.Name, data.IsAvailable,
                    data.MaxHeat, data.ProductionCosts, data.CO2Emissions ?? 0,
                    data.GasConsumption ?? 0
                ),
                "OilBoiler" => new OilBoilerInfo(
                    data.ShortName, data.Name, data.IsAvailable,
                    data.MaxHeat, data.ProductionCosts, data.CO2Emissions ?? 0,
                    data.OilConsumption ?? 0
                ),
                "GasMotor" => new GasMotorInfo(
                    data.ShortName, data.Name, data.IsAvailable,
                    data.MaxHeat, data.MaxElectricity ?? 0, data.ProductionCosts,
                    data.CO2Emissions ?? 0, data.Gas2Consumption ?? 0
                ),
                "ElectricBoiler" => new ElectricBoilerInfo(
                    data.ShortName, data.Name, data.IsAvailable,
                    data.MaxHeat, data.MaxElectricity ?? 0, data.ProductionCosts
                ),
                _ => null
            };

            if (unit != null)
                units.Add(unit);
        }

        return units;
    }

    private List<UnitData> ConvertToUnitData(List<ProductionUnit> units)
    {
        var unitsData = new List<UnitData>();

        foreach (var unit in units)
        {
            var type = unit.GetType().Name switch
            {
                nameof(GasBoilersInfo) => "GasBoiler",
                nameof(OilBoilerInfo) => "OilBoiler",
                nameof(GasMotorInfo) => "GasMotor",
                nameof(ElectricBoilerInfo) => "ElectricBoiler",
                _ => "Unknown"
            };

            unitsData.Add(new UnitData
            {
                Type = type,
                ShortName = unit.ShortName,
                Name = unit.Name,
                IsAvailable = unit.IsAvailable,
                MaxHeat = unit.MaxHeat,
                ProductionCosts = unit.ProductionCosts,
                CO2Emissions = unit.CO2Emissions,
                GasConsumption = unit.GasConsumption,
                OilConsumption = unit.OilConsumption,
                Gas2Consumption = unit.Gas2Consumption,
                MaxElectricity = unit.MaxElectricity
            });
        }

        return unitsData;
    }

    private List<ProductionUnit> GetDefaultUnits()
    {
        return new List<ProductionUnit>
        {
            new GasBoilersInfo("GB1", "Gas Boiler 1", true, 3.0, 510, 132, 1.05),
            new GasBoilersInfo("GB2", "Gas Boiler 2", true, 2.0, 540, 134, 1.08),
            new GasBoilersInfo("GB3", "Gas Boiler 3", true, 4.0, 580, 136, 1.09),
            new OilBoilerInfo("OB1", "Oil Boiler 1", true, 6.0, 690, 147, 1.18),
            new GasMotorInfo("GM1", "Gas Motor 1", true, 5.3, 3.9, 975, 227, 1.82),
            new ElectricBoilerInfo("EB1", "Electric Boiler 1", true, 6.0, -6.0, 15)
        };
    }
}
