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
        private static readonly Texture2D HeatingIcon = ContentFinder<Texture2D>.Get("UI/Commands/TempLower", false) ?? BaseContent.BadTex;
        private static readonly Texture2D CoolingIcon = ContentFinder<Texture2D>.Get("UI/Commands/TempRaise", false) ?? BaseContent.BadTex;

        private const float MODE_THRESHOLD = 2f; // Switch mode when 2°C away from target
        private const float MIN_HEATING_OUTDOOR_TEMP = -25f; // -25°C = -13°F

        private bool isHeating = false;
        private bool manualModeOverride = false; // Allow manual control
        private CompTempControl tempControl;
        private ThingComp airconComp; // DBH's CompAirconUnit

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            tempControl = parent.GetComp<CompTempControl>();

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

            if (parent.IsHashIntervalTick(60)) // Check every 60 ticks (1 second)
            {
                UpdateHeatPumpMode();
            }
        }

        private void UpdateHeatPumpMode()
        {
            if (tempControl == null)
                return;

            Room room = parent.GetRoom(RegionType.Set_Passable);
            if (room == null)
                return;

            float roomTemp = room.Temperature;
            float targetTemp = tempControl.targetTemperature;
            float outdoorTemp = parent.Map.mapTemperature.OutdoorTemp;

            // Check if outdoor temperature allows heating
            bool canHeat = outdoorTemp >= MIN_HEATING_OUTDOOR_TEMP;

            // Only auto-switch if not in manual override mode
            if (!manualModeOverride)
            {
                // Determine if we should be heating or cooling based on INDOOR temperature
                bool shouldHeat = roomTemp < (targetTemp - MODE_THRESHOLD);
                bool shouldCool = roomTemp > (targetTemp + MODE_THRESHOLD);

                // Switch modes based on need
                if (shouldHeat && canHeat)
                {
                    isHeating = true;
                }
                else if (shouldCool)
                {
                    isHeating = false;
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
                    // In heating mode: disable DBH's cooling and push heat ourselves
                    // Use reflection to disable the aircon comp
                    var compEnabled = airconComp.GetType().GetField("compEnabled",
                        System.Reflection.BindingFlags.Instance |
                        System.Reflection.BindingFlags.NonPublic |
                        System.Reflection.BindingFlags.Public);
                    if (compEnabled != null)
                    {
                        compEnabled.SetValue(airconComp, false);
                    }

                    // Push heat at the inverse rate of cooling (energyPerSecond = -21, so we push +21)
                    float heatPushRate = 21f;
                    GenTemperature.PushHeat(parent, heatPushRate);
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
