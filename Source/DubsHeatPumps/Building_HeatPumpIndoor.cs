using RimWorld;
using Verse;
using DubsBadHygiene;

namespace DubsHeatPumps
{
    public class Building_HeatPumpIndoor : Building_AirconUnit
    {
        private CompHeatPump heatPumpComp;
        private CompTempControl tempControl;

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            heatPumpComp = GetComp<CompHeatPump>();
            tempControl = GetComp<CompTempControl>();
        }

        public override void TickRare()
        {
            base.TickRare();

            if (heatPumpComp == null || tempControl == null)
                return;

            // Adjust the temperature control based on heat pump mode
            Room room = this.GetRoom(RegionType.Set_Passable);
            if (room == null)
                return;

            float roomTemp = room.Temperature;
            float targetTemp = tempControl.targetTemperature;

            // Allow heating when room is cold and cooling when room is hot
            if (heatPumpComp.IsHeating && heatPumpComp.CanHeat)
            {
                // In heating mode, act like a heater (only if outdoor temp allows)
                if (roomTemp < targetTemp)
                {
                    // Push warm air into the room
                    GenTemperature.PushHeat(this, tempControl.Props.energyPerSecond * 4.16666651f);
                }
            }
            // Cooling mode is handled by the base CompAirconUnit from DBH
        }
    }
}
