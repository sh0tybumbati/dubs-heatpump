using RimWorld;
using Verse;
using System.Collections.Generic;
using UnityEngine;

namespace DubsHeatPumps
{
    /// <summary>
    /// Heat pump indoor unit that automatically switches between heating and cooling modes
    /// Works alongside DBH's CompAirconUnit without inheritance
    /// </summary>
    [StaticConstructorOnStartup]
    public class CompHeatPumpIndoor : ThingComp
    {
        private static readonly Texture2D HeatingIcon = ContentFinder<Texture2D>.Get("UI/Commands/TempRaise", false) ?? BaseContent.BadTex;
        private static readonly Texture2D CoolingIcon = ContentFinder<Texture2D>.Get("UI/Commands/TempLower", false) ?? BaseContent.BadTex;

        private static readonly Material CapacityFilled = SolidColorMaterials.SimpleSolidColorMaterial(new Color(0.5f, 0.8f, 1f)); // Cyan like DBH
        private static readonly Material CapacityUnfilled = SolidColorMaterials.SimpleSolidColorMaterial(new Color(0.3f, 0.3f, 0.3f)); // Dark gray

        private const float MODE_THRESHOLD = 2f; // Switch mode when 2°C away from target
        private const float MIN_HEATING_OUTDOOR_TEMP = -25f; // -25°C = -13°F

        private bool isHeating = false;
        private bool manualModeOverride = false; // Allow manual control
        private ThingComp thermostatComp; // DBH's CompThermostat (doesn't inherit from CompTempControl!)
        private ThingComp airconComp; // DBH's CompRoomUnit
        private CompPowerTrader powerComp;

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);

            // Get DBH's CompThermostat - it does NOT inherit from CompTempControl!
            thermostatComp = parent.AllComps.Find(c => c.GetType().Name == "CompThermostat");
            powerComp = parent.GetComp<CompPowerTrader>();

            // Get DBH's CompRoomUnit
            airconComp = parent.AllComps.Find(c => c.GetType().Name == "CompRoomUnit");
            if (airconComp == null)
            {
                // Fallback: try CompAirconUnit
                airconComp = parent.AllComps.Find(c => c.GetType().Name == "CompAirconUnit");
            }

            // Initialize mode based on current conditions if not loading from save
            if (!respawningAfterLoad && thermostatComp != null)
            {
                Room room = parent.GetRoom(RegionType.Set_Passable);
                if (room != null)
                {
                    float targetTemp = GetTargetTemperature();
                    isHeating = room.Temperature < targetTemp;
                }
            }
        }

        public override void CompTick()
        {
            base.CompTick();

            // Update mode every second
            if (parent.IsHashIntervalTick(60))
            {
                UpdateHeatPumpMode();
            }

            // Push heat every tick when in heating mode (not just once per second)
            if (isHeating && CanHeat)
            {
                if (powerComp != null && powerComp.PowerOn)
                {
                    // Push heat to match vanilla heater performance
                    // Reduced from 21 to 3 heat/sec to prevent too-fast heating
                    float heatPerTick = 3f / 60f; // 3 heat per second = 0.05 per tick

                    Room room = parent.GetRoom(RegionType.Set_Passable);
                    if (room != null && !room.UsesOutdoorTemperature)
                    {
                        float heatPush = heatPerTick / room.CellCount;
                        room.Temperature += heatPush;
                    }
                }
            }
        }

        /// <summary>
        /// Get target temperature from DBH's CompThermostat using reflection
        /// </summary>
        private float GetTargetTemperature()
        {
            if (thermostatComp == null)
                return 21f; // Default fallback

            try
            {
                var targetTempField = thermostatComp.GetType().GetField("targetTemperature",
                    System.Reflection.BindingFlags.Instance |
                    System.Reflection.BindingFlags.NonPublic |
                    System.Reflection.BindingFlags.Public);

                if (targetTempField != null)
                {
                    var value = targetTempField.GetValue(thermostatComp);
                    if (value is float temp)
                        return temp;
                }
            }
            catch
            {
                // Reflection failed, return default
            }

            return 21f; // Default fallback
        }

        private void UpdateHeatPumpMode()
        {
            if (thermostatComp == null || parent?.Map == null)
                return;

            Room room = parent.GetRoom(RegionType.Set_Passable);
            if (room == null)
                return;

            float roomTemp = room.Temperature;
            float targetTemp = GetTargetTemperature();
            float outdoorTemp = parent.Map.mapTemperature.OutdoorTemp;

            // Check if outdoor temperature allows heating
            bool canHeat = outdoorTemp >= MIN_HEATING_OUTDOOR_TEMP;

            // Only auto-switch if not in manual override mode
            if (!manualModeOverride)
            {
                // Determine if we should be heating or cooling based on room temperature
                bool shouldHeat = roomTemp < (targetTemp - MODE_THRESHOLD);
                bool shouldCool = roomTemp > (targetTemp + MODE_THRESHOLD);

                // Switch modes based on need
                if (shouldHeat && canHeat)
                {
                    if (!isHeating)
                    {
                        Log.Message($"HeatPump switching to HEATING mode (room {roomTemp:F1}°C < target {targetTemp:F1}°C)");
                        isHeating = true;
                    }
                }
                else if (shouldCool)
                {
                    if (isHeating)
                    {
                        Log.Message($"HeatPump switching to COOLING mode (room {roomTemp:F1}°C > target {targetTemp:F1}°C)");
                        isHeating = false;
                    }
                }
                // If in dead zone (within threshold), maintain current mode
            }

            // Disable heating if outdoor temp too low (override manual mode if necessary)
            if (isHeating && !canHeat)
            {
                isHeating = false;
                manualModeOverride = false; // Clear override if conditions prevent heating
            }

            // Control heating vs cooling
            if (airconComp != null)
            {
                if (isHeating && canHeat)
                {
                    // In heating mode: disable DBH's cooling (heat is pushed in CompTick)
                    // Use reflection to disable the aircon comp
                    var compEnabled = airconComp.GetType().GetField("compEnabled",
                        System.Reflection.BindingFlags.Instance |
                        System.Reflection.BindingFlags.NonPublic |
                        System.Reflection.BindingFlags.Public);
                    if (compEnabled != null)
                    {
                        compEnabled.SetValue(airconComp, false);
                    }
                }
                else
                {
                    // In cooling mode: enable DBH's aircon comp to handle cooling
                    var compEnabled = airconComp.GetType().GetField("compEnabled",
                        System.Reflection.BindingFlags.Instance |
                        System.Reflection.BindingFlags.NonPublic |
                        System.Reflection.BindingFlags.Public);
                    if (compEnabled != null)
                    {
                        compEnabled.SetValue(airconComp, true);
                    }
                }
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
                return outdoorTemp >= MIN_HEATING_OUTDOOR_TEMP;
            }
        }

        /// <summary>
        /// Get efficiency from DBH's capacity system
        /// Efficiency = Available outdoor capacity / Total indoor capacity requested
        /// </summary>
        private float GetDBHEfficiency()
        {
            if (airconComp == null)
                return 1f;

            try
            {
                // Try to get efficiency from DBH's aircon component using reflection
                // CompRoomUnit might use a different property name or method
                var efficiencyProp = airconComp.GetType().GetProperty("Efficiency",
                    System.Reflection.BindingFlags.Instance |
                    System.Reflection.BindingFlags.NonPublic |
                    System.Reflection.BindingFlags.Public);

                if (efficiencyProp != null)
                {
                    var value = efficiencyProp.GetValue(airconComp);
                    if (value is float efficiency)
                        return efficiency;
                }

                // Try alternative property names
                var efficiencyField = airconComp.GetType().GetField("efficiency",
                    System.Reflection.BindingFlags.Instance |
                    System.Reflection.BindingFlags.NonPublic |
                    System.Reflection.BindingFlags.Public);

                if (efficiencyField != null)
                {
                    var value = efficiencyField.GetValue(airconComp);
                    if (value is float efficiency)
                        return efficiency;
                }
            }
            catch
            {
                // Reflection failed, return default
            }

            return 1f; // Default 100% efficiency if can't find the value
        }

        public float CurrentEfficiency => GetDBHEfficiency();

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref isHeating, "isHeating", false);
            Scribe_Values.Look(ref manualModeOverride, "manualModeOverride", false);
        }

        public override string CompInspectStringExtra()
        {
            string result = "";

            // Current mode
            string mode = isHeating ? "Heating" : "Cooling";
            result += $"Mode: {mode}\n";

            // Efficiency (from DBH capacity system)
            float efficiency = GetDBHEfficiency();
            result += $"Efficiency: {(efficiency * 100f).ToString("F0")}%\n";

            // Room and target temperatures
            Room room = parent.GetRoom(RegionType.Set_Passable);
            if (room != null && thermostatComp != null)
            {
                float roomTemp = room.Temperature;
                float targetTemp = GetTargetTemperature();
                result += $"Room: {roomTemp.ToStringTemperature()} / Target: {targetTemp.ToStringTemperature()}\n";
            }

            // Outdoor temperature
            if (parent?.Map != null)
            {
                float outdoorTemp = parent.Map.mapTemperature.OutdoorTemp;
                result += $"Outdoor: {outdoorTemp.ToStringTemperature()}";

                // Show warning if cannot heat
                if (room != null && thermostatComp != null &&
                    room.Temperature < (GetTargetTemperature() - MODE_THRESHOLD) && !CanHeat)
                {
                    result += $"\nHeating unavailable below {MIN_HEATING_OUTDOOR_TEMP.ToStringTemperature()} outdoor";
                }
            }

            return result;
        }

        public override void PostDraw()
        {
            base.PostDraw();

            // Draw vertical capacity bar matching DBH style
            // Shows efficiency: available outdoor capacity / indoor capacity requested
            GenDraw.FillableBarRequest r = default(GenDraw.FillableBarRequest);
            r.center = parent.DrawPos + Vector3.up * 0.1f;
            r.size = new Vector2(0.08f, 0.55f);
            r.fillPercent = GetDBHEfficiency();
            r.filledMat = CapacityFilled;
            r.unfilledMat = CapacityUnfilled;
            r.margin = 0.15f;
            r.rotation = Rot4.North;
            GenDraw.DrawFillableBar(r);
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            foreach (Gizmo gizmo in base.CompGetGizmosExtra())
            {
                yield return gizmo;
            }

            // Add mode toggle button (was indicator, now clickable)
            Command_Action modeToggle = new Command_Action
            {
                defaultLabel = isHeating ? "Heating" : "Cooling",
                defaultDesc = (manualModeOverride ? "[Manual] " : "[Auto] ") +
                    (isHeating
                        ? "Heat pump is in heating mode. Outdoor unit absorbing heat from outside air.\n\nClick to switch to cooling mode."
                        : "Heat pump is in cooling mode. Outdoor unit exhausting heat outside.\n\nClick to switch to heating mode."),
                icon = isHeating ? HeatingIcon : CoolingIcon,
                action = delegate
                {
                    // Toggle mode and enable manual override
                    isHeating = !isHeating;
                    manualModeOverride = true;

                    // Can't manually enable heating if outdoor temp too low
                    if (isHeating && !CanHeat)
                    {
                        isHeating = false;
                        Messages.Message("Cannot enable heating mode: outdoor temperature below " +
                            MIN_HEATING_OUTDOOR_TEMP.ToStringTemperature(),
                            parent, MessageTypeDefOf.RejectInput, false);
                    }
                }
            };

            // Color code the icon
            if (isHeating)
            {
                modeToggle.defaultIconColor = new Color(1f, 0.5f, 0.2f); // Orange for heating
            }
            else
            {
                modeToggle.defaultIconColor = new Color(0.4f, 0.7f, 1f); // Blue for cooling
            }

            // Show disabled if heating but can't heat
            if (isHeating && !CanHeat)
            {
                modeToggle.Disable("Outdoor temperature too low for heating");
            }

            yield return modeToggle;

            // Add auto mode button
            if (manualModeOverride)
            {
                Command_Action autoModeBtn = new Command_Action
                {
                    defaultLabel = "Auto Mode",
                    defaultDesc = "Return to automatic mode switching. System will automatically switch between heating and cooling based on room temperature.",
                    icon = ContentFinder<Texture2D>.Get("UI/Commands/Draft", false) ?? BaseContent.BadTex,
                    action = delegate
                    {
                        manualModeOverride = false;
                    }
                };
                yield return autoModeBtn;
            }
        }
    }
}
