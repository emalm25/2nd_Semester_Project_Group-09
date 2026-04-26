using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.VisualBasic;
using System.Globalization;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CsvHandler
{

public static class JsonReader
{
    public static List<Data> JsonReadHandle(string fileName)
    {
        List<Data> result = new List<Data>();
        string jsonContent = File.ReadAllText(fileName);

        using (JsonDocument doc = JsonDocument.Parse(jsonContent))
        {
            JsonElement root = doc.RootElement;

            if (root.ValueKind == JsonValueKind.Array)
            {
                foreach (JsonElement item in root.EnumerateArray())
                {
                    var data = ParseDataFromJson(item);
                    result.Add(data);
                }
            }
            else
            {
                throw new FormatException($"Invalid JSON format in '{fileName}': expected an array");
            }
        }

        return result;
    }

    private static Data ParseDataFromJson(JsonElement item)
    {
        DateTime startTime = DateTime.Parse(item[0].GetString() ?? "", CultureInfo.InvariantCulture);
        DateTime endTime = DateTime.Parse(item[1].GetString() ?? "", CultureInfo.InvariantCulture);
        double heatDemand = item[2].GetDouble();
        double electricityPrice = item[3].GetDouble();

        return new Data(startTime, endTime, heatDemand, electricityPrice);
    }
}

}