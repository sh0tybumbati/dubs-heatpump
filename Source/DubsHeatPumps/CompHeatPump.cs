using RimWorld;
using Verse;
using DubsBadHygiene;

namespace DubsHeatPumps
{
    public class CompHeatPump : ThingComp
    {
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

            string mode = isHeating ? "Heating" : "Cooling";
            string result = $"Heat Pump Mode: {mode}";

            // Show warning if trying to heat but outdoor temp is too cold
            if (isHeating && !CanHeat)
            {
                result += "\nWarning: Outdoor temperature too low for heating";
            }

            return result;
        }
    }
}
