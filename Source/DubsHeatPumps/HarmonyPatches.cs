using HarmonyLib;
using RimWorld;
using Verse;
using UnityEngine;
using System.Reflection;

namespace DubsHeatPumps
{
    [StaticConstructorOnStartup]
    public static class HarmonyPatches
    {
        static HarmonyPatches()
        {
            var harmony = new Harmony("dubwise.heatpumps");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
            Log.Message("[DubsHeatPumps] Harmony patches applied");
        }
    }

    /// <summary>
    /// Patch Thing.Draw() to draw capacity bars on wall-mounted heat pump units
    /// Thing.Draw() is called every frame during real-time rendering, perfect for overlays
    /// </summary>
    [HarmonyPatch(typeof(Thing), "Draw")]
    public static class Thing_Draw_Patch
    {
        private static readonly Material CapacityFilled = SolidColorMaterials.SimpleSolidColorMaterial(new Color(0.5f, 0.8f, 1f)); // Cyan like DBH
        private static readonly Material CapacityUnfilled = SolidColorMaterials.SimpleSolidColorMaterial(new Color(0.3f, 0.3f, 0.3f)); // Dark gray

        [HarmonyPostfix]
        public static void Postfix(Thing __instance)
        {
            // Only draw for heat pump indoor units
            CompHeatPumpIndoor heatPump = __instance.TryGetComp<CompHeatPumpIndoor>();
            if (heatPump == null)
                return;

            // Only draw when the thing is visible and spawned
            if (!__instance.Spawned || __instance.Map != Find.CurrentMap)
                return;

            // Draw vertical capacity bar matching DBH style
            // Shows efficiency: based on outdoor temperature (100% optimal, 50% at extremes)
            GenDraw.FillableBarRequest r = default(GenDraw.FillableBarRequest);
            r.center = __instance.DrawPos + Vector3.up * 0.1f;
            r.size = new Vector2(0.08f, 0.55f);
            r.fillPercent = heatPump.CurrentEfficiency;
            r.filledMat = CapacityFilled;
            r.unfilledMat = CapacityUnfilled;
            r.margin = 0.15f;
            r.rotation = Rot4.North;

            GenDraw.DrawFillableBar(r);
        }
    }
}
