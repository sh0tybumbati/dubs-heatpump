using RimWorld;
using Verse;
using System.Reflection;

namespace DubsHeatPumps
{
    /// <summary>
    /// Component for heat pump outdoor units
    /// Calculates efficiency based on ambient temperature and modifies DBH capacity
    /// </summary>
    public class CompHeatPumpOutdoor : ThingComp
    {
        private const float MIN_HEATING_OUTDOOR_TEMP = -25f; // -25°C = -13°F

        private ThingComp baseUnitComp; // DBH's CompAirconBaseUnit

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);

            // Get DBH's CompAirconBaseUnit
            baseUnitComp = parent.AllComps.Find(c => c.GetType().Name == "CompAirconBaseUnit");
        }

        public override void CompTick()
        {
            base.CompTick();

            // Update efficiency every second
            if (parent.IsHashIntervalTick(60))
            {
                UpdateEfficiency();
            }
        }

        private void UpdateEfficiency()
        {
            if (baseUnitComp == null || parent?.Map == null)
                return;

            // Get ambient temperature of the room the outdoor unit is in
            float ambientTemp = GetAmbientTemperature();

            // Calculate efficiency based on ambient temp
            float efficiency = CalculateEfficiency(ambientTemp);

            // Apply efficiency to DBH's base capacity
            ApplyEfficiencyToCapacity(efficiency);
        }

        private float GetAmbientTemperature()
        {
            if (parent?.Map == null)
                return 21f;

            Room room = parent.GetRoom(RegionType.Set_Passable);

            if (room == null || room.UsesOutdoorTemperature)
            {
                // Unit is outside or in unroofed area - use outdoor temp
                return parent.Map.mapTemperature.OutdoorTemp;
            }
            else
            {
                // Unit is in a room (like utility room) - use room temp
                return room.Temperature;
            }
        }

        private float CalculateEfficiency(float ambientTemp)
        {
            // Efficiency curve for heat pumps:
            // - Optimal at moderate temps (15-25°C)
            // - Degrades at temperature extremes

            // For heating: less efficient when it's very cold
            // For cooling: less efficient when it's very hot

            // Use a combined curve that works for both modes
            // 100% efficiency at 15-25°C
            // Degrades to 50% at extremes (-25°C cold, 50°C hot)

            if (ambientTemp >= 15f && ambientTemp <= 25f)
                return 1f; // 100% efficiency in optimal range

            if (ambientTemp < 15f)
            {
                // Cold: efficiency decreases as temp drops
                if (ambientTemp <= MIN_HEATING_OUTDOOR_TEMP)
                    return 0.5f; // 50% at -25°C

                // Linear interpolation between -25°C (50%) and 15°C (100%)
                float range = 15f - MIN_HEATING_OUTDOOR_TEMP; // 40 degrees
                float position = ambientTemp - MIN_HEATING_OUTDOOR_TEMP; // 0 to 40
                return 0.5f + (position / range) * 0.5f;
            }
            else
            {
                // Hot: efficiency decreases as temp rises
                if (ambientTemp >= 50f)
                    return 0.5f; // 50% at 50°C

                // Linear interpolation between 25°C (100%) and 50°C (50%)
                float range = 50f - 25f; // 25 degrees
                float position = ambientTemp - 25f; // 0 to 25
                return 1f - (position / range) * 0.5f;
            }
        }

        private void ApplyEfficiencyToCapacity(float efficiency)
        {
            if (baseUnitComp == null)
                return;

            try
            {
                // Get the base capacity from DBH
                var baseCapacityField = baseUnitComp.GetType().GetField("BaseCapacity",
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

                if (baseCapacityField == null)
                    return;

                float baseCapacity = (float)baseCapacityField.GetValue(baseUnitComp);

                // Calculate effective capacity with efficiency multiplier
                float effectiveCapacity = baseCapacity * efficiency;

                // Try to set the current capacity
                // DBH might have a "Capacity" field separate from "BaseCapacity"
                var capacityField = baseUnitComp.GetType().GetField("Capacity",
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

                if (capacityField != null)
                {
                    capacityField.SetValue(baseUnitComp, effectiveCapacity);
                }
            }
            catch
            {
                // Reflection failed
            }
        }

        public float CurrentEfficiency
        {
            get
            {
                if (parent?.Map == null)
                    return 1f;

                float ambientTemp = GetAmbientTemperature();
                return CalculateEfficiency(ambientTemp);
            }
        }

        public override string CompInspectStringExtra()
        {
            if (parent?.Map == null)
                return null;

            float ambientTemp = GetAmbientTemperature();
            float efficiency = CalculateEfficiency(ambientTemp);

            string result = $"Efficiency: {(efficiency * 100f).ToString("F1")}%";

            // Add status based on temp
            if (ambientTemp < 0f)
                result += " (very cold)";
            else if (ambientTemp < 10f)
                result += " (cold)";
            else if (ambientTemp > 40f)
                result += " (very hot)";
            else if (ambientTemp > 30f)
                result += " (hot)";

            return result;
        }
    }
}
