using RimWorld;
using Verse;
using UnityEngine;
using System.Collections.Generic;

namespace DubsHeatPumps
{
    /// <summary>
    /// MapComponent that draws capacity bars on heat pump indoor units
    /// This works around the limitation that PostDraw() doesn't work for wall-mounted buildings
    /// </summary>
    [StaticConstructorOnStartup]
    public class MapComponent_HeatPumpOverlay : MapComponent
    {
        private static readonly Material CapacityFilled = SolidColorMaterials.SimpleSolidColorMaterial(new Color(0.5f, 0.8f, 1f)); // Cyan like DBH
        private static readonly Material CapacityUnfilled = SolidColorMaterials.SimpleSolidColorMaterial(new Color(0.3f, 0.3f, 0.3f)); // Dark gray

        public MapComponent_HeatPumpOverlay(Map map) : base(map)
        {
        }

        private int debugTicks = 0;

        public override void MapComponentOnGUI()
        {
            base.MapComponentOnGUI();

            // Debug: Log once per second
            debugTicks++;
            if (debugTicks % 60 == 0)
            {
                Log.Message($"[HeatPump] MapComponentOnGUI CALLED, currentMap={map == Find.CurrentMap}");
            }

            if (map != Find.CurrentMap)
                return;

            // Find all heat pump indoor units on the map
            List<Thing> allThings = map.listerThings.AllThings;
            int heatPumpCount = 0;

            for (int i = 0; i < allThings.Count; i++)
            {
                Thing thing = allThings[i];
                if (thing.def.defName != "HeatPumpIndoorUnit")
                    continue;

                heatPumpCount++;

                CompHeatPumpIndoor heatPump = thing.TryGetComp<CompHeatPumpIndoor>();
                if (heatPump == null)
                {
                    if (debugTicks % 60 == 0)
                        Log.Warning($"[HeatPump] Found HeatPumpIndoorUnit but CompHeatPumpIndoor is NULL!");
                    continue;
                }

                if (!thing.Spawned)
                    continue;

                // Get capacity ratio from DBH
                float capacityRatio = heatPump.GetDBHCapacityRatio();

                if (debugTicks % 60 == 0)
                {
                    Log.Message($"[HeatPump] Drawing bar for {thing.Label}: ratio={capacityRatio:F2}, pos={thing.DrawPos}");
                }

                // Draw vertical capacity bar
                GenDraw.FillableBarRequest r = default(GenDraw.FillableBarRequest);
                r.center = thing.DrawPos + Vector3.up * 0.1f;
                r.size = new Vector2(0.08f, 0.55f);
                r.fillPercent = capacityRatio;
                r.filledMat = CapacityFilled;
                r.unfilledMat = CapacityUnfilled;
                r.margin = 0.15f;
                r.rotation = Rot4.North;

                GenDraw.DrawFillableBar(r);
            }

            if (debugTicks % 60 == 0 && heatPumpCount > 0)
            {
                Log.Message($"[HeatPump] Found {heatPumpCount} heat pump units on map");
            }
        }
    }
}
