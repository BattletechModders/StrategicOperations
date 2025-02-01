using System.Collections.Generic;
using System.Linq;
using BattleTech;
using BattleTech.UI;
using CustAmmoCategories;
using CustAmmoCategoriesPatches;
using CustomComponents;
using CustomUnits;
using UnityEngine;

namespace StrategicOperations.Framework
{
    public static class ResupplyUtils
    {
        
        public const string ResupplyUnitStat = "IsResupplyUnit";
        
        public static bool AreAnyWeaponsOutOfAmmo(this AbstractActor actor)
        {
            foreach (var weapon in actor.Weapons)
            {
                if (!weapon.HasAmmo) return false;
            }
            return true;
        }

        public static AbstractActor GetClosestDetectedResupply(this AbstractActor actor)
        {
            var friendlyUnits = actor.team.VisibilityCache.GetAllFriendlies(actor);
            var num = -1f;
            var distance = -9999f;
            AbstractActor resupplyActor = null;
            foreach (var friendly in friendlyUnits)
            {
                if (!friendly.IsResupplyUnit) continue;
                distance = Vector3.Distance(actor.CurrentPosition, friendly.CurrentPosition);
                if (num < 0f || distance < num)
                {
                    num = distance;
                    resupplyActor = friendly;
                }
            }
            return resupplyActor;
        }

        public static float GetDistanceToClosestDetectedResupply(this AbstractActor actor, Vector3 position)
        {
            var friendlyUnits = actor.team.VisibilityCache.GetAllFriendlies(actor);
            var num = -1f;
            var magnitude = -9999f;
            foreach (var friendly in friendlyUnits)
            {
                if (!friendly.IsResupplyUnit) continue;
                magnitude = (position - friendly.CurrentPosition).magnitude;
                if (num < 0f || magnitude < num)
                {
                    num = magnitude;
                }
            }
            return magnitude;
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

        public static void ModifyAmmoCount(this Weapon weapon, int value)
        {
            weapon.StatCollection.ModifyStat("resupplyAMMO", -1, "InternalAmmo", StatCollection.StatOperation.Int_Add, value);
        }

        public static void ModifyAmmoCount(this AmmunitionBox box, int value)
        {
            box.StatCollection.ModifyStat("resupplyAMMO", -1, "CurrentAmmo", StatCollection.StatOperation.Int_Add, value);
            box.tCurrentAmmo(box.CurrentAmmo);
        }

        public static void ModifyMechArmorValue(this Mech mech, ArmorLocation loc, float value)
        {
            mech.StatCollection.ModifyStat("resupplyARMOR", -1, mech.GetStringForArmorLocation(loc), StatCollection.StatOperation.Float_Add, value);
        }

        public static int ProcessResupplyUnit(this AbstractActor actor, AbstractActor resupplyActor)
        {
            var searchForSpammy = !string.IsNullOrEmpty(ModInit.modSettings.ResupplyConfig.SPAMMYAmmoDefId);

            var totalAmmoTonnage = 0f;
            var totalArmorPoints = 0f;
            foreach (var weapon in actor.Weapons)
            {
                if (weapon.exDef().InternalAmmo.Count > 0)
                {
                    var startingCapacity = weapon.exDef().InternalAmmo.First().Value;
                    if (weapon.InternalAmmo < startingCapacity)
                    {
                        //InternalAmmoTonnage internalTonnage;
                        if (weapon.weaponDef.Is<InternalAmmoTonnage>(out var internalTonnage))
                        {
                            var missingAmmoForMagicBox = startingCapacity - weapon.InternalAmmo;
                            ModInit.modLog?.Trace?.Write(
                                $"[ProcessResupplyUnit - Internal SPAMMY] - Found weapon {weapon.Description.UIName} needs {missingAmmoForMagicBox} shots added (starting capacity was {startingCapacity}.");
                            var sourceTonnagePerShot =
                                internalTonnage.InternalAmmoTons / startingCapacity;
                            var sourceTonnageNeeded = missingAmmoForMagicBox * sourceTonnagePerShot;
                            totalAmmoTonnage += sourceTonnageNeeded;
                            foreach (var magicBox in resupplyActor.ammoBoxes)
                            {
                                if (weapon.InternalAmmo >= startingCapacity) break;
                                if (magicBox.CurrentAmmo <= 0) continue;

                                var spammy = searchForSpammy && magicBox.ammoDef.Description.Id ==
                                             ModInit.modSettings.ResupplyConfig.SPAMMYAmmoDefId &&
                                             !ModInit.modSettings.ResupplyConfig.SPAMMYBlackList.Contains(weapon
                                                 .weaponDef
                                                 .Description.Id);

                                var intSpammy = searchForSpammy && magicBox.ammoDef.Description.Id ==
                                             ModInit.modSettings.ResupplyConfig.InternalSPAMMYDefId &&
                                             !ModInit.modSettings.ResupplyConfig.InternalSPAMMYBlackList.Contains(weapon
                                                 .weaponDef
                                                 .Description.Id);

                                if (spammy || intSpammy)
                                {
                                    var magicTonnagePerShot = magicBox.tonnage / magicBox.AmmoCapacity;
                                    var magicTonnageAvailable = magicTonnagePerShot * magicBox.CurrentAmmo;
                                    var replacementSourceShotsAvailable =
                                        Mathf.FloorToInt(magicTonnageAvailable / sourceTonnagePerShot);
                                    var magicShotsPerSource = sourceTonnagePerShot / magicTonnagePerShot;
                                    var totalMagicShotsConsumed = Mathf.FloorToInt(Mathf.Min(
                                        magicShotsPerSource * replacementSourceShotsAvailable,
                                        magicShotsPerSource * missingAmmoForMagicBox));
                                    if (replacementSourceShotsAvailable >= missingAmmoForMagicBox)
                                    {
                                        weapon.ModifyAmmoCount(missingAmmoForMagicBox);
                                        weapon.DecInternalAmmo(-1,-missingAmmoForMagicBox);
                                        weapon.tInternalAmmo(weapon.InternalAmmo);
                                        magicBox.ModifyAmmoCount(-totalMagicShotsConsumed);
                                        ModInit.modLog?.Trace?.Write(
                                            $"[ProcessResupplyUnit - Internal Ammo!] - Full Resupply complete for weapon {weapon.Description.UIName}. Ammobox now has {weapon.InternalAmmo}/{weapon.weaponDef.StartingAmmoCapacity} {startingCapacity} ammo, SPAMMY box has {magicBox.CurrentAmmo}/{magicBox.AmmoCapacity} remaining. \n\nMathDumps: Replacement of ammo needs to use {sourceTonnageNeeded} tons of SPAMMY. SPAMMY has {magicTonnageAvailable} tons available at {magicTonnagePerShot} per shot for total SPAMMY shots needed of {totalMagicShotsConsumed}");
                                        break;
                                    }
                                    else
                                    {
                                        weapon.ModifyAmmoCount(replacementSourceShotsAvailable);
                                        weapon.DecInternalAmmo(-1, -replacementSourceShotsAvailable);
                                        weapon.tInternalAmmo(weapon.InternalAmmo);
                                        magicBox.ZeroAmmoCount();
                                        ModInit.modLog?.Trace?.Write(
                                            $"[ProcessResupplyUnit - Internal Ammo!] - Partial Resupply complete for weapon {weapon.Description.UIName}. Ammobox now has {weapon.InternalAmmo}/{weapon.weaponDef.StartingAmmoCapacity} {startingCapacity} ammo, SPAMMY box has {magicBox.CurrentAmmo}/{magicBox.AmmoCapacity} remaining. \n\nMathDumps: Replacement of ammo needs to use {sourceTonnageNeeded} tons of SPAMMY. SPAMMY has {magicTonnageAvailable} tons available at {magicTonnagePerShot} per shot for total SPAMMY shots needed of {totalMagicShotsConsumed}");

                                        //update missing ammo amounts here
                                        missingAmmoForMagicBox = startingCapacity - weapon.InternalAmmo;
                                        sourceTonnageNeeded = missingAmmoForMagicBox * sourceTonnagePerShot;
                                        ModInit.modLog?.Trace?.Write(
                                            $"[ProcessResupplyUnit - Internal Ammo!] - Updating ammo needed after partial resupply. missingAmmoForMagicBox: {missingAmmoForMagicBox}, sourceTonnageNeeded: {sourceTonnageNeeded}");
                                    }
                                }
                            }
                        }
                    }
                }
            }

            foreach (var ammoBoxToFill in actor.ammoBoxes)
            {
                if (ammoBoxToFill.CurrentAmmo < ammoBoxToFill.AmmoCapacity)
                {
                    var initialMissingAmmoCount = ammoBoxToFill.AmmoCapacity - ammoBoxToFill.CurrentAmmo;
                    ModInit.modLog?.Trace?.Write($"[ProcessResupplyUnit - Ammo] - Found box {ammoBoxToFill.Description.UIName} needs {initialMissingAmmoCount} shots added.");
                    var missingAmmoTonnage = initialMissingAmmoCount * (ammoBoxToFill.tonnage / ammoBoxToFill.AmmoCapacity);
                    totalAmmoTonnage += missingAmmoTonnage;
                    var magicBoxes = new List<AmmunitionBox>();
                    foreach (var resupplyBox in resupplyActor.ammoBoxes)
                    {
                        if (ammoBoxToFill.CurrentAmmo >= ammoBoxToFill.AmmoCapacity) break;
                        if (resupplyBox.ammoDef.Description.Id == ammoBoxToFill.ammoDef.Description.Id)
                        {
                            if (resupplyBox.CurrentAmmo <= 0) continue;
                            var currentMissingAmmoCount =
                                ammoBoxToFill.AmmoCapacity - ammoBoxToFill.CurrentAmmo;
                            ModInit.modLog?.Trace?.Write($"[ProcessResupplyUnit - Regular Ammo] - Found resupply box, has {resupplyBox.CurrentAmmo} available.");
                            if (resupplyBox.CurrentAmmo >= currentMissingAmmoCount)
                            {
                                ammoBoxToFill.ModifyAmmoCount(currentMissingAmmoCount);
                                resupplyBox.ModifyAmmoCount(-currentMissingAmmoCount);
                                ModInit.modLog?.Trace?.Write($"[ProcessResupplyUnit - Regular Ammo] - Full Resupply complete for ammoBox {ammoBoxToFill.Description.UIName}. Ammobox now has {ammoBoxToFill.CurrentAmmo}/{ammoBoxToFill.AmmoCapacity} ammo, resupply box has {resupplyBox.CurrentAmmo}/{resupplyBox.AmmoCapacity} remaining");
                                break;
                            }
                            else
                            {
                                ammoBoxToFill.ModifyAmmoCount(resupplyBox.CurrentAmmo);
                                resupplyBox.ModifyAmmoCount(-resupplyBox.CurrentAmmo);
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
                            if (ammoBoxToFill.CurrentAmmo >= ammoBoxToFill.AmmoCapacity) break;
                            if (magicBox.CurrentAmmo <= 0) continue;
                            var magicTonnagePerShot = magicBox.tonnage / magicBox.AmmoCapacity;
                            var magicTonnageAvailable = magicTonnagePerShot * magicBox.CurrentAmmo;
                            var replacementSourceShotsAvailable = Mathf.FloorToInt(magicTonnageAvailable / sourceTonnagePerShot);
                            var magicShotsPerSource = sourceTonnagePerShot/magicTonnagePerShot;
                            var totalMagicShotsConsumed = Mathf.FloorToInt(Mathf.Min(magicShotsPerSource * replacementSourceShotsAvailable, magicShotsPerSource * missingAmmoForMagicBox));
                            if (replacementSourceShotsAvailable >= missingAmmoForMagicBox)
                            {
                                ammoBoxToFill.ModifyAmmoCount(missingAmmoForMagicBox);
                                magicBox.ModifyAmmoCount(-totalMagicShotsConsumed);
                                ModInit.modLog?.Trace?.Write(
                                    $"[ProcessResupplyUnit - Use SpAce Magic Modular (by Yang) Ammo!] - Full Resupply complete for ammoBox {ammoBoxToFill.Description.UIName}. Ammobox now has {ammoBoxToFill.CurrentAmmo}/{ammoBoxToFill.AmmoCapacity} ammo, SPAMMY box has {magicBox.CurrentAmmo}/{magicBox.AmmoCapacity} remaining. \n\nMathDumps: Replacement of ammo needs to use {sourceTonnageNeeded} tons of SPAMMY. SPAMMY has {magicTonnageAvailable} tons available at {magicTonnagePerShot} per shot for total SPAMMY shots needed of {totalMagicShotsConsumed}");
                                break;
                            }
                            else
                            {
                                ammoBoxToFill.ModifyAmmoCount(replacementSourceShotsAvailable);
                                magicBox.ZeroAmmoCount();
                                ModInit.modLog?.Trace?.Write(
                                    $"[ProcessResupplyUnit - Use SpAce Magic Modular (by Yang) Ammo!] - Partial Resupply complete for ammoBox {ammoBoxToFill.Description.UIName}. Ammobox now has {ammoBoxToFill.CurrentAmmo}/{ammoBoxToFill.AmmoCapacity} ammo, SPAMMY box has {magicBox.CurrentAmmo}/{magicBox.AmmoCapacity} remaining. \n\nMathDumps: Replacement of ammo needs to use {sourceTonnageNeeded} tons of SPAMMY. SPAMMY has {magicTonnageAvailable} tons available at {magicTonnagePerShot} per shot for total SPAMMY shots needed of {totalMagicShotsConsumed}");
                                missingAmmoForMagicBox = ammoBoxToFill.AmmoCapacity - ammoBoxToFill.CurrentAmmo;
                                sourceTonnageNeeded = missingAmmoForMagicBox * sourceTonnagePerShot;
                                ModInit.modLog?.Trace?.Write(
                                    $"[ProcessResupplyUnit - Use SpAce Magic Modular (by Yang) Ammo!] - Updating ammo needed after partial resupply. missingAmmoForMagicBox: {missingAmmoForMagicBox}, sourceTonnageNeeded: {sourceTonnageNeeded}");
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
                        foreach (var ammobox in resupplyActor.ammoBoxes)
                        {
                            current = mech.ArmorForLocation((int)loc);
                            if (current <= initialCapped)
                            {
                                var missingArmor = initialCapped - current;
                                //totalArmorPoints += missingArmor;
                            
                                if (ammobox.ammunitionBoxDef.AmmoID ==
                                    ModInit.modSettings.ResupplyConfig.ArmorSupplyAmmoDefId)
                                {
                                    if (ammobox.CurrentAmmo <= 0) continue;
                                    ModInit.modLog?.Trace?.Write(
                                        $"[ProcessResupplyUnit - ARMOR] - Location {loc} can replace {missingArmor} points of armor. Armor Ammo has {ammobox.CurrentAmmo} available.");
                                    var armorAmmoNeeded = Mathf.CeilToInt(missingArmor);
                                    if (ammobox.CurrentAmmo >= armorAmmoNeeded)
                                    {
                                        totalArmorPoints += armorAmmoNeeded;
                                        mech.ModifyMechArmorValue(loc, missingArmor);
                                        ammobox.ModifyAmmoCount(-armorAmmoNeeded);
                                        ModInit.modLog?.Trace?.Write($"[ProcessResupplyUnit - ARMOR] - Location {loc} replaced {missingArmor} points of armor, using {armorAmmoNeeded} armorAmmo. {ammobox.CurrentAmmo} remains.");
                                    }
                                    else
                                    {
                                        totalArmorPoints += ammobox.CurrentAmmo;
                                        mech.ModifyMechArmorValue(loc, ammobox.CurrentAmmo);
                                        ammobox.ZeroAmmoCount();
                                        ModInit.modLog?.Trace?.Write($"[ProcessResupplyUnit - ARMOR] - Location {loc} replaced {ammobox.CurrentAmmo} points of armor, using {armorAmmoNeeded} armorAmmo. {ammobox.CurrentAmmo} remains.");
                                    }
                                }
                            }
                        }
                    }
                }
            }

            var phasesFromAmmo = totalAmmoTonnage * ModInit.modSettings.ResupplyConfig.ResupplyPhasesPerAmmoTonnage;
            var phasesFromArmor = totalArmorPoints * ModInit.modSettings.ResupplyConfig.ResupplyPhasesPerArmorPoint;
            var multiFromTags = 1f;
            foreach (var tag in actor.GetStaticUnitTags())
            {
                if (ModInit.modSettings.ResupplyConfig.UnitTagFactor.ContainsKey(tag))
                {
                    multiFromTags *= ModInit.modSettings.ResupplyConfig.UnitTagFactor[tag];
                }
            }
            var finalPhases = Mathf.RoundToInt((ModInit.modSettings.ResupplyConfig.BasePhasesToResupply + phasesFromAmmo + phasesFromArmor) * multiFromTags);
            ModInit.modLog?.Trace?.Write($"[ProcessResupplyUnit] - Calculated resupply should take {finalPhases} phases: {ModInit.modSettings.ResupplyConfig.BasePhasesToResupply} from baseline, {phasesFromAmmo} from ammo, {phasesFromArmor} from armor, x {multiFromTags} total from tags.");
            return finalPhases;
        }

        public static void UpdateResupplyAbilitiesGetAllLivingActors(this CombatGameState combat)
        {
            var actors = combat.GetAllLivingActors();
            foreach (var unit in actors)
            {
                if (!ModState.TeamsWithResupply.Contains(unit.team.GUID)) continue;
                if (unit.IsResupplyUnit) continue;
                if (unit.GetPilot().Abilities
                        .All(x => x.Def.Id != ModInit.modSettings.ResupplyConfig.ResupplyAbilityID) &&
                    unit.ComponentAbilities.All(y =>
                        y.Def.Id != ModInit.modSettings.ResupplyConfig.ResupplyAbilityID))
                {
                    unit.Combat.DataManager.AbilityDefs.TryGet(ModInit.modSettings.ResupplyConfig.ResupplyAbilityID,
                        out var def);
                    var ability = new Ability(def);
                    ModInit.modLog?.Trace?.Write(
                        $"[UpdateResupplyAbilitiesGetAllLivingActors] Adding {ability.Def?.Description?.Id} to {unit.DisplayName}.");
                    ability.Init(unit.Combat);
                    unit.GetPilot().Abilities.Add(ability);
                    unit.GetPilot().ActiveAbilities.Add(ability);
                }
            }
        }

        public static void UpdateResupplyTeams(this CombatGameState combat)
        {
            foreach (var actor in combat.GetAllLivingActors())
            {
                if (actor.IsResupplyUnit)
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

        public static void ZeroAmmoCount(this AmmunitionBox box)
        {
            box.StatCollection.ModifyStat("resupplyAMMO", -1, "CurrentAmmo", StatCollection.StatOperation.Set, 0);
        }
    }
}
