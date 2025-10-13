using DubsBadHygiene;
using Verse;

namespace DubsHeatPumps
{
    public class Building_HeatPumpOutdoor : Building_AirconOutdoorUnit
    {
        public override void TickRare()
        {
            base.TickRare();

            // The outdoor unit automatically reverses operation based on indoor units
            // When indoor units heat, outdoor absorbs heat from outside
            // When indoor units cool, outdoor exhausts heat outside
            // This is handled by the base DBH air conditioner logic
        }

        public override string GetInspectString()
        {
            string baseString = base.GetInspectString();

            // Add heat pump specific information
            if (compAirconBaseUnit != null)
            {
                // Check if any connected indoor units are in heating mode
                bool anyHeating = false;
                int heatingUnits = 0;
                int coolingUnits = 0;

                foreach (var connectedUnit in compAirconBaseUnit.ConnectedUnits)
                {
                    if (connectedUnit is Building_HeatPumpIndoor heatPump)
                    {
                        var heatPumpComp = heatPump.GetComp<CompHeatPump>();
                        if (heatPumpComp != null)
                        {
                            if (heatPumpComp.IsHeating)
                            {
                                anyHeating = true;
                                heatingUnits++;
                            }
                            else
                            {
                                coolingUnits++;
                            }
                        }
                    }
                }

                if (heatingUnits > 0 || coolingUnits > 0)
                {
                    string mode = anyHeating ? "Heating mode" : "Cooling mode";
                    string operation = anyHeating ? "absorbing exterior heat" : "exhausting heat";
                    baseString += $"\n{mode} ({operation})";
                    baseString += $"\nConnected units: {heatingUnits + coolingUnits} ({(anyHeating ? heatingUnits + " heating" : coolingUnits + " cooling")})";
                }

                // Show outdoor temperature
                if (Map != null)
                {
                    float outdoorTemp = Map.mapTemperature.OutdoorTemp;
                    baseString += $"\nOutdoor: {outdoorTemp.ToStringTemperature()}";
                }
            }

            return baseString;
        }
    }
}
