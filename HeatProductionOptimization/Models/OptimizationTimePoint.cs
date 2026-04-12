using System;
using System.Collections.Generic;

namespace HeatProductionOptimization.Models;

public class OptimizationTimePoint
{
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public double HeatDemand { get; set; }
    public double HeatDelivered { get; set; }
    public double ElectricityPrice { get; set; }
    public double Cost { get; set; }
    public double Co2 { get; set; }
    public double ElectricityProduced { get; set; }
    public double ElectricityConsumed { get; set; }
    public double NetElectricity { get; set; }
    public List<OptimizationResult.DispatchResult> Dispatches { get; set; } = new();
    public bool DemandMet { get; set; }
}
