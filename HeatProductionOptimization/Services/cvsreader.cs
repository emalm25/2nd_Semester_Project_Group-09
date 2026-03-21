using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.VisualBasic;
using System.Globalization;
using System.IO;

namespace CsvHandler
{

public static class Csvreader
{
    public static List<Data> CSVReadHandle(string fileName )
        {
            List<Data> result = new List<Data>();
        using (StreamReader reader = new StreamReader(fileName))
            {   
            while (!reader.EndOfStream)
                {
            
                string? line = reader.ReadLine();

                if(string.IsNullOrWhiteSpace(line))
                continue;

                string [] values = line.Split('\t');

                DateTime start = DateTime.Parse(values[0]);
                DateTime end = DateTime.Parse(values[1]);
                double heat = double.Parse(values[2]);
                double price = double.Parse(values[3]);

            result.Add(new Data (start, end, heat, price) );
                
                }
            }
        
    return result;

        }
    
}

}