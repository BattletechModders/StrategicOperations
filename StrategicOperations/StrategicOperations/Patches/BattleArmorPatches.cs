using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BattleTech;
using BattleTech.UI;
using Harmony;
using StrategicOperations.Framework;
using UnityEngine;

namespace StrategicOperations.Patches
{
    class BattleArmorPatches
    {

        [HarmonyPatch(typeof(Ability), "Activate",
            new Type[] {typeof(AbstractActor), typeof(ICombatant)})]
        public static class Ability_Activate
        {
            public static void Postfix(Ability __instance, AbstractActor creator, ICombatant target)
            {
                if (creator == null) return;
                if (!creator.IsSwarmingUnit() && !creator.IsMountedUnit())
                {
                    if (!ModState.CachedActiveComponents.ContainsKey(creator.GUID))
                    {
                        ModState.CachedActiveComponents.Add(creator.GUID, new List<Transform>());
                    }

                    if (__instance.IsAvailable && target is AbstractActor)
                    {
                        if (__instance.Def.Id == ModInit.modSettings.BattleArmorMountID)
                        {
                            if (target.team.IsFriendly(creator.team))
                            {
                                // need to check for destroyed locations and cache them in state, then restore after calling TrooperSquad.InitGameRep. I think. unless maybe I can just turn off the mesh but leave the gameobject? might be simpler/not require CU. lets try that first. - dont think this'll work, probably need to start on 
                                creator.GameRep.IsTargetable = false;
                                creator.TeleportActor(target.CurrentPosition);
                                
                                var components = creator.GameRep.transform.Cast<Transform>().ToList();
                                foreach (var component in components)
                                {
                                    ModInit.modLog.LogTrace(
                                        $"[Ability.Activate - BattleArmorMountID] Disabling component in creator.GameRep.tranform: {component.name}.");
                                    ModState.CachedActiveComponents[creator.GUID].Add(component);
                                    component.gameObject.SetActive(false);


                                    if (component.gameObject.activeSelf && component.GetComponent<MechRepresentation>() != null && false) //disable this block rn
                                    {
                                        ModInit.modLog.LogTrace(
                                            $"[Ability.Activate - BattleArmorMountID] Checking component {component.name} was gameObject activeSelf and has child component MechRepresentation (!= null).");
                                        ModState.CachedActiveComponents[creator.GUID].Add(component);

                                        var mesh = component.Find("mesh");
                                        mesh.gameObject.SetActive(false);
                                    }
                                }

                                creator.GameRep.enabled = false; // 
                                //creator.GameRep.gameObject.SetActive(false);
                                //CombatMovementReticle.Instance.RefreshActor(creator); // or just end activation completely? definitely on use.
                                creator.OnActivationEnd(creator.GUID, 1);
                                ModState.PositionLockMount.Add(creator.GUID, target.GUID);
                                ModInit.modLog.LogMessage(
                                    $"[Ability.Activate - BattleArmorMountID] Added PositionLockMount with rider  {creator.DisplayName} {creator.GUID} and carrier {target.DisplayName} {target.GUID}.");
                            }
                        }
                        else if (__instance.Def.Id == ModInit.modSettings.BattleArmorSwarmID)
                        {
                            if (target.team.IsEnemy(creator.team))
                            {
                                creator.GameRep.IsTargetable = false;
                                creator.TeleportActor(target.CurrentPosition);

                                var components = creator.GameRep.transform.Cast<Transform>().ToList();
                                foreach (var component in components)
                                {
                                    ModInit.modLog.LogTrace(
                                        $"[Ability.Activate - BattleArmorMountID] Disabling component in creator.GameRep.tranform: {component.name}.");
                                    ModState.CachedActiveComponents[creator.GUID].Add(component);
                                    component.gameObject.SetActive(false);


                                    if (component.gameObject.activeSelf && component.GetComponent<MechRepresentation>() != null && false) //disable this block rn
                                    {
                                        ModInit.modLog.LogTrace(
                                            $"[Ability.Activate - BattleArmorMountID] Checking component {component.name} was gameObject activeSelf and has child component MechRepresentation (!= null).");
                                        ModState.CachedActiveComponents[creator.GUID].Add(component);

                                        var mesh = component.Find("mesh");
                                        mesh.gameObject.SetActive(false);
                                    }
                                }

                                creator.GameRep.enabled = false;
                                //creator.GameRep.gameObject.SetActive(false);
                                //CombatMovementReticle.Instance.RefreshActor(creator);
                                creator.OnActivationEnd(creator.GUID, 1);
                                ModState.PositionLockSwarm.Add(creator.GUID, target.GUID);
                                ModInit.modLog.LogMessage(
                                    $"[Ability.Activate - BattleArmorSwarmID] Added PositionLockSwarm with rider  {creator.DisplayName} {creator.GUID} and carrier {target.DisplayName} {target.GUID}.");
                            }
                        }
                    }
                }

                else if (creator.IsSwarmingUnit() || creator.IsMountedUnit())
                {
                    if (!ModState.CachedActiveComponents.ContainsKey(creator.GUID))
                    {
                        ModState.CachedActiveComponents.Add(creator.GUID, new List<Transform>());
                    }
                    if (__instance.IsAvailable && target is AbstractActor targetActor)
                    {
                        if (__instance.Def.Id == ModInit.modSettings.BattleArmorMountID)
                        {
                            if (target.team.IsFriendly(creator.team))
                            {

                                creator.GameRep.IsTargetable = true;
                                var newPos = BattleArmorUtils.FetchAdjacentHex(targetActor);
                                
                                creator.TeleportActor(newPos);

                                if (ModState.CachedActiveComponents.ContainsKey(creator.GUID) && false)
                                {
                                    var components = creator.GameRep.transform.Cast<Transform>().ToList();
                                    foreach (var component in components)
                                    {
                                        ModInit.modLog.LogTrace(
                                            $"[Ability.Activate - BattleArmorMountID] Checking uf  component in creator.GameRep.tranform is in Modstate: {component.name}.");
                                        if (ModState.CachedActiveComponents[creator.GUID].Any(x => x.gameObject.name == component.gameObject.name))
                                        {
                                            component.gameObject.SetActive(true);
                                            ModInit.modLog.LogTrace(
                                                $"[Ability.Activate - BattleArmorMountID] Reenabling component: {component.name}.");
                                        }
                                    }
                                }

                                creator.GameRep.enabled = true;
                                creator.InitGameRep(null);
                                //creator.GameRep.gameObject.SetActive(true);

                                CombatMovementReticle.Instance.RefreshActor(creator);
                                // need to teleport to random adjacent hex. maybe immediately give evasion? or allow movement after?
                                ModState.PositionLockMount.Remove(creator.GUID);
                                ModInit.modLog.LogMessage(
                                    $"[Ability.Activate - BattleArmorMountID] Removing PositionLockMount with rider  {creator.DisplayName} {creator.GUID} and carrier {target.DisplayName} {target.GUID}.");
                            }
                        }
                        else if (__instance.Def.Id == ModInit.modSettings.BattleArmorSwarmID)
                        {
                            if (target.team.IsEnemy(creator.team))
                            {

                                creator.GameRep.IsTargetable = true;
                                var newPos = BattleArmorUtils.FetchAdjacentHex(targetActor);

                                creator.TeleportActor(newPos);
                                if (ModState.CachedActiveComponents.ContainsKey(creator.GUID) && false)
                                {
                                    var components = creator.GameRep.transform.Cast<Transform>().ToList();
                                    foreach (var component in components)
                                    {
                                        ModInit.modLog.LogTrace(
                                            $"[Ability.Activate - BattleArmorMountID] Checking uf  component in creator.GameRep.tranform is in Modstate: {component.name}.");
                                        if (ModState.CachedActiveComponents[creator.GUID].Any(x => x.gameObject.name == component.gameObject.name))
                                        {
                                            component.gameObject.SetActive(true);
                                            ModInit.modLog.LogTrace(
                                                $"[Ability.Activate - BattleArmorMountID] Reenabling component: {component.name}.");
                                        }
                                    }
                                }

                                creator.GameRep.enabled = true;
                                creator.InitGameRep(null);
                                //creator.GameRep.gameObject.SetActive(true);
                                CombatMovementReticle.Instance.RefreshActor(creator);
                                ModState.PositionLockSwarm.Remove(creator.GUID);
                                ModInit.modLog.LogMessage(
                                    $"[Ability.Activate - BattleArmorSwarmID] Removing PositionLockSwarm with rider  {creator.DisplayName} {creator.GUID} and carrier {target.DisplayName} {target.GUID}.");
                            }
                        }
                    }
                }
            }
        }

        [HarmonyPatch(typeof(CombatHUDMechwarriorTray), "ResetMechwarriorButtons",
            new Type[] {typeof(AbstractActor)})]
        public static class CombatHUDMechwarriorTray_ResetMechwarriorButtons
        {
            [HarmonyPriority(Priority.Last)]
            public static void Postfix(CombatHUDMechwarriorTray __instance, AbstractActor actor)
            {
                if (actor == null) return;
                if (actor.IsMountedUnit())
                {
                    ModInit.modLog.LogMessage(
                        $"[CombatHUDMechwarriorTray.ResetMechwarriorButtons] Actor {actor.DisplayName} {actor.GUID} found in PositionLockMount. Disabling buttons.");
                    __instance.FireButton.DisableButton();
                    __instance.MoveButton.DisableButton();
                    __instance.SprintButton.DisableButton();
                    __instance.JumpButton.DisableButton();
//                    __instance.DoneWithMechButton.DisableButton(); // we want this button
//                    __instance.EjectButton.DisableButton(); // we probably want this one too

                    var moraleButtons = Traverse.Create(__instance).Property("MoraleButtons")
                        .GetValue<CombatHUDActionButton[]>();

                    foreach (var moraleButton in moraleButtons)
                    {
                        moraleButton.DisableButton();
                    }

                    var abilityButtons = Traverse.Create(__instance).Property("AbilityButtons")
                        .GetValue<CombatHUDActionButton[]>();

                    foreach (var abilityButton in abilityButtons)
                    {
                        if (abilityButton?.Ability?.Def?.Id == ModInit.modSettings.BattleArmorMountID)
                            abilityButton?.DisableButton();
                    }
                    return;
                }
                else if (actor.IsSwarmingUnit())
                {
                    ModInit.modLog.LogMessage(
                        $"[CombatHUDMechwarriorTray.ResetMechwarriorButtons] Actor {actor.DisplayName} {actor.GUID} found in PositionLockSwarm. Disabling buttons.");
                    __instance.FireButton.DisableButton();
                    __instance.MoveButton.DisableButton();
                    __instance.SprintButton.DisableButton();
                    __instance.JumpButton.DisableButton();
                    //                    __instance.DoneWithMechButton.DisableButton(); // we want this button
                    //                    __instance.EjectButton.DisableButton(); // we probably want this one too

                    var moraleButtons = Traverse.Create(__instance).Property("MoraleButtons")
                        .GetValue<CombatHUDActionButton[]>();

                    foreach (var moraleButton in moraleButtons)
                    {
                        moraleButton.DisableButton();
                    }

                    var abilityButtons = Traverse.Create(__instance).Property("AbilityButtons")
                        .GetValue<CombatHUDActionButton[]>();

                    foreach (var abilityButton in abilityButtons)
                    {
                        if (abilityButton?.Ability?.Def?.Id == ModInit.modSettings.BattleArmorSwarmID)
                            abilityButton?.DisableButton();
                    }
                    return;
                }
            }
        }
    }
}