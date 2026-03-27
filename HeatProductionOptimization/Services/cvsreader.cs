using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.VisualBasic;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;

namespace CsvHandler
{

public static class Csvreader
{
    private static readonly Regex RowPattern = new(
        @"^(\d{2}\.\d{2}\.\d{4}\s+\d{2}:\d{2})\s+(\d{2}\.\d{2}\.\d{4}\s+\d{2}:\d{2})\s+([+-]?\d+(?:[\.,]\d+)?)\s+([+-]?\d+(?:[\.,]\d+)?)$",
        RegexOptions.Compiled);

    public static List<Data> CSVReadHandle(string fileName )
        {
            List<Data> result = new List<Data>();
        using (StreamReader reader = new StreamReader(fileName))
            {
            int lineNumber = 0;
            while (!reader.EndOfStream)
                {
                lineNumber++;
                string? line = reader.ReadLine();

                if(string.IsNullOrWhiteSpace(line))
                continue;

                var match = RowPattern.Match(line.Trim());
                if (!match.Success)
                {
                    throw new FormatException($"Invalid data format at line {lineNumber} in '{fileName}': '{line}'");
                }

                string startText = match.Groups[1].Value;
                string endText = match.Groups[2].Value;
                string heatText = match.Groups[3].Value.Replace(',', '.');
                string priceText = match.Groups[4].Value.Replace(',', '.');

                DateTime start = DateTime.ParseExact(startText, "dd.MM.yyyy HH:mm", CultureInfo.InvariantCulture);
                DateTime end = DateTime.ParseExact(endText, "dd.MM.yyyy HH:mm", CultureInfo.InvariantCulture);
                double heat = double.Parse(heatText, CultureInfo.InvariantCulture);
                double price = double.Parse(priceText, CultureInfo.InvariantCulture);

                result.Add(new Data (start, end, heat, price) );
                
                }
            }
        
    return result;

        }
    
}

}