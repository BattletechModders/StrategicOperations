﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using BattleTech;
using BattleTech.UI;
using CustomUnits;
using UnityEngine;

namespace StrategicOperations.Framework
{
    public static class ResupplyUtils
    {
        public static void modifyAmmoCount(this AmmunitionBox box, int value)
        {
            box.StatCollection.ModifyStat("resupplyAMMO", -1, "CurrentAmmo", StatCollection.StatOperation.Int_Add, value);
        }
        public static void zeroAmmoCount(this AmmunitionBox box)
        {
            box.StatCollection.ModifyStat("resupplyAMMO", -1, "CurrentAmmo", StatCollection.StatOperation.Set, 0);
        }
        public static void modifyMechArmorValue(this Mech mech, ArmorLocation loc, float value)
        {
            mech.StatCollection.ModifyStat("resupplyARMOR", -1, mech.GetStringForArmorLocation(loc), StatCollection.StatOperation.Float_Add, value);
        }

        public static bool AreAnyWeaponsOutOfAmmo(this AbstractActor actor)
        {
            foreach (var weapon in actor.Weapons)
            {
                if (!weapon.HasAmmo) return false;
            }
            return true;
        }

        public static float GetDistanceToClosestDetectedResupply(this AbstractActor actor, Vector3 position)
        {
            var friendlyUnits = actor.team.VisibilityCache.GetAllFriendlies(actor).Where(x => !x.IsDead && !x.IsFlaggedForDeath);
            var num = -1f;
            var magnitude = -9999f;
            foreach (var friendly in friendlyUnits)
            {
                if (!friendly.GetStaticUnitTags().Contains(ModInit.modSettings.ResupplyConfig.ResupplyUnitTag)) continue;
                magnitude = (position - friendly.CurrentPosition).magnitude;
                if (num < 0f || magnitude < num)
                {
                    num = magnitude;
                }
            }
            return magnitude;
        }

        public static AbstractActor GetClosestDetectedResupply(this AbstractActor actor)
        {
            var friendlyUnits = actor.team.VisibilityCache.GetAllFriendlies(actor).Where(x => !x.IsDead && !x.IsFlaggedForDeath);
            var num = -1f;
            var distance = -9999f;
            AbstractActor resupplyActor = null;
            foreach (var friendly in friendlyUnits)
            {
                if (!friendly.GetStaticUnitTags().Contains(ModInit.modSettings.ResupplyConfig.ResupplyUnitTag)) continue;
                distance = Vector3.Distance(actor.CurrentPosition, friendly.CurrentPosition);
                if (num < 0f || distance < num)
                {
                    num = distance;
                    resupplyActor = friendly;
                }
            }
            return resupplyActor;
        }

        public static void InitiateShutdownForPhases(this AbstractActor actor, int phases)
        {
            if (actor.IsShutDown)
            {
                if (ModState.ResupplyShutdownPhases.ContainsKey(actor.GUID))
                {
                    ModState.ResupplyShutdownPhases[actor.GUID] += phases;
                }
                return;
            }
            //if (actor is Mech mech) mech.GenerateAndPublishHeatSequence(-1, true, false, actor.GUID);
            MessageCenterMessage invocation = new ShutdownInvocation(actor);
            actor.Combat.MessageCenter.PublishInvocationExternal(invocation);
            if (!ModState.ResupplyShutdownPhases.ContainsKey(actor.GUID))
            {
                ModState.ResupplyShutdownPhases.Add(actor.GUID, phases);
            }
        }

        public static void ProcessResupplyUnit(this AbstractActor actor, AbstractActor resupplyActor)
        {
            var searchForSpammy = !string.IsNullOrEmpty(ModInit.modSettings.ResupplyConfig.SPAMMYAmmoDefId);
            foreach (var ammoBoxToFill in actor.ammoBoxes)
            {
                if (ammoBoxToFill.CurrentAmmo < ammoBoxToFill.AmmoCapacity)
                {
                    var initialMissingAmmoCount = ammoBoxToFill.AmmoCapacity - ammoBoxToFill.CurrentAmmo;
                    ModInit.modLog?.Trace?.Write($"[ProcessResupplyUnit - Regular Ammo] - Found box {ammoBoxToFill.Description.UIName} needs {initialMissingAmmoCount} shots added.");
                    var magicBoxes = new List<AmmunitionBox>();
                    foreach (var resupplyBox in resupplyActor.ammoBoxes)
                    {
                        if (resupplyBox.ammoDef.Description.Id == ammoBoxToFill.ammoDef.Description.Id)
                        {
                            if (resupplyBox.CurrentAmmo <= 0) continue;
                            var currentMissingAmmoCount =
                                ammoBoxToFill.AmmoCapacity - ammoBoxToFill.CurrentAmmo;
                            ModInit.modLog?.Trace?.Write($"[ProcessResupplyUnit - Regular Ammo] - Found resupply box, has {resupplyBox.CurrentAmmo} available.");
                            if (resupplyBox.CurrentAmmo >= currentMissingAmmoCount)
                            {
                                ammoBoxToFill.modifyAmmoCount(currentMissingAmmoCount);
                                resupplyBox.modifyAmmoCount(-currentMissingAmmoCount);
                                ModInit.modLog?.Trace?.Write($"[ProcessResupplyUnit - Regular Ammo] - Full Resupply complete for ammoBox {ammoBoxToFill.Description.UIName}. Ammobox now has {ammoBoxToFill.CurrentAmmo}/{ammoBoxToFill.AmmoCapacity} ammo, resupply box has {resupplyBox.CurrentAmmo}/{resupplyBox.AmmoCapacity} remaining");
                                break;
                            }
                            else
                            {
                                ammoBoxToFill.modifyAmmoCount(resupplyBox.CurrentAmmo);
                                resupplyBox.modifyAmmoCount(-resupplyBox.CurrentAmmo);
                                ModInit.modLog?.Trace?.Write($"[ProcessResupplyUnit - Regular Ammo] - Partial Resupply complete for ammoBox {ammoBoxToFill.Description.UIName}. Ammobox now has {ammoBoxToFill.CurrentAmmo}/{ammoBoxToFill.AmmoCapacity} ammo, resupply box has {resupplyBox.CurrentAmmo}/{resupplyBox.AmmoCapacity} remaining");
                            }
                        }
                        else if (searchForSpammy && resupplyBox.ammoDef.Description.Id == ModInit.modSettings.ResupplyConfig.SPAMMYAmmoDefId)
                        {
                            magicBoxes.Add(resupplyBox);
                        }
                    }

                    if (ModInit.modSettings.ResupplyConfig.SPAMMYBlackList.Contains(
                            ammoBoxToFill.ammoDef.Description.Id)) continue;
                    if (ammoBoxToFill.CurrentAmmo < ammoBoxToFill.AmmoCapacity && magicBoxes.Count > 0)
                    {
                        var missingAmmoForMagicBox = ammoBoxToFill.AmmoCapacity - ammoBoxToFill.CurrentAmmo;
                        ModInit.modLog?.Trace?.Write(
                            $"[ProcessResupplyUnit - SPAMMY] - Found box {ammoBoxToFill.Description.UIName} needs {missingAmmoForMagicBox} shots added.");
                        var sourceTonnagePerShot = ammoBoxToFill.tonnage / ammoBoxToFill.AmmoCapacity;
                        var sourceTonnageNeeded = missingAmmoForMagicBox * sourceTonnagePerShot;
                        foreach (var magicBox in magicBoxes)
                        {
                            if (magicBox.CurrentAmmo <= 0) continue;
                            var magicTonnagePerShot = magicBox.tonnage / magicBox.AmmoCapacity;
                            var magicTonnageAvailable = magicTonnagePerShot * magicBox.CurrentAmmo;
                            var replacementSourceShotsAvailable = Mathf.FloorToInt(magicTonnageAvailable / sourceTonnagePerShot);
                            var magicShotsPerSource = sourceTonnagePerShot/magicTonnagePerShot;
                            var totalMagicShotsConsumed = Mathf.FloorToInt(Mathf.Min(magicShotsPerSource * replacementSourceShotsAvailable, magicShotsPerSource * missingAmmoForMagicBox));
                            if (replacementSourceShotsAvailable >= missingAmmoForMagicBox)
                            {
                                ammoBoxToFill.modifyAmmoCount(missingAmmoForMagicBox);
                                magicBox.modifyAmmoCount(-totalMagicShotsConsumed);
                                ModInit.modLog?.Trace?.Write(
                                    $"[ProcessResupplyUnit - Use SpAce Magic Modular (by Yang) Ammo!] - Full Resupply complete for ammoBox {ammoBoxToFill.Description.UIName}. Ammobox now has {ammoBoxToFill.CurrentAmmo}/{ammoBoxToFill.AmmoCapacity} ammo, SPAMMY box has {magicBox.CurrentAmmo}/{magicBox.AmmoCapacity} remaining. \n\nMathDumps: Replacement of ammo needs to use {sourceTonnageNeeded} tons of SPAMMY. SPAMMY has {magicTonnageAvailable} tons available at {magicTonnagePerShot} per shot for total SPAMMY shots needed of {totalMagicShotsConsumed}");
                                break;
                            }
                            else
                            {
                                ammoBoxToFill.modifyAmmoCount(replacementSourceShotsAvailable);
                                magicBox.zeroAmmoCount();
                                ModInit.modLog?.Trace?.Write(
                                    $"[ProcessResupplyUnit - Use SpAce Magic Modular (by Yang) Ammo!] - Partial Resupply complete for ammoBox {ammoBoxToFill.Description.UIName}. Ammobox now has {ammoBoxToFill.CurrentAmmo}/{ammoBoxToFill.AmmoCapacity} ammo, SPAMMY box has {magicBox.CurrentAmmo}/{magicBox.AmmoCapacity} remaining. \n\nMathDumps: Replacement of ammo needs to use {sourceTonnageNeeded} tons of SPAMMY. SPAMMY has {magicTonnageAvailable} tons available at {magicTonnagePerShot} per shot for total SPAMMY shots needed of {totalMagicShotsConsumed}");
                            }
                        }
                    }
                }
            }

            if (!string.IsNullOrEmpty(ModInit.modSettings.ResupplyConfig.ArmorSupplyAmmoDefId))
            {
                if (actor is Mech mech)
                {
                    foreach (ArmorLocation loc in ModState.MechArmorSwarmOrder)
                    {
                        if (mech.IsLocationDestroyed(MechStructureRules.GetChassisLocationFromArmorLocation(loc)))
                            continue;
                        var initialCapped = HUDMechArmorReadout.GetInitialArmorForLocation(mech.MechDef, loc) * ModInit.modSettings.ResupplyConfig.ArmorRepairMax;
                        var current = mech.ArmorForLocation((int) loc);
                        if (current <= initialCapped)
                        {
                            var missingArmor = initialCapped - current;
                            foreach (var ammobox in resupplyActor.ammoBoxes)
                            {
                                if (ammobox.ammunitionBoxDef.AmmoID ==
                                    ModInit.modSettings.ResupplyConfig.ArmorSupplyAmmoDefId)
                                {
                                    if (ammobox.CurrentAmmo <= 0) continue;
                                    ModInit.modLog?.Trace?.Write(
                                        $"[ProcessResupplyUnit - ARMOR] - Location {loc} can replace {missingArmor} points of armor. Armor Ammo has {ammobox.CurrentAmmo} available.");
                                    var armorAmmoNeeded = Mathf.CeilToInt(missingArmor);
                                    if (ammobox.CurrentAmmo >= armorAmmoNeeded)
                                    {
                                        mech.modifyMechArmorValue(loc, missingArmor);
                                        ammobox.modifyAmmoCount(-armorAmmoNeeded);
                                        ModInit.modLog?.Trace?.Write($"[ProcessResupplyUnit - ARMOR] - Location {loc} replaced {missingArmor} points of armor, using {armorAmmoNeeded} armorAmmo. {ammobox.CurrentAmmo} remains.");
                                    }
                                    else
                                    {
                                        mech.modifyMechArmorValue(loc, ammobox.CurrentAmmo);
                                        ammobox.zeroAmmoCount();
                                        ModInit.modLog?.Trace?.Write($"[ProcessResupplyUnit - ARMOR] - Location {loc} replaced {missingArmor} points of armor, using {armorAmmoNeeded} armorAmmo. {ammobox.CurrentAmmo} remains.");
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        public static void UpdateResupplyTeams(this CombatGameState combat)
        {
            foreach (var actor in combat.AllActors)
            {
                if (actor.GetStaticUnitTags().Contains(ModInit.modSettings.ResupplyConfig.ResupplyUnitTag))
                {
                    foreach (var team in combat.Teams)
                    {
                        if (ModState.TeamsWithResupply.Contains(team.GUID)) continue;
                        if (team.IsFriendly(actor.team)) ModState.TeamsWithResupply.Add(team.GUID);
                    }
                    if (!ModState.TeamsWithResupply.Contains(actor.team.GUID)) ModState.TeamsWithResupply.Add(actor.team.GUID);
                }
            }
        }

        public static void UpdateResupplyAbilitiesAllActors(this CombatGameState combat)
        {
            var actors = combat.AllActors;
            foreach (var unit in actors)
            {
                if (!ModState.TeamsWithResupply.Contains(unit.team.GUID)) continue;
                if (unit.GetStaticUnitTags().Contains(ModInit.modSettings.ResupplyConfig.ResupplyUnitTag)) continue;
                if (unit.GetPilot().Abilities
                        .All(x => x.Def.Id != ModInit.modSettings.ResupplyConfig.ResupplyAbilityID) &&
                    unit.ComponentAbilities.All(y =>
                        y.Def.Id != ModInit.modSettings.ResupplyConfig.ResupplyAbilityID))
                {
                    unit.Combat.DataManager.AbilityDefs.TryGet(ModInit.modSettings.ResupplyConfig.ResupplyAbilityID,
                        out var def);
                    var ability = new Ability(def);
                    ModInit.modLog?.Trace?.Write(
                        $"[UpdateResupplyAbilitiesAllActors] Adding {ability.Def?.Description?.Id} to {unit.DisplayName}.");
                    ability.Init(unit.Combat);
                    unit.GetPilot().Abilities.Add(ability);
                    unit.GetPilot().ActiveAbilities.Add(ability);
                }
            }
        }
    }
}