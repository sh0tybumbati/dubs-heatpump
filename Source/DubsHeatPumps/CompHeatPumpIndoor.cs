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

        private static readonly Material HeatingCapacityFilled = SolidColorMaterials.SimpleSolidColorMaterial(new Color(1f, 0.5f, 0.2f)); // Orange
        private static readonly Material CoolingCapacityFilled = SolidColorMaterials.SimpleSolidColorMaterial(new Color(0.4f, 0.7f, 1f)); // Blue
        private static readonly Material CapacityUnfilled = SolidColorMaterials.SimpleSolidColorMaterial(new Color(0.3f, 0.3f, 0.3f)); // Dark gray

        private const float MODE_THRESHOLD = 2f; // Switch mode when 2°C away from target
        private const float MIN_HEATING_OUTDOOR_TEMP = -25f; // -25°C = -13°F

        private bool isHeating = false;
        private bool manualModeOverride = false; // Allow manual control
        private CompTempControl tempControl;
        private ThingComp airconComp; // DBH's CompAirconUnit
        private CompPowerTrader powerComp;

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);

            // Try to get CompTempControl - DBH uses CompThermostat which inherits from it
            tempControl = parent.GetComp<CompTempControl>();
            if (tempControl == null)
            {
                // Try finding by base type name if direct get fails
                tempControl = parent.AllComps.Find(c => c is CompTempControl) as CompTempControl;
            }

            powerComp = parent.GetComp<CompPowerTrader>();

            // Get DBH's aircon component using reflection (can't reference directly)
            airconComp = parent.AllComps.Find(c => c.GetType().Name == "CompAirconUnit");

            // Initialize mode based on current conditions if not loading from save
            if (!respawningAfterLoad)
            {
                Room room = parent.GetRoom(RegionType.Set_Passable);
                if (room != null && tempControl != null)
                {
                    // Start in heating mode if room is colder than target
                    isHeating = room.Temperature < tempControl.targetTemperature;
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
                    // Push heat every tick to match vanilla heater performance
                    // Use GenTemperature.ControlTemperatureTempChange for accurate heating
                    float heatPerTick = 21f / 60f; // 21 heat per second = 0.35 per tick

                    Room room = parent.GetRoom(RegionType.Set_Passable);
                    if (room != null && !room.UsesOutdoorTemperature)
                    {
                        float heatPush = heatPerTick / room.CellCount;
                        room.Temperature += heatPush;

                        // Debug log every 5 seconds
                        if (Find.TickManager.TicksGame % 300 == 0)
                        {
                            Log.Message($"HeatPump HEATING: Pushing {heatPush:F4}°C to room (size: {room.CellCount}, temp before: {(room.Temperature - heatPush):F2}°C, after: {room.Temperature:F2}°C)");
                        }
                    }
                }
                else
                {
                    if (Find.TickManager.TicksGame % 300 == 0)
                    {
                        Log.Warning($"HeatPump can't heat: powerComp={(powerComp != null ? "exists" : "null")}, PowerOn={(powerComp?.PowerOn ?? false)}");
                    }
                }
            }
            else
            {
                if (Find.TickManager.TicksGame % 300 == 0 && (isHeating || !CanHeat))
                {
                    Log.Warning($"HeatPump not heating: isHeating={isHeating}, CanHeat={CanHeat}");
                }
            }
        }

        private void UpdateHeatPumpMode()
        {
            if (tempControl == null)
            {
                Log.Warning("HeatPump: tempControl is null");
                return;
            }

            Room room = parent.GetRoom(RegionType.Set_Passable);
            if (room == null)
            {
                Log.Warning("HeatPump: room is null");
                return;
            }

            float roomTemp = room.Temperature;
            float targetTemp = tempControl.targetTemperature;
            float outdoorTemp = parent.Map.mapTemperature.OutdoorTemp;

            // Check if outdoor temperature allows heating
            bool canHeat = outdoorTemp >= MIN_HEATING_OUTDOOR_TEMP;

            // Log every 5 seconds for debugging
            if (Find.TickManager.TicksGame % 300 == 0)
            {
                Log.Message($"HeatPump Debug - Room: {roomTemp:F1}°C, Target: {targetTemp:F1}°C, Outdoor: {outdoorTemp:F1}°C, " +
                    $"Mode: {(isHeating ? "Heat" : "Cool")}, ManualOverride: {manualModeOverride}, CanHeat: {canHeat}, PowerOn: {(powerComp?.PowerOn ?? false)}");
            }

            // Only auto-switch if not in manual override mode
            if (!manualModeOverride)
            {
                // Determine if we should be heating or cooling based on INDOOR temperature
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
        /// Calculate heat pump efficiency based on outdoor temperature
        /// Returns coefficient of performance (COP) - higher is more efficient
        /// Heating: Better efficiency when outdoor temp is warmer
        /// Cooling: Better efficiency when outdoor temp is cooler
        /// </summary>
        private float CalculateEfficiency()
        {
            if (parent?.Map == null)
                return 1f;

            float outdoorTemp = parent.Map.mapTemperature.OutdoorTemp;
            Room room = parent.GetRoom(RegionType.Set_Passable);
            if (room == null || tempControl == null)
                return 1f;

            float indoorTemp = room.Temperature;
            float targetTemp = tempControl.targetTemperature;
            float tempDifference = Mathf.Abs(outdoorTemp - targetTemp);

            if (isHeating)
            {
                // Heating efficiency: decreases as outdoor temp gets colder
                // At 20°C outdoor: ~100% efficient (COP 4.0)
                // At 0°C outdoor: ~75% efficient (COP 3.0)
                // At -20°C outdoor: ~50% efficient (COP 2.0)
                float efficiencyFactor = Mathf.Lerp(1f, 0.5f, Mathf.Clamp01((20f - outdoorTemp) / 40f));
                return efficiencyFactor;
            }
            else
            {
                // Cooling efficiency: decreases as outdoor temp gets hotter
                // At 20°C outdoor: ~100% efficient
                // At 35°C outdoor: ~75% efficient
                // At 50°C outdoor: ~50% efficient
                float efficiencyFactor = Mathf.Lerp(1f, 0.5f, Mathf.Clamp01((outdoorTemp - 20f) / 30f));
                return efficiencyFactor;
            }
        }

        public float CurrentEfficiency => CalculateEfficiency();

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

            // Efficiency
            float efficiency = CalculateEfficiency();
            result += $"Efficiency: {(efficiency * 100f).ToString("F0")}%\n";

            // Room and target temperatures
            Room room = parent.GetRoom(RegionType.Set_Passable);
            if (room != null && tempControl != null)
            {
                float roomTemp = room.Temperature;
                float targetTemp = tempControl.targetTemperature;
                result += $"Room: {roomTemp.ToStringTemperature()} / Target: {targetTemp.ToStringTemperature()}\n";
            }

            // Outdoor temperature
            if (parent?.Map != null)
            {
                float outdoorTemp = parent.Map.mapTemperature.OutdoorTemp;
                result += $"Outdoor: {outdoorTemp.ToStringTemperature()}";

                // Show warning if cannot heat
                if (room != null && tempControl != null &&
                    room.Temperature < (tempControl.targetTemperature - MODE_THRESHOLD) && !CanHeat)
                {
                    result += $"\nHeating unavailable below {MIN_HEATING_OUTDOOR_TEMP.ToStringTemperature()} outdoor";
                }
            }

            return result;
        }

        public override void PostDraw()
        {
            base.PostDraw();

            // Draw capacity bar showing heat pump capacity usage
            // The capacity value (120) comes from the XML CompProperties_CompAirconUnit
            GenDraw.FillableBarRequest r = default(GenDraw.FillableBarRequest);
            r.center = parent.DrawPos + Vector3.up * 0.1f;
            r.size = new Vector2(0.55f, 0.08f);

            // Show 100% capacity usage when active (120/120), 0% when off
            bool isActive = parent.GetComp<CompPowerTrader>()?.PowerOn ?? false;
            r.fillPercent = isActive ? 1f : 0f;

            // Color the bar based on current mode
            r.filledMat = isHeating ? HeatingCapacityFilled : CoolingCapacityFilled;
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
