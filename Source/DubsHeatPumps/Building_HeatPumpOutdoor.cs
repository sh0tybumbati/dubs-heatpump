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
                foreach (var connectedUnit in compAirconBaseUnit.ConnectedUnits)
                {
                    if (connectedUnit is Building_HeatPumpIndoor heatPump)
                    {
                        var heatPumpComp = heatPump.GetComp<CompHeatPump>();
                        if (heatPumpComp != null && heatPumpComp.IsHeating)
                        {
                            anyHeating = true;
                            break;
                        }
                    }
                }

                string mode = anyHeating ? "Heating mode (absorbing exterior heat)" : "Cooling mode (exhausting heat)";
                baseString += "\n" + mode;
            }

            return baseString;
        }
    }
}
