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

    [JsonPropertyName("scenarios")]
    public List<string> Scenarios { get; set; } = new() { "all" };
}

public class ScenarioData
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("displayName")]
    public string DisplayName { get; set; } = "";

    [JsonPropertyName("units")]
    public List<string> Units { get; set; } = new();

    [JsonPropertyName("maintenanceUnits")]
    public Dictionary<string, string> MaintenanceUnits { get; set; } = new();
}

public class UnitConfigurationService
{
    private readonly string _configPath;
    private readonly string _scenariosConfigPath;
    private readonly JsonSerializerOptions _jsonOptions;

    public UnitConfigurationService()
    {
        _configPath = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            "units.json"
        );

        _scenariosConfigPath = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            "scenarios-config.json"
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

    public async Task<List<ScenarioData>> LoadScenariosAsync()
    {
        try
        {
            if (!File.Exists(_scenariosConfigPath))
            {
                return GetDefaultScenarios();
            }

            var json = await File.ReadAllTextAsync(_scenariosConfigPath);
            var scenarios = JsonSerializer.Deserialize<List<ScenarioData>>(json, _jsonOptions);

            return scenarios ?? GetDefaultScenarios();
        }
        catch
        {
            return GetDefaultScenarios();
        }
    }

    public List<ScenarioData> LoadScenarios()
    {
        try
        {
            if (!File.Exists(_scenariosConfigPath))
            {
                return GetDefaultScenarios();
            }

            var json = File.ReadAllText(_scenariosConfigPath);
            var scenarios = JsonSerializer.Deserialize<List<ScenarioData>>(json, _jsonOptions);

            return scenarios ?? GetDefaultScenarios();
        }
        catch
        {
            return GetDefaultScenarios();
        }
    }

    public async Task SaveScenariosAsync(List<ScenarioData> scenarios)
    {
        try
        {
            var json = JsonSerializer.Serialize(scenarios, _jsonOptions);
            await File.WriteAllTextAsync(_scenariosConfigPath, json);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error saving scenarios: {ex.Message}");
        }
    }

    public void SaveScenarios(List<ScenarioData> scenarios)
    {
        try
        {
            var json = JsonSerializer.Serialize(scenarios, _jsonOptions);
            File.WriteAllText(_scenariosConfigPath, json);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error saving scenarios: {ex.Message}");
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
            {
                unit.Scenarios = new List<string>(data.Scenarios ?? new List<string> { "all" });
                units.Add(unit);
            }
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
                MaxElectricity = unit.MaxElectricity,
                Scenarios = new List<string>(unit.Scenarios)
            });
        }

        return unitsData;
    }

    private List<ProductionUnit> GetDefaultUnits()
    {
        return new List<ProductionUnit>
        {
            new GasBoilersInfo("GB1", "Gas Boiler 1", true, 3.0, 510, 132, 1.05) { Scenarios = new List<string> { "Scenario1", "Scenario2" } },
            new GasBoilersInfo("GB2", "Gas Boiler 2", true, 2.0, 540, 134, 1.08) { Scenarios = new List<string> { "Scenario1", "Scenario2" } },
            new GasBoilersInfo("GB3", "Gas Boiler 3", true, 4.0, 580, 136, 1.09) { Scenarios = new List<string> { "Scenario1" } },
            new OilBoilerInfo("OB1", "Oil Boiler 1", true, 6.0, 690, 147, 1.18) { Scenarios = new List<string> { "Scenario1" } },
            new GasMotorInfo("GM1", "Gas Motor 1", true, 5.3, 3.9, 975, 227, 1.82) { Scenarios = new List<string> { "Scenario2" } },
            new ElectricBoilerInfo("EB1", "Electric Boiler 1", true, 6.0, -6.0, 15) { Scenarios = new List<string> { "Scenario2" } }
        };
    }

    private static List<ScenarioData> GetDefaultScenarios()
    {
        return new List<ScenarioData>
        {
            new ScenarioData
            {
                Name = "Scenario1",
                DisplayName = "Scenario 1",
                Units = new List<string> { "GB1", "GB2", "GB3", "OB1" },
                MaintenanceUnits = new Dictionary<string, string>
                {
                    { "Winter", "OB1" },
                    { "Summer", "GB3" }
                }
            },
            new ScenarioData
            {
                Name = "Scenario2",
                DisplayName = "Scenario 2",
                Units = new List<string> { "GB1", "GB2", "GM1", "EB1" },
                MaintenanceUnits = new Dictionary<string, string>
                {
                    { "Winter", "GM1" },
                    { "Summer", "EB1" }
                }
            }
        };
    }
}
