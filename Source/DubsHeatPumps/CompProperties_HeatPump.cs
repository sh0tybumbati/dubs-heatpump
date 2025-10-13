using Verse;

namespace DubsHeatPumps
{
    public class CompProperties_HeatPump : CompProperties
    {
        // Temperature threshold for mode switching (in Celsius)
        // If room temp is above (target + threshold), switch to cooling
        // If room temp is below (target - threshold), switch to heating
        public float modeThreshold = 2f;

        // Efficiency multiplier for heat pump vs standard heater/cooler
        public float efficiencyMultiplier = 1.2f;

        // Minimum outdoor temperature for heating operation (in Celsius)
        // Below this temperature, heat pump cannot extract enough heat from outside air
        public float minHeatingOutdoorTemp = -25f;

        public CompProperties_HeatPump()
        {
            compClass = typeof(CompHeatPump);
        }
    }
}
