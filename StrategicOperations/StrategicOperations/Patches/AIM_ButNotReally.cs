using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BattleTech;
using BattleTech.UI;
using CustAmmoCategories;
using CustomUnits;
using Harmony;
using Localize;
using UnityEngine;

namespace StrategicOperations.Patches
{
    public class AIM_ButNotReally
    {
        [HarmonyPatch(typeof(Weapon), "InitStats")]
        public static class Weapon_InitStats
        {
            public static void Prefix(Weapon __instance)
            {
                __instance.StatCollection.AddStatistic<float>("APArmorShardsModWeaponMultiplier", 1f);
                __instance.StatCollection.AddStatistic<float>("APMaxArmorThicknessWeaponMultiplier", 1f);
                __instance.StatCollection.AddStatistic<float>("APCriticalChanceMultiplierWeaponMultiplier", 1f);
            }
        }

        [HarmonyPatch(typeof(CustomAmmoCategories), "APArmorShardsMod")]
        public static class CustomAmmoCategories_APArmorShardsMod_WeaponExt
        {
            public static void Postfix(Weapon weapon, ref float __result)
            {
                if (__result > 0f)
                {
                    __result *= weapon.StatCollection.GetValue<float>("APArmorShardsModWeaponMultiplier");
                }
            }
        }

        [HarmonyPatch(typeof(CustomAmmoCategories), "APMaxArmorThickness")]
        public static class CustomAmmoCategories_APMaxArmorThickness_WeaponExt
        {
            public static void Postfix(Weapon weapon, ref float __result)
            {
                if (__result > 0f)
                {
                    __result *= weapon.StatCollection.GetValue<float>("APMaxArmorThicknessWeaponMultiplier");
                }
            }
        }

        [HarmonyPatch(typeof(CustomAmmoCategories), "APCriticalChanceMultiplier")]
        public static class CustomAmmoCategories_APCriticalChanceMultiplier_WeaponExt
        {
            public static void Postfix(Weapon weapon, ref float __result)
            {
                if (__result > 0f)
                {
                    __result *= weapon.StatCollection.GetValue<float>("APCriticalChanceMultiplierWeaponMultiplier");
                }
            }
        }

        [HarmonyPatch(typeof(CombatHUDFakeVehicleArmorHover), "setToolTipInfo", new Type[] {typeof(Mech), typeof(ChassisLocations)})]
        public static class CombatHUDFakeVehicleArmorHover_setToolTipInfo
        {
            static bool Prepare() => ModInit.modSettings.ShowAmmoInVehicleTooltips;
            
            public static bool Prefix(CombatHUDFakeVehicleArmorHover __instance, Mech vehicle, ChassisLocations location)
            {
                if (!ModInit.modSettings.EnforceIFFForAmmoTooltips || (ModInit.modSettings.EnforceIFFForAmmoTooltips &&
                    vehicle.team.IsFriendly(vehicle.Combat.LocalPlayerTeam)))
                {
                    var tooltip = Traverse.Create(__instance).Property("ToolTip").GetValue<CombatHUDTooltipHoverElement>();
                    tooltip.BuffStrings.Clear();
                    tooltip.DebuffStrings.Clear();
                    tooltip.BasicString = new Text(Vehicle.GetLongChassisLocation(location.toVehicleLocation()),
                        Array.Empty<object>());
                    for (int index = 0; index < vehicle.allComponents.Count; index++)
                    {
                        MechComponent allComponent = vehicle.allComponents[index];
                        if (allComponent.Location == (int) location)
                        {
                            string componentName = allComponent.UIName.ToString(true);
                            int allAmmo = 1;
                            Weapon weaponComp;
                            AmmunitionBox ammo;
                            if ((weaponComp = (allComponent as Weapon)) != null &&
                                !weaponComp.AmmoCategoryValue.Is_NotSet)
                            {
                                componentName = string.Concat(new object[]
                                {
                                    componentName,
                                    "<size=80%> (x",
                                    allAmmo = weaponComp.CurrentAmmo,
                                    ")"
                                });
                            }
                            else if ((ammo = (allComponent as AmmunitionBox)) != null)
                            {
                                int curr = ammo.CurrentAmmo;
                                int max = ammo.AmmoCapacity;
                                componentName = string.Concat(new object[]
                                {
                                    componentName,
                                    "<size=80%> (",
                                    curr,
                                    "/",
                                    max,
                                    ")"
                                });
                                if (curr < max / 2)
                                {
                                    componentName = "<#808080>" + componentName;
                                }
                            }

                            Weapon weapon = allComponent as Weapon;
                            if (allComponent.DamageLevel < ComponentDamageLevel.NonFunctional &&
                                (weapon == null || weapon.HasAmmo))
                            {
                                tooltip.BuffStrings.Add(new Text(componentName));
                            }
                            else
                            {
                                tooltip.DebuffStrings.Add(new Text(componentName));
                            }
                        }
                    }
                    return false;
                }
                return true;
            }
        }
    }
}
