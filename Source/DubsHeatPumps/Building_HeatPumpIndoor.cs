using RimWorld;
using Verse;
using UnityEngine;

namespace DubsHeatPumps
{
    /// <summary>
    /// Custom Building class for heat pump indoor units
    /// Overrides Draw() to add capacity bar rendering
    /// </summary>
    [StaticConstructorOnStartup]
    public class Building_HeatPumpIndoor : Building
    {
        private static readonly Material CapacityFilled = SolidColorMaterials.SimpleSolidColorMaterial(new Color(0.5f, 0.8f, 1f)); // Cyan like DBH
        private static readonly Material CapacityUnfilled = SolidColorMaterials.SimpleSolidColorMaterial(new Color(0.3f, 0.3f, 0.3f)); // Dark gray

        public override void Draw()
        {
            base.Draw();

            // Debug: Check if Draw() is being called
            if (Find.TickManager.TicksGame % 60 == 0) // Log once per second
            {
                Log.Message($"[HeatPump] Draw() CALLED for {Label} at {DrawPos}");
            }

            // Get the heat pump component
            CompHeatPumpIndoor heatPump = this.TryGetComp<CompHeatPumpIndoor>();
            if (heatPump == null)
            {
                if (Find.TickManager.TicksGame % 60 == 0)
                    Log.Warning($"[HeatPump] CompHeatPumpIndoor is NULL!");
                return;
            }

            // Only draw when spawned and on current map
            if (!Spawned || Map != Find.CurrentMap)
            {
                if (Find.TickManager.TicksGame % 60 == 0)
                    Log.Message($"[HeatPump] Not drawing - Spawned={Spawned}, CurrentMap={Map == Find.CurrentMap}");
                return;
            }

            // Get capacity ratio from DBH's aircon component
            float capacityRatio = heatPump.GetDBHCapacityRatio();

            // Debug: Log bar parameters
            if (Find.TickManager.TicksGame % 60 == 0)
            {
                Log.Message($"[HeatPump] Drawing bar - capacityRatio={capacityRatio:F2}, pos={DrawPos}");
            }

            // Draw vertical capacity bar matching DBH style
            // Shows this unit's share of available outdoor capacity
            GenDraw.FillableBarRequest r = default(GenDraw.FillableBarRequest);
            r.center = DrawPos + Vector3.up * 0.1f;
            r.size = new Vector2(0.08f, 0.55f);
            r.fillPercent = capacityRatio;
            r.filledMat = CapacityFilled;
            r.unfilledMat = CapacityUnfilled;
            r.margin = 0.15f;
            r.rotation = Rot4.North;

            GenDraw.DrawFillableBar(r);
        }
    }
}
