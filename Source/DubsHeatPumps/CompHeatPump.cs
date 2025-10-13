using RimWorld;
using Verse;
using DubsBadHygiene;
using System.Collections.Generic;
using UnityEngine;

namespace DubsHeatPumps
{
    [StaticConstructorOnStartup]
    public class CompHeatPump : ThingComp
    {
        // Use vanilla heater icon for heating, and cooling icon (or designator cancel) for cooling
        private static readonly Texture2D HeatingIcon = ContentFinder<Texture2D>.Get("UI/Designators/Heat", false) ?? BaseContent.BadTex;
        private static readonly Texture2D CoolingIcon = ContentFinder<Texture2D>.Get("UI/Designators/Cool", false) ?? BaseContent.BadTex;
        private CompProperties_HeatPump Props => (CompProperties_HeatPump)props;

        private CompTempControl tempControl;
        private CompAirconUnit airconUnit;
        private bool isHeating = false;

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            tempControl = parent.GetComp<CompTempControl>();
            airconUnit = parent.GetComp<CompAirconUnit>();
        }

        public override void CompTick()
        {
            base.CompTick();

            if (parent.IsHashIntervalTick(60)) // Check every 60 ticks (1 second)
            {
                UpdateHeatPumpMode();
            }
        }

        private void UpdateHeatPumpMode()
        {
            if (tempControl == null || airconUnit == null)
                return;

            Room room = parent.GetRoom(RegionType.Set_Passable);
            if (room == null)
                return;

            float roomTemp = room.Temperature;
            float targetTemp = tempControl.targetTemperature;
            float threshold = Props.modeThreshold;
            float outdoorTemp = parent.Map.mapTemperature.OutdoorTemp;

            // Determine if we should be heating or cooling
            bool shouldHeat = roomTemp < (targetTemp - threshold);
            bool shouldCool = roomTemp > (targetTemp + threshold);

            // Check if outdoor temperature allows heating
            bool canHeat = outdoorTemp >= Props.minHeatingOutdoorTemp;

            if (shouldCool && isHeating)
            {
                // Switch to cooling mode
                isHeating = false;
                UpdateMode();
            }
            else if (shouldHeat && !isHeating && canHeat)
            {
                // Switch to heating mode (only if outdoor temp permits)
                isHeating = true;
                UpdateMode();
            }
            else if (isHeating && !canHeat)
            {
                // Turn off heating if outdoor temp drops too low
                isHeating = false;
                UpdateMode();
            }
        }

        private void UpdateMode()
        {
            // The mode switching is handled by the vanilla CompTempControl
            // We just need to ensure the signs are correct
            if (tempControl != null)
            {
                // When heating, energyPerSecond should be positive
                // When cooling, energyPerSecond should be negative (handled by CompAirconUnit)
                // The outdoor unit will automatically reverse its operation
            }
        }

        public bool IsHeating => isHeating;

        public bool CanHeat
        {
            get
            {
                if (parent?.Map == null)
                    return false;
                float outdoorTemp = parent.Map.mapTemperature.OutdoorTemp;
                return outdoorTemp >= Props.minHeatingOutdoorTemp;
            }
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref isHeating, "isHeating", false);
        }

        public override string CompInspectStringExtra()
        {
            if (tempControl == null)
                return null;

            string result = "";

            // Current mode
            string mode = isHeating ? "Heating" : "Cooling";
            result += $"Mode: {mode}\n";

            // Room and target temperatures
            Room room = parent.GetRoom(RegionType.Set_Passable);
            if (room != null)
            {
                float roomTemp = room.Temperature;
                float targetTemp = tempControl.targetTemperature;
                result += $"Room: {roomTemp.ToStringTemperature()} / Target: {targetTemp.ToStringTemperature()}\n";
            }

            // Outdoor temperature
            if (parent?.Map != null)
            {
                float outdoorTemp = parent.Map.mapTemperature.OutdoorTemp;
                result += $"Outdoor: {outdoorTemp.ToStringTemperature()}\n";

                // Temperature differential
                if (room != null)
                {
                    float differential = isHeating
                        ? room.Temperature - outdoorTemp
                        : outdoorTemp - room.Temperature;
                    result += $"Temp differential: {differential.ToStringTemperatureOffset()}\n";
                }

                // Show warning if trying to heat but outdoor temp is too cold
                if (isHeating && !CanHeat)
                {
                    result += "WARNING: Too cold outside for heating\n";
                }
                else if (room != null && room.Temperature < (tempControl.targetTemperature - Props.modeThreshold) && !CanHeat)
                {
                    result += $"Heating unavailable below {Props.minHeatingOutdoorTemp.ToStringTemperature()} outdoor";
                }
            }

            return result.TrimEnd('\n');
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            foreach (Gizmo gizmo in base.CompGetGizmosExtra())
            {
                yield return gizmo;
            }

            // Add mode indicator gizmo
            Command_Action modeIndicator = new Command_Action
            {
                defaultLabel = isHeating ? "Mode: Heating" : "Mode: Cooling",
                defaultDesc = isHeating
                    ? "Heat pump is currently heating the room. Outdoor unit is absorbing heat from outside air."
                    : "Heat pump is currently cooling the room. Outdoor unit is exhausting heat outside.",
                icon = isHeating ? HeatingIcon : CoolingIcon,
                action = delegate { } // Read-only indicator
            };

            // Add visual indicator by changing icon color
            if (isHeating)
            {
                modeIndicator.defaultIconColor = new Color(1f, 0.5f, 0.2f); // Orange for heating
            }
            else
            {
                modeIndicator.defaultIconColor = new Color(0.4f, 0.7f, 1f); // Blue for cooling
            }

            // Show disabled if heating but can't heat
            if (isHeating && !CanHeat)
            {
                modeIndicator.Disable("Outdoor temperature too low for heating");
            }

            yield return modeIndicator;
        }
    }
}
