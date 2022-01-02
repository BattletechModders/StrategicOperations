using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Abilifier;
using Abilifier.Patches;
using BattleTech;
using BattleTech.UI;
using CustAmmoCategories;
using CustomActivatableEquipment;
using CustomComponents;
using CustomUnits;
using Harmony;
using HBS.Math;
using HBS.Pooling;
using Localize;
using StrategicOperations.Framework;
using UnityEngine;
using UnityEngine.UI;
using MechStructureRules = BattleTech.MechStructureRules;
using Random = System.Random;
using Text = Localize.Text;

namespace StrategicOperations.Patches
{
    public class BattleArmorPatches
    {

        [HarmonyPatch(typeof(AbstractActor), "InitEffectStats",
            new Type[] {})]
        public static class AbstractActor_InitEffectStats
        {
            public static void Postfix(AbstractActor __instance)
            {
                __instance.StatCollection.AddStatistic<bool>("CanSwarm", false);
                __instance.StatCollection.AddStatistic<int>("InternalBattleArmorSquadCap", 0);
                __instance.StatCollection.AddStatistic<int>("InternalBattleArmorSquads", 0);
                __instance.StatCollection.AddStatistic<bool>("HasBattleArmorMounts", false);
                __instance.StatCollection.AddStatistic<bool>("IsBattleArmorHandsy", false);
                __instance.StatCollection.AddStatistic<bool>("IsUnmountableBattleArmor", false);
                __instance.StatCollection.AddStatistic<bool>("IsUnswarmableBattleArmor", false);
                __instance.StatCollection.AddStatistic<bool>("BattleArmorMount", false);
                __instance.StatCollection.AddStatistic<float>("BattleArmorDeSwarmerSwat", 0.3f);
                __instance.StatCollection.AddStatistic<int>("BattleArmorDeSwarmerRollInitPenalty", 0);
                __instance.StatCollection.AddStatistic<int>("BattleArmorDeSwarmerSwatInitPenalty", 0);
                __instance.StatCollection.AddStatistic<float>("BattleArmorDeSwarmerSwatDamage", 0f);
                __instance.StatCollection.AddStatistic<float>("BattleArmorDeSwarmerRoll", 0.5f);
                //__instance.StatCollection.AddStatistic<float>("SquishumToadsAsplode", 0.0f);
            }
        }

        [HarmonyPatch(typeof(ActivatableComponent), "activateComponent", new Type[] {typeof(MechComponent), typeof(bool), typeof(bool)})]
        public static class ActivatableComponent_activateComponent
        {
            public static void Postfix(ActivatableComponent __instance, MechComponent component, bool autoActivate, bool isInital)
            {
                if (ModInit.modSettings.BPodComponentIDs.Contains(component.defId))
                {
                    ActivatableComponent activatableComponent = component.componentDef.GetComponent<ActivatableComponent>();
                    var enemyActors = Utils.GetAllEnemiesWithinRange(component.parent, activatableComponent.Explosion.Range);
                    foreach (var enemyActor in enemyActors)
                    {
                        if (enemyActor is TrooperSquad trooperSquad)
                        {
                            if (trooperSquad.IsSwarmingUnit() && ModState.PositionLockSwarm[trooperSquad.GUID] == component.parent.GUID)
                            {
                                trooperSquad.DismountBA(component.parent, true);
                            }
                            
                            var baLoc = trooperSquad.GetPossibleHitLocations(component.parent);
                            var podDmg = activatableComponent.Explosion.Damage;
                            //var podDmg = component.parent.StatCollection.GetValue<float>("SquishumToadsAsplode");
                            //var divDmg = podDmg / baLoc.Count;

                            var clusters = BattleArmorUtils.CreateBPodDmgClusters(baLoc, podDmg);

                            for (int i = 0; i < clusters.Count; i++)
                            {
                                ModInit.modLog.LogMessage($"[ActivatableComponent - activateComponent] BA Armor Damage Location {baLoc}: {trooperSquad.GetStringForArmorLocation((ArmorLocation)baLoc[i])} for {clusters[i]}");
                                var hitinfo = new WeaponHitInfo(-1, -1, 0, 0, component.parent.GUID, trooperSquad.GUID, 1, new float[1], new float[1], new float[1], new bool[1], new int[baLoc[i]], new int[1], new AttackImpactQuality[1], new AttackDirection[1], new Vector3[1], new string[1], new int[baLoc[i]]);
                                trooperSquad.TakeWeaponDamage(hitinfo, baLoc[i], trooperSquad.MeleeWeapon, clusters[i], 0, 0, DamageType.ComponentExplosion);

                                var vector = trooperSquad.GameRep.GetHitPosition(baLoc[i]);
                                var message = new FloatieMessage(hitinfo.attackerId, trooperSquad.GUID, $"{(int)Mathf.Max(1f, clusters[i])}", trooperSquad.Combat.Constants.CombatUIConstants.floatieSizeMedium, FloatieMessage.MessageNature.ArmorDamage, vector.x, vector.y, vector.z);
                                trooperSquad.Combat.MessageCenter.PublishMessage(message);
                            }
                        }
                    }
                }
            }
        }

        [HarmonyPatch(typeof(SelectionStateTargetSingleCombatantBase), "ProcessClickedCombatant", new Type[] {typeof(ICombatant)})]
        public static class SelectionStateTargetSingleCombatantBase_ProcessClickedCombatant
        {
            public static void Postfix(SelectionStateTargetSingleCombatantBase __instance, ICombatant combatant)
            {
                if (__instance.FromButton.Ability.Def.Id == ModInit.modSettings.BattleArmorMountAndSwarmID)
                {
                    var cHUD = Traverse.Create(__instance).Property("HUD").GetValue<CombatHUD>();
                    var creator = cHUD.SelectedActor;
                    
                    if (creator is Mech creatorMech && combatant != null && combatant.team.IsEnemy(creator.team))
                    {
                        var chance = creator.Combat.ToHit.GetToHitChance(creator, creatorMech.MeleeWeapon, combatant, creator.CurrentPosition, combatant.CurrentPosition, 1, MeleeAttackType.Charge, false);
                        ModInit.modLog.LogTrace($"[SelectionState.ShowFireButton - Swarm Success calculated as {chance}, storing in state.");
                        ModState.SwarmSuccessChance = chance;
                        var chanceDisplay = (float)Math.Round(chance, 2) * 100;
                        cHUD.AttackModeSelector.FireButton.FireText.SetText($"{chanceDisplay}% - Confirm", Array.Empty<object>());
                    }
                }
            }
        }

        [HarmonyPatch(typeof(SelectionStateAbilityInstant), "OnAddToStack", new Type[] {})]
        public static class SelectionStateAbilityInstant_OnAddToStack
        {
            public static void Postfix(SelectionStateAbilityInstant __instance)
            {
                var cHUD = Traverse.Create(__instance).Property("HUD").GetValue<CombatHUD>();
                var creator = cHUD.SelectedActor;
                if (__instance.FromButton.Ability.Def.Id == ModInit.modSettings.BattleArmorDeSwarmRoll)
                {
                    var parsed = float.TryParse(__instance.FromButton.Ability.Def.EffectData
                        .FirstOrDefault(x => x.statisticData.statName == "BattleArmorDeSwarmerRoll")
                        ?.statisticData
                        .modValue, out var baseChance);
                    if (!parsed) baseChance = 0.55f;

                    var pilotSkill = creator.GetPilot().Piloting;
                    var finalChance = Mathf.Min(baseChance + (0.05f * pilotSkill), 0.95f);
                    ModInit.modLog.LogMessage($"[SelectionStateAbilityInstant.OnAddToStack - BattleArmorDeSwarm] Deswarm chance: {finalChance} from baseChance {baseChance} + pilotSkill x 0.05 {0.05f * pilotSkill}, max 0.95., stored in state.");
                    ModState.DeSwarmSuccessChance = finalChance;
                    var chanceDisplay = (float)Math.Round(finalChance, 3) * 100;
                    cHUD.AttackModeSelector.FireButton.FireText.SetText($"{chanceDisplay}% - Confirm", Array.Empty<object>());
                }
                else if (__instance.FromButton.Ability.Def.Id == ModInit.modSettings.BattleArmorDeSwarmSwat)
                {
                    
                    var parsed = float.TryParse(__instance.FromButton.Ability.Def.EffectData
                        .FirstOrDefault(x => x.statisticData.statName == "BattleArmorDeSwarmerSwat")
                        ?.statisticData
                        .modValue, out var baseChance);
                    if (!parsed) baseChance = 0.55f;

                    var pilotSkill = creator.GetPilot().Piloting;
                    var missingActuatorCount = -8;
                    foreach (var armComponent in creator.allComponents.Where(x =>
                                 x.IsFunctional && (x.Location == 2 || x.Location == 32)))
                    {
                        foreach (var CategoryID in ModInit.modSettings.ArmActuatorCategoryIDs)
                        {
                            if (armComponent.mechComponentRef.IsCategory(CategoryID))
                            {
                                missingActuatorCount += 1;
                                break;
                            }
                        }
                    }

                    var finalChance = baseChance + (0.05f * pilotSkill) - (0.05f * missingActuatorCount);
                    ModInit.modLog.LogMessage($"[SelectionStateAbilityInstant.OnAddToStack - BattleArmorDeSwarm] Deswarm chance: {finalChance} from baseChance {baseChance} + pilotSkill x 0.05 {0.05f * pilotSkill} - missingActuators x 0.05 {0.05f * missingActuatorCount}, stored in state.");
                    ModState.DeSwarmSuccessChance = finalChance;
                    var chanceDisplay = (float)Math.Round(finalChance, 3) * 100;
                    cHUD.AttackModeSelector.FireButton.FireText.SetText($"{chanceDisplay}% - Confirm", Array.Empty<object>());
                }

            }
        }

        [HarmonyPatch(typeof(AbilityExtensions.SelectionStateMWTargetSingle), "CanTargetCombatant",
            new Type[] {typeof(ICombatant)})]
        public static class SelectionStateMWTargetSingle_CanTargetCombatant
        {
            public static bool Prefix(AbilityExtensions.SelectionStateMWTargetSingle __instance, ICombatant potentialTarget, ref bool __result)
            {
                if (potentialTarget is AbstractActor targetActor)
                {
                    if (__instance.FromButton.Ability.Def.Id == ModInit.modSettings.BattleArmorMountAndSwarmID && (__instance.SelectedActor == targetActor || targetActor is TrooperSquad))
                    {
                        __result = false;
                        return false;
                    }

                    if (__instance.SelectedActor.IsMountedUnit() && targetActor.HasMountedUnits())
                    {
                        if (ModState.PositionLockMount[__instance.SelectedActor.GUID] == targetActor.GUID)
                        {
                            __result = true;
                            return false;
                        }
                        __result = false;
                        return false;
                    }

                    if (__instance.SelectedActor.IsMountedUnit() && !targetActor.HasMountedUnits())
                    {
                        __result = false;
                        return false;
                    }

                    if (!__instance.SelectedActor.IsMountedUnit() && targetActor.HasMountedUnits())
                    {
                        if (targetActor.getAvailableInternalBASpace() > 0)
                        {
                            __result = true;
                            return false;
                        }
                        // figure out carrying capacity here and set true
                        __result = false;
                        return false;
                    }

                    if (__instance.SelectedActor.IsSwarmingUnit() && targetActor.HasSwarmingUnits())
                    {
                        if (ModState.PositionLockSwarm[__instance.SelectedActor.GUID] == targetActor.GUID)
                        {
                            __result = true;
                            return false;
                        }
                        __result = false;
                        return false;
                    }

                    if (__instance.SelectedActor.IsSwarmingUnit() && !targetActor.HasSwarmingUnits())
                    {
                        __result = false;
                        return false;
                    }

                    if (!__instance.SelectedActor.IsSwarmingUnit() && targetActor.HasSwarmingUnits())
                    {
                        __result = true;
                        return false;
                    }

                    if (potentialTarget.team.IsFriendly(__instance.SelectedActor.team))
                    {
                        if (!__instance.SelectedActor.getIsBattleArmorHandsy() && !targetActor.getHasBattleArmorMounts() && targetActor.getAvailableInternalBASpace() <= 0)
                        {
                            __result = false;
                            return false;
                        }

                        if (targetActor.getIsUnMountable())
                        {
                            __result = false;
                            return false;
                        }
                    }

                    if (potentialTarget.team.IsEnemy(__instance.SelectedActor.team))
                    {
                        if (targetActor.getIsUnSwarmable())
                        {
                            __result = false;
                            return false;
                        }
                    }
                }
                __result = true;
                return true;
            }
        }

        [HarmonyPatch(typeof(Ability), "Activate",
            new Type[] {typeof(AbstractActor), typeof(ICombatant)})]
        public static class Ability_Activate
        {
            public static void Postfix(Ability __instance, AbstractActor creator, ICombatant target)
            {
                if (creator == null) return;
                if (UnityGameInstance.BattleTechGame.Combat.ActiveContract.ContractTypeValue.IsSkirmish) return;

                if (__instance.IsAvailable)
                {
                    if (target is AbstractActor targetActor)
                    {
                        if (creator.HasSwarmingUnits() && creator.GUID == targetActor.GUID)
                        {
                            var swarmingUnits = ModState.PositionLockSwarm.Where(x => x.Value == creator.GUID).ToList();

                            if (__instance.Def.Id == ModInit.modSettings.BattleArmorDeSwarmRoll)
                            {
                                var finalChance = 0f;
                                var rollInitPenalty = creator.StatCollection.GetValue<int>("BattleArmorDeSwarmerRollInitPenalty");
                                if (!creator.team.IsLocalPlayer)
                                {
                                    var baseChance = creator.StatCollection.GetValue<float>("BattleArmorDeSwarmerRoll");//0.5f;
                                    var pilotSkill = creator.GetPilot().Piloting;
                                    finalChance = Mathf.Min(baseChance + (0.05f * pilotSkill), 0.95f);
                                    ModInit.modLog.LogMessage($"[Ability.Activate - BattleArmorDeSwarm] Deswarm chance: {finalChance} from baseChance {baseChance} + pilotSkill x 0.05 {0.05f * pilotSkill}, max 0.95.");
                                }
                                else
                                {
                                    finalChance = ModState.DeSwarmSuccessChance;
                                    ModInit.modLog.LogMessage($"[Ability.Activate - BattleArmorDeSwarm] restored deswarm roll chance from state: {ModState.DeSwarmSuccessChance}");
                                }
                                var roll = ModInit.Random.NextDouble();
                                foreach (var swarmingUnit in swarmingUnits)
                                {
                                    var swarmingUnitActor = __instance.Combat.FindActorByGUID(swarmingUnit.Key);
                                    var swarmingUnitSquad = swarmingUnitActor as TrooperSquad;
                                    if (roll <= finalChance)
                                    {
                                        ModInit.modLog.LogMessage(
                                            $"[Ability.Activate - BattleArmorDeSwarm] Deswarm SUCCESS: {roll} <= {finalChance}.");
                                        var txt = new Text("Remove Swarming Battle Armor: SUCCESS");
                                        creator.Combat.MessageCenter.PublishMessage(new AddSequenceToStackMessage(
                                            new ShowActorInfoSequence(creator, txt, FloatieMessage.MessageNature.Buff,
                                                false)));
                                        for (int i = 0; i < rollInitPenalty; i++)
                                        {
                                            swarmingUnitActor.ForceUnitOnePhaseDown(creator.GUID, -1, false);
                                        }
                                        var destroyBARoll = ModInit.Random.NextDouble();
                                        if (destroyBARoll <= .3f)
                                        {
                                            ModInit.modLog.LogMessage(
                                                $"[Ability.Activate - DestroyBA on Roll] SUCCESS: {destroyBARoll} <= {finalChance}.");
                                            var trooperLocs = swarmingUnitActor.GetPossibleHitLocations(creator);
                                            for (int i = 0; i < trooperLocs.Count; i++)
                                            {
                                                var cLoc = (ChassisLocations)trooperLocs[i];
                                                var hitinfo = new WeaponHitInfo(-1, -1, 0, 0, creator.GUID, swarmingUnitActor.GUID, 1, new float[1], new float[1], new float[1], new bool[1], new int[trooperLocs[i]], new int[1], new AttackImpactQuality[1], new AttackDirection[1], new Vector3[1], new string[1], new int[trooperLocs[i]]);
                                                swarmingUnitSquad?.NukeStructureLocation(hitinfo, trooperLocs[i], cLoc, Vector3.up, DamageType.ComponentExplosion);
                                            }
                                        }
                                        else
                                        {
                                            ModInit.modLog.LogMessage(
                                                $"[Ability.Activate - DestroyBA on Roll] FAILURE: {destroyBARoll} > {finalChance}.");
                                            swarmingUnitActor.DismountBA(creator, true);
                                        }
                                    }
                                    else
                                    {
                                        var txt = new Text("Remove Swarming Battle Armor: FAILURE");
                                        creator.Combat.MessageCenter.PublishMessage(new AddSequenceToStackMessage(
                                            new ShowActorInfoSequence(creator, txt, FloatieMessage.MessageNature.Buff,
                                                false)));
                                    ModInit.modLog.LogMessage(
                                            $"[Ability.Activate - BattleArmorDeSwarm] Deswarm FAILURE: {roll} > {finalChance}.");
                                    }
                                }
                            }

                            else if (__instance.Def.Id == ModInit.modSettings.BattleArmorDeSwarmSwat)
                            {
                                var finalChance = 0f;
                                var swatInitPenalty =
                                    creator.StatCollection.GetValue<int>("BattleArmorDeSwarmerSwatInitPenalty");
                                if (!creator.team.IsLocalPlayer)
                                {
                                    var baseChance =
                                        creator.StatCollection.GetValue<float>(
                                            "BattleArmorDeSwarmerSwat"); //0.5f;//0.3f;
                                    var pilotSkill = creator.GetPilot().Piloting;
                                    var missingActuatorCount = -8;
                                    foreach (var armComponent in creator.allComponents.Where(x =>
                                                 x.IsFunctional && (x.Location == 2 || x.Location == 32)))
                                    {
                                        foreach (var CategoryID in ModInit.modSettings.ArmActuatorCategoryIDs)
                                        {
                                            if (armComponent.mechComponentRef.IsCategory(CategoryID))
                                            {
                                                missingActuatorCount += 1;
                                                break;
                                            }
                                        }
                                    }

                                    finalChance = baseChance + (0.05f * pilotSkill) - (0.05f * missingActuatorCount);
                                    ModInit.modLog.LogMessage(
                                        $"[Ability.Activate - BattleArmorDeSwarm] Deswarm chance: {finalChance} from baseChance {baseChance} + pilotSkill x 0.05 {0.05f * pilotSkill} - missingActuators x 0.05 {0.05f * missingActuatorCount}.");
                                }
                                else
                                {
                                    finalChance = ModState.DeSwarmSuccessChance;
                                    ModInit.modLog.LogMessage($"[Ability.Activate - BattleArmorDeSwarm] restored deswarm swat chance from state: {ModState.DeSwarmSuccessChance}");
                                }
                                var roll = ModInit.Random.NextDouble();
                                foreach (var swarmingUnit in swarmingUnits)
                                {
                                    var swarmingUnitActor = __instance.Combat.FindActorByGUID(swarmingUnit.Key);
                                    if (roll <= finalChance)
                                    {
                                        var txt = new Text("Remove Swarming Battle Armor: SUCCESS");
                                        creator.Combat.MessageCenter.PublishMessage(new AddSequenceToStackMessage(
                                            new ShowActorInfoSequence(creator, txt, FloatieMessage.MessageNature.Buff,
                                                false))); 
                                        ModInit.modLog.LogMessage($"[Ability.Activate - BattleArmorDeSwarm] Deswarm SUCCESS: {roll} <= {finalChance}.");
                                        for (int i = 0; i < swatInitPenalty; i++)
                                        {
                                            swarmingUnitActor.ForceUnitOnePhaseDown(creator.GUID, -1, false);
                                        }
                                        var dmgRoll = ModInit.Random.NextDouble(); 
                                        if (dmgRoll <= finalChance)
                                        {
                                            if (swarmingUnitActor is TrooperSquad swarmingUnitAsSquad)
                                            {
                                                var baLoc = swarmingUnitAsSquad.GetPossibleHitLocations(creator).GetRandomElement();
                                                ModInit.modLog.LogMessage($"[Ability.Activate - BattleArmorDeSwarm] BA Armor Damage Location {baLoc}: {swarmingUnitAsSquad.GetStringForArmorLocation((ArmorLocation)baLoc)}");
                                                var swatDmg = creator.StatCollection.GetValue<float>("BattleArmorDeSwarmerSwatDamage");
                                                var hitinfo = new WeaponHitInfo(-1, -1, 0, 0, creator.GUID, swarmingUnitActor.GUID, 1, new float[1], new float[1], new float[1], new bool[1], new int[baLoc], new int[1], new AttackImpactQuality[1], new AttackDirection[1], new Vector3[1], new string[1], new int[baLoc]);

                                                swarmingUnitActor.TakeWeaponDamage(hitinfo, baLoc, swarmingUnitAsSquad.MeleeWeapon, swatDmg, 0, 0, DamageType.Melee);
                                            }
                                        }
                                        swarmingUnitActor.DismountBA(creator, true);
                                    }
                                    else
                                    {
                                        var txt = new Text("Remove Swarming Battle Armor: FAILURE");
                                        creator.Combat.MessageCenter.PublishMessage(new AddSequenceToStackMessage(
                                            new ShowActorInfoSequence(creator, txt, FloatieMessage.MessageNature.Buff,
                                                false)));
                                        ModInit.modLog.LogMessage(
                                            $"[Ability.Activate - BattleArmorDeSwarm] Deswarm FAILURE: {roll} > {finalChance}. Doing nothing and ending turn!");
                                    }
                                }
                            }

                            if (creator is Mech mech)
                            {
                                mech.GenerateAndPublishHeatSequence(-1, true, false, mech.GUID);
                            }
                            if (__instance.Def.Id == ModInit.modSettings.BattleArmorDeSwarmRoll)
                            {
                                creator.FlagForKnockdown();
                                creator.HandleKnockdown(-1,creator.GUID,Vector2.one, null);
                            }
                            if (creator.team.IsLocalPlayer)
                            {
                                var sequence = creator.DoneWithActor();
                                creator.Combat.MessageCenter.PublishMessage(new AddSequenceToStackMessage(sequence));
                                creator.OnActivationEnd(creator.GUID, -1);
                            }
                            return;
                        }

                        if (!creator.IsSwarmingUnit() && !creator.IsMountedUnit())
                        {
                            if (__instance.Def.Id == ModInit.modSettings.BattleArmorMountAndSwarmID && target.team.IsFriendly(creator.team))
                            {
                                foreach (var effectData in ModState.BAUnhittableEffect.effects)
                                {
                                    creator.Combat.EffectManager.CreateEffect(effectData, ModState.BAUnhittableEffect.ID,
                                        -1, creator, creator, default(WeaponHitInfo), 1);
                                }
                                targetActor.MountBattleArmorToChassis(creator);
                                //creator.GameRep.IsTargetable = false;
                                creator.TeleportActor(target.CurrentPosition);

                                //creator.GameRep.enabled = false;
                                //creator.GameRep.gameObject.SetActive(false);
                                //creator.GameRep.gameObject.Despawn();
                                //UnityEngine.Object.Destroy(creator.GameRep.gameObject);

                                //CombatMovementReticle.Instance.RefreshActor(creator); // or just end activation completely? definitely on use.
                                
                                ModState.PositionLockMount.Add(creator.GUID, target.GUID);
                                ModInit.modLog.LogMessage(
                                    $"[Ability.Activate - BattleArmorMountID] Added PositionLockMount with rider  {creator.DisplayName} {creator.GUID} and carrier {target.DisplayName} {target.GUID}.");

                                if (creator.team.IsLocalPlayer)
                                {
                                    var sequence = creator.DoneWithActor();
                                    creator.Combat.MessageCenter.PublishMessage(new AddSequenceToStackMessage(sequence));
                                    creator.OnActivationEnd(creator.GUID, -1);
                                }
                            }
                            else if (__instance.Def.Id == ModInit.modSettings.BattleArmorMountAndSwarmID && target.team.IsEnemy(creator.team) && creator is Mech creatorMech && creatorMech.canSwarm())
                            {
                                targetActor.CheckForBPodAndActivate();
                                if (creator.IsFlaggedForDeath)
                                {
                                    creator.HandleDeath(targetActor.GUID);
                                    return;
                                }
                                var meleeChance = creator.team.IsLocalPlayer ? ModState.SwarmSuccessChance : creator.Combat.ToHit.GetToHitChance(creator, creatorMech.MeleeWeapon, target, creator.CurrentPosition, target.CurrentPosition, 1, MeleeAttackType.Charge, false);
                                
                                var roll = ModInit.Random.NextDouble();
                                ModInit.modLog.LogMessage($"[Ability.Activate - BattleArmorSwarmID] Rolling simplified melee: roll {roll} vs hitChance {meleeChance}; chance in Modstate was {ModState.SwarmSuccessChance}.");
                                if (roll <= meleeChance)
                                {
                                    foreach (var effectData in ModState.BAUnhittableEffect.effects)
                                    {
                                        creator.Combat.EffectManager.CreateEffect(effectData, ModState.BAUnhittableEffect.ID,
                                            -1, creator, creator, default(WeaponHitInfo), 1);
                                    }

                                    var txt = new Text("Swarm Attack: SUCCESS");
                                    creator.Combat.MessageCenter.PublishMessage(new AddSequenceToStackMessage(
                                        new ShowActorInfoSequence(creator, txt, FloatieMessage.MessageNature.Buff,
                                            false)));

                                    ModInit.modLog.LogMessage(
                                        $"[Ability.Activate - BattleArmorSwarmID] Cleaning up dummy attacksequence.");
                                    targetActor.MountBattleArmorToChassis(creator);
                                    //creator.GameRep.IsTargetable = false;
                                    creator.TeleportActor(target.CurrentPosition);

                                    //creator.GameRep.enabled = false;
                                    //creator.GameRep.gameObject.SetActive(false); //this might be the problem with attacking.
                                    //creator.GameRep.gameObject.Despawn();
                                    //UnityEngine.Object.Destroy(creator.GameRep.gameObject);
                                    //CombatMovementReticle.Instance.RefreshActor(creator);

                                    ModState.PositionLockSwarm.Add(creator.GUID, target.GUID);
                                    ModInit.modLog.LogMessage(
                                        $"[Ability.Activate - BattleArmorSwarmID] Added PositionLockSwarm with rider  {creator.DisplayName} {creator.GUID} and carrier {target.DisplayName} {target.GUID}.");
                                    creator.ResetPathing(false);
                                    creator.Pathing.UpdateCurrentPath(false);

                                    if (ModInit.modSettings.AttackOnSwarmSuccess)
                                    {
                                        var weps = creator.Weapons.Where(x => x.IsEnabled && x.HasAmmo).ToList();
                                        var loc = ModState.BADamageTrackers[creator.GUID].BA_MountedLocations.Values
                                            .GetRandomElement();
                                        if (true)
                                        {
                                            var attackStackSequence = new AttackStackSequence(creator, target,
                                                creator.CurrentPosition,
                                                creator.CurrentRotation, weps, MeleeAttackType.NotSet, loc, -1);
                                            creator.Combat.MessageCenter.PublishMessage(
                                                new AddSequenceToStackMessage(attackStackSequence));
                                            ModInit.modLog.LogMessage(
                                                $"[Ability.Activate - BattleArmorSwarmID] Creating attack sequence on successful swarm attack targeting location {loc}.");
                                        }

                                        if (false)
                                        {
                                            var attackInvocationMsg = new AttackInvocation(creator, target, weps,
                                                MeleeAttackType.NotSet, loc);

                                            ReceiveMessageCenterMessage subscriber =
                                                delegate(MessageCenterMessage message)
                                                {
                                                    AddSequenceToStackMessage addSequenceToStackMessage =
                                                        message as AddSequenceToStackMessage;
                                                    //creator.Combat..Orders = addSequenceToStackMessage.sequence;
                                                };
                                            creator.Combat.MessageCenter.AddSubscriber(
                                                MessageCenterMessageType.AddSequenceToStackMessage, subscriber);
                                            creator.Combat.MessageCenter.PublishMessage(attackInvocationMsg);
                                            creator.Combat.MessageCenter.RemoveSubscriber(
                                                MessageCenterMessageType.AddSequenceToStackMessage, subscriber);
                                            //creator.Combat.MessageCenter.PublishMessage(attackInvocation);
                                        }
                                    }
                                    if (creator.team.IsLocalPlayer)
                                    {
                                        var sequence = creator.DoneWithActor();
                                        creator.Combat.MessageCenter.PublishMessage(new AddSequenceToStackMessage(sequence));
                                        creator.OnActivationEnd(creator.GUID, -1);
                                    }
                                }
                                else
                                {
                                    var txt = new Text("Swarm Attack: FAILURE");
                                    creator.Combat.MessageCenter.PublishMessage(new AddSequenceToStackMessage(
                                        new ShowActorInfoSequence(creator, txt, FloatieMessage.MessageNature.Buff,
                                            false)));
                                    ModInit.modLog.LogMessage(
                                        $"[Ability.Activate - BattleArmorSwarmID] Cleaning up dummy attacksequence.");
                                    ModInit.modLog.LogMessage(
                                        $"[Ability.Activate - BattleArmorSwarmID] No hits in HitInfo, plonking unit at target hex.");
                                    creator.TeleportActor(target.CurrentPosition);
                                    creator.ResetPathing(false);
                                    creator.Pathing.UpdateCurrentPath(false);
                                    if (creator.team.IsLocalPlayer)
                                    {
                                        var sequence = creator.DoneWithActor();
                                        creator.Combat.MessageCenter.PublishMessage(new AddSequenceToStackMessage(sequence));
                                        creator.OnActivationEnd(creator.GUID, -1);
                                    }
                                }
                            }
                            else if (__instance.Def.Id == ModInit.modSettings.BattleArmorMountAndSwarmID &&
                                     target.team.IsEnemy(creator.team) && creator is Mech creatorMech2 &&
                                     !creatorMech2.canSwarm())
                            {
                                var popup = GenericPopupBuilder.Create(GenericPopupType.Info, $"Unit {creatorMech2.DisplayName} is unable to make swarming attacks!");
                                popup.AddButton("Confirm", null, true, null);
                                popup.IsNestedPopupWithBuiltInFader().CancelOnEscape().Render();
                                return;
                            }
                        }

                        else if (creator.IsSwarmingUnit() || creator.IsMountedUnit())
                        {
                            if (__instance.Def.Id == ModInit.modSettings.BattleArmorMountAndSwarmID)
                            {
                                creator.DismountBA(targetActor);
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
                if (UnityGameInstance.BattleTechGame.Combat.ActiveContract.ContractTypeValue.IsSkirmish) return;
                if (actor == null) return;
                if (actor.IsMountedUnit())
                {
                    ModInit.modLog.LogTrace(
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
                        if (abilityButton?.Ability?.Def?.Id == ModInit.modSettings.BattleArmorMountAndSwarmID)
                            abilityButton?.DisableButton();
                    }
                    return;
                }
                else if (actor.IsSwarmingUnit())
                {
                    ModInit.modLog.LogTrace(
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
                        if (abilityButton?.Ability?.Def?.Id == ModInit.modSettings.BattleArmorMountAndSwarmID)
                            abilityButton?.DisableButton();
                    }
                }
            }
        }

        //patching LOFCache.GetLineOfFire with BA to make sure its not obstructed AND that the carrier isnt obstructed. gonna be messy AF. will also probaly break LowVis.

        

        [HarmonyPatch(typeof(AbstractActor), "HasLOFToTargetUnitAtTargetPosition",
            new Type[] { typeof(ICombatant), typeof(float), typeof(Vector3), typeof(Quaternion), typeof(Vector3), typeof(Quaternion), typeof(bool) })]
        public static class AbstractActor_HasLOFToTargetUnitAtTargetPosition_Patch
        {
            static bool Prepare() => false; //disabled for now
            // make sure units doing swarming or riding cannot be targeted.
            public static void Postfix(AbstractActor __instance, ICombatant targetUnit, float maxRange, Vector3 attackPosition, Quaternion attackRotation, Vector3 targetPosition, Quaternion targetRotation, bool isIndirectFireCapable, ref bool __result)
            {
                if (targetUnit is AbstractActor targetActor)
                {
                    if (targetActor.IsSwarmingUnit() || targetActor.IsMountedUnit())
                    {
//                        ModInit.modLog.LogTrace($"[AbstractActor.HasLOFToTargetUnitAtTargetPosition] {targetActor.DisplayName} is swarming or mounted, preventing LOS.");
                        __result = false;
                    }
                }
            }
        }

        [HarmonyPatch(typeof(AbstractActor), "HasIndirectLOFToTargetUnit",
            new Type[] { typeof(Vector3), typeof(Quaternion), typeof(ICombatant), typeof(bool) })]
        public static class AbstractActor_HasIndirectLOFToTargetUnit_Patch
        {
            public static void Postfix(AbstractActor __instance, Vector3 attackPosition, Quaternion attackRotation, ICombatant targetUnit, bool enabledWeaponsOnly, ref bool __result)
            {
                if (__instance.IsSwarmingUnit() && targetUnit is AbstractActor targetActor)
                {
                    if (ModState.PositionLockSwarm[__instance.GUID] == targetActor.GUID)
                    {
//                        ModInit.modLog.LogTrace($"[AbstractActor.HasIndirectLOFToTargetUnit] {__instance.DisplayName} is swarming {targetActor.DisplayName}, forcing direct LOS for weapons");
                        __result = false;
                    }
                }
            }
        }

        [HarmonyPatch(typeof(Weapon), "WillFireAtTargetFromPosition",
            new Type[] {typeof(ICombatant), typeof(Vector3), typeof(Quaternion)})]
        public static class Weapon_WillFireAtTargetFromPosition
        {
            public static void Postfix(Weapon __instance, ICombatant target, Vector3 position, Quaternion rotation, ref bool __result)
            {
                if (__instance.parent.IsSwarmingUnit() && target is AbstractActor targetActor)
                {
                    if (ModState.PositionLockSwarm[__instance.parent.GUID] == targetActor.GUID)
                    {
 //                       ModInit.modLog.LogTrace($"[Weapon.WillFireAtTargetFromPosition] {__instance.parent.DisplayName} is swarming {targetActor.DisplayName}, forcing LOS for weapon {__instance.Name}");
                        __result = true;
                    }
                }
            }
        }

        [HarmonyPatch(typeof(CombatHUDButtonBase), "OnClick",
            new Type[] { })]
        public static class CombatHUDButtonBase_OnClick
        {
            static bool Prepare() => true;
            public static void Prefix(CombatHUDButtonBase __instance)
            {
                if (__instance.GUID != "BTN_DoneWithMech") return;
                var hud = Traverse.Create(__instance).Property("HUD").GetValue<CombatHUD>();
                var actor = hud.SelectedActor;
                if (!actor.IsSwarmingUnit())
                {
                    ModInit.modLog.LogMessage($"[CombatHUDButtonBase.OnClick] Actor {actor.DisplayName} is not swarming, ending turn like normal.");
                    return;
                }
                var target = actor.Combat.FindActorByGUID(ModState.PositionLockSwarm[actor.GUID]);
                ModInit.modLog.LogMessage($"[CombatHUDButtonBase.OnClick] Actor {actor.DisplayName} has active swarm attack on {target.DisplayName}");

                var weps = actor.Weapons.Where(x => x.IsEnabled && x.HasAmmo).ToList();

                //                var baselineAccuracyModifier = actor.StatCollection.GetValue<float>("AccuracyModifier");
                //                actor.StatCollection.Set<float>("AccuracyModifier", -99999.0f);
                //                ModInit.modLog.LogTrace($"[AbstractActor.DoneWithActor] Actor {actor.DisplayName} getting baselineAccuracyModifer set to {actor.AccuracyModifier}");

                var loc = ModState.BADamageTrackers[actor.GUID].BA_MountedLocations.Values.GetRandomElement();
                var attackStackSequence = new AttackStackSequence(actor, target, actor.CurrentPosition,
                    actor.CurrentRotation, weps, MeleeAttackType.NotSet, loc, -1);
                actor.Combat.MessageCenter.PublishMessage(new AddSequenceToStackMessage(attackStackSequence));

//                actor.StatCollection.Set<float>("AccuracyModifier", baselineAccuracyModifier);
//                ModInit.modLog.LogTrace($"[AbstractActor.DoneWithActor] Actor {actor.DisplayName} resetting baselineAccuracyModifer to {actor.AccuracyModifier}");
                return;
            }
        }


        [HarmonyPatch(typeof(SelectionStateFire), "ProcessClickedCombatant",
            new Type[] {typeof(ICombatant)})]
        public static class SelectionStateFire_ProcessClickedCombatant
        {
            static bool Prepare() => false; //disable for now, try with force-end turn.
            public static void Postfix(SelectionStateFire __instance, ref ICombatant combatant)
            {
                if (__instance.SelectedActor.IsSwarmingUnit())
                {
                    var newTarget =
                        __instance.SelectedActor.Combat.FindActorByGUID(
                            ModState.PositionLockSwarm[__instance.SelectedActor.GUID]);
                    combatant = newTarget;
                }
            }
        }

        [HarmonyPatch(typeof(Mech), "OnLocationDestroyed",
            new Type[] {typeof(ChassisLocations), typeof(Vector3), typeof(WeaponHitInfo), typeof(DamageType)})]
        public static class Mech_OnLocationDestroyed
        {
            public static void Prefix(Mech __instance, ChassisLocations location, Vector3 attackDirection,
                WeaponHitInfo hitInfo, DamageType damageType)
            {
                if (!__instance.HasMountedUnits() && !__instance.HasSwarmingUnits()) return;

                foreach (var squadInfo in ModState.BADamageTrackers.Where(x =>
                    x.Value.TargetGUID == __instance.GUID && !x.Value.IsSquadInternal &&
                    x.Value.BA_MountedLocations.ContainsValue((int)location)))
                {
    
                    var wereSwarmingUnitsResponsible = squadInfo.Key == hitInfo.attackerId;

                    ModInit.modLog.LogTrace(
                        $"[Mech.OnLocationDestroyed] Evaluating {squadInfo.Key} for {squadInfo.Value.TargetGUID}");
                    if (ModInit.Random.NextDouble() >= (double) 1 / 3 || wereSwarmingUnitsResponsible) continue;
                    if (__instance.Combat.FindActorByGUID(squadInfo.Key) is Mech BattleArmorAsMech)
                    {
                        var BattleArmorMounts =
                            squadInfo.Value.BA_MountedLocations.Where(x => x.Value == (int) location);
                        foreach (var mount in BattleArmorMounts)
                        {
                            var BALocArmor = (ArmorLocation) mount.Key;
                            var BALocStruct = MechStructureRules.GetChassisLocationFromArmorLocation(BALocArmor);
                            BattleArmorAsMech.NukeStructureLocation(hitInfo, 1, BALocStruct, attackDirection,
                                damageType);
                        }
                    }
                }
            }
        }


        [HarmonyPatch(typeof(AbstractActor), "HandleDeath",
            new Type[] {typeof(string) })]
        public static class AbstractActor_HandleDeath
        {
            public static void Prefix(AbstractActor __instance, string attackerGUID)
            {
                if (__instance.HasSwarmingUnits())
                {
                    var swarmingUnits = new List<KeyValuePair<string, string>>(ModState.PositionLockSwarm.Where(x => x.Value == __instance.GUID).ToList());
                    var wereSwarmingUnitsResponsible = swarmingUnits.Any(x => x.Key == attackerGUID);
                    foreach (var swarmingUnit in swarmingUnits)
                    {
                        var actor = __instance.Combat.FindActorByGUID(swarmingUnit.Key);
                        var squad = actor as TrooperSquad;
                        if (ModInit.Random.NextDouble() <= (double)1 / 3 && !wereSwarmingUnitsResponsible)
                        {
                            var trooperLocs = squad.GetPossibleHitLocations(__instance);
                            for (int i = 0; i < trooperLocs.Count; i++)
                            {
                                var cLoc = (ChassisLocations)trooperLocs[i];
                                var hitinfo = new WeaponHitInfo(-1, -1, 0, 0, __instance.GUID, squad.GUID, 1, new float[1], new float[1], new float[1], new bool[1], new int[trooperLocs[i]], new int[1], new AttackImpactQuality[1], new AttackDirection[1], new Vector3[1], new string[1], new int[trooperLocs[i]]);
                                squad.NukeStructureLocation(hitinfo, trooperLocs[i], cLoc, Vector3.up, DamageType.ComponentExplosion);
                            }
                            continue;
                        }
                        ModInit.modLog.LogTrace($"[AbstractActor.HandleDeath] Swarmed unit {__instance.DisplayName} destroyed, calling dismount.");
                        actor.DismountBA(__instance, false, true);
                    }
                }

                if (__instance.HasMountedUnits())
                {
                    var mountedUnits = new List<KeyValuePair<string,string>>(ModState.PositionLockMount.Where(x => x.Value == __instance.GUID).ToList());
                    foreach (var mountedUnit in mountedUnits)
                    {
                        var actor = __instance.Combat.FindActorByGUID(mountedUnit.Key);
                        var squad = actor as TrooperSquad;
                        if (ModInit.Random.NextDouble() <= (double)1 / 3)
                        {
                            var trooperLocs = squad.GetPossibleHitLocations(__instance);
                            for (int i = 0; i < trooperLocs.Count; i++)
                            {
                                var cLoc = (ChassisLocations)trooperLocs[i];
                                var hitinfo = new WeaponHitInfo(-1, -1, 0, 0, __instance.GUID, squad.GUID, 1, new float[1], new float[1], new float[1], new bool[1], new int[trooperLocs[i]], new int[1], new AttackImpactQuality[1], new AttackDirection[1], new Vector3[1], new string[1], new int[trooperLocs[i]]);
                                squad.NukeStructureLocation(hitinfo, trooperLocs[i], cLoc, Vector3.up, DamageType.ComponentExplosion);
                            }
                            continue;
                        }
                        ModInit.modLog.LogTrace($"[AbstractActor.HandleDeath] Mount {__instance.DisplayName} destroyed, calling dismount.");
                        actor.DismountBA(__instance, false, true);
                    }
                }
            }
        }

        [HarmonyPatch(typeof(CombatHUDMechwarriorTray), "ResetAbilityButton",
            new Type[] { typeof(AbstractActor), typeof(CombatHUDActionButton), typeof(Ability), typeof(bool) })]
        public static class CombatHUDMechwarriorTray_ResetAbilityButton_Patch
        {
            public static void Postfix(CombatHUDMechwarriorTray __instance, AbstractActor actor, CombatHUDActionButton button, Ability ability, bool forceInactive)
            {
                if (UnityGameInstance.BattleTechGame.Combat.ActiveContract.ContractTypeValue.IsSkirmish) return;
                if (actor == null || ability == null) return;
//                if (button == __instance.FireButton)
//                {
 //                   ModInit.modLog.LogTrace(
 //                       $"Leaving Fire Button Enabled");
 //                   return;
//                }
                if (actor.IsMountedUnit() || actor.IsSwarmingUnit())
                {
                    button.DisableButton();
                }

                if (ability.Def.Id == ModInit.modSettings.BattleArmorDeSwarmRoll ||
                    ability.Def.Id == ModInit.modSettings.BattleArmorDeSwarmSwat)
                {
                    if (actor is Vehicle vehicle || actor.IsCustomUnitVehicle())
                    {
                        button.DisableButton();
                    }

                    if (!actor.HasSwarmingUnits())
                    {
                        button.DisableButton();
                    }
                }
            }
        }

        [HarmonyPatch(typeof(CombatSelectionHandler), "AddSprintState",
            new Type[] {typeof(AbstractActor)})]
        public static class CombatSelectionHandler_AddSprintState
        {
            public static bool Prefix(CombatSelectionHandler __instance, AbstractActor actor)
            {
                if (actor.IsMountedUnit() || actor.IsSwarmingUnit())
                {
                    ModInit.modLog.LogTrace($"[CombatSelectionHandler.AddSprintState] Actor {actor.DisplayName}: Disabling SprintState");
                    var SelectionStack = Traverse.Create(__instance).Property("SelectionStack").GetValue<List<SelectionState>>();
                    if (!SelectionStack.Any(x => x is SelectionStateDoneWithMech))
                    {
                        var HUD = Traverse.Create(__instance).Property("HUD").GetValue<CombatHUD>();
                        var doneState = new SelectionStateDoneWithMech(actor.Combat, HUD,
                            HUD.MechWarriorTray.DoneWithMechButton, actor);
                        var addState = Traverse.Create(__instance)
                            .Method("addNewState", new Type[] {typeof(SelectionState)});
                        addState.GetValue(doneState);
                    }
                    return false;
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(CombatSelectionHandler), "AddMoveState",
            new Type[] { typeof(AbstractActor) })]
        public static class CombatSelectionHandler_AddMoveState
        {
            public static bool Prefix(CombatSelectionHandler __instance, AbstractActor actor)
            {
                if (actor.IsMountedUnit() || actor.IsSwarmingUnit())
                {
                    ModInit.modLog.LogTrace($"[CombatSelectionHandler.AddMoveState] Actor {actor.DisplayName}: Disabling AddMoveState");
                    var SelectionStack = Traverse.Create(__instance).Property("SelectionStack").GetValue<List<SelectionState>>();
                    if (!SelectionStack.Any(x => x is SelectionStateDoneWithMech))
                    {
                        var HUD = Traverse.Create(__instance).Property("HUD").GetValue<CombatHUD>();
                        var doneState = new SelectionStateDoneWithMech(actor.Combat, HUD,
                            HUD.MechWarriorTray.DoneWithMechButton, actor);
                        var addState = Traverse.Create(__instance)
                            .Method("addNewState", new Type[] { typeof(SelectionState) });
                        addState.GetValue(doneState);
                    }
                    return false;
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(Mech), "DamageLocation",
            new Type[] {typeof(int), typeof(WeaponHitInfo), typeof(ArmorLocation), typeof(Weapon), typeof(float), typeof(float), typeof(int), typeof(AttackImpactQuality), typeof(DamageType)})]
        public static class Mech_DamageLocation_Patch
        {
            public static void Prefix(Mech __instance, int originalHitLoc, WeaponHitInfo hitInfo, ArmorLocation aLoc, Weapon weapon, ref float totalArmorDamage, ref float directStructureDamage, int hitIndex, AttackImpactQuality impactQuality, DamageType damageType)
            {
                if (!__instance.HasMountedUnits() && !__instance.HasSwarmingUnits()) return;

                foreach (var squadInfo in ModState.BADamageTrackers.Where(x => x.Value.TargetGUID == __instance.GUID && !x.Value.IsSquadInternal && x.Value.BA_MountedLocations.ContainsValue((int)aLoc)))
                {
                    ModInit.modLog.LogTrace($"[Mech.DamageLocation] Evaluating {squadInfo.Key} for {squadInfo.Value.TargetGUID}");
                    if (ModInit.Random.NextDouble() > (double)1 / 3) continue;
                    if (__instance.Combat.FindActorByGUID(squadInfo.Key) is Mech BattleArmorAsMech)
                    {
                        if (BattleArmorAsMech.GUID == hitInfo.attackerId) return;
                        var BattleArmorMounts = squadInfo.Value.BA_MountedLocations.Where(x => x.Value == (int) aLoc);
                        foreach (var mount in BattleArmorMounts)
                        {
                            var BALocArmor = (ArmorLocation) mount.Key;
                            //var BALocArmorString = BattleArmorAsMech.GetStringForArmorLocation(BALocArmor);
                            var BALocStruct = MechStructureRules.GetChassisLocationFromArmorLocation(BALocArmor);
                            //var BALocStructString = BattleArmorAsMech.GetStringForStructureLocation(BALocStruct);

                            var BattleArmorLocArmor = BattleArmorAsMech.ArmorForLocation((int) BALocArmor);
                            var BattleArmorLocStruct = BattleArmorAsMech.StructureForLocation((int) BALocStruct);

                            if (directStructureDamage > 0)
                            {
                                ModInit.modLog.LogMessage(
                                    $"[Mech.DamageLocation] directStructureDamage: {directStructureDamage}");
                                var directStructureDiff = directStructureDamage - BattleArmorLocStruct;
                                if (directStructureDiff >= 0)
                                {
                                    directStructureDamage -= BattleArmorLocStruct;
                                    ModInit.modLog.LogMessage(
                                        $"[Mech.DamageLocation] directStructureDamage Diff: {directStructureDiff}. Mech directStructureDamage decremented to {directStructureDamage}");
                                    BattleArmorAsMech.TakeWeaponDamage(hitInfo, (int) BALocArmor, weapon, 0,
                                        BattleArmorLocStruct, hitIndex, damageType);
                                    ModInit.modLog.LogMessage(
                                        $"[Mech.DamageLocation] Battle Armor at location {BALocArmor} takes {BattleArmorLocStruct} direct structure damage");
                                    __instance.Combat.MessageCenter.PublishMessage(new AddSequenceToStackMessage(new ShowActorInfoSequence(BattleArmorAsMech, Strings.T("Battle Armor Damaged!"), FloatieMessage.MessageNature.CriticalHit, false)));
                                    continue;
                                }
                                
                                else if (directStructureDiff < 0)
                                {
                                    ModInit.modLog.LogMessage(
                                        $"[Mech.DamageLocation] directStructureDamage Diff: {directStructureDiff}. Mech directStructureDamage decremented to 0");
                                    BattleArmorAsMech.TakeWeaponDamage(hitInfo, (int) BALocArmor, weapon, 0,
                                        Mathf.Abs(directStructureDamage), hitIndex, damageType);
                                    ModInit.modLog.LogMessage(
                                        $"[Mech.DamageLocation] Battle Armor at location {BALocArmor} takes {directStructureDamage} direct structure damage");
                                    __instance.Combat.MessageCenter.PublishMessage(new AddSequenceToStackMessage(new ShowActorInfoSequence(BattleArmorAsMech, Strings.T("Battle Armor Damaged!"), FloatieMessage.MessageNature.CriticalHit, false)));
                                    directStructureDamage = 0;
                                }
                            }

                            if (totalArmorDamage > 0)
                            {
                                ModInit.modLog.LogMessage(
                                    $"[Mech.DamageLocation] totalArmorDamage: {totalArmorDamage}");
                                var totalArmorDamageDiff =
                                    totalArmorDamage - (BattleArmorLocArmor + BattleArmorLocStruct);
                                if (totalArmorDamageDiff > 0)
                                {
                                    totalArmorDamage -= totalArmorDamageDiff;
                                    ModInit.modLog.LogMessage(
                                        $"[Mech.DamageLocation] totalArmorDamageDiff Diff: {totalArmorDamageDiff}. Mech totalArmorDamage decremented to {totalArmorDamage}");
                                    BattleArmorAsMech.TakeWeaponDamage(hitInfo, (int) BALocArmor, weapon,
                                        Mathf.Abs(totalArmorDamageDiff), 0, hitIndex, damageType);
                                    ModInit.modLog.LogMessage(
                                        $"[Mech.DamageLocation] Battle Armor at location {BALocArmor} takes {BattleArmorLocArmor} damage");
                                    __instance.Combat.MessageCenter.PublishMessage(new AddSequenceToStackMessage(new ShowActorInfoSequence(BattleArmorAsMech, Strings.T("Battle Armor Damaged!"), FloatieMessage.MessageNature.CriticalHit, false)));
                                }

                                else if (totalArmorDamageDiff <= 0)
                                {
                                    ModInit.modLog.LogMessage(
                                        $"[Mech.DamageLocation] totalArmorDamageDiff Diff: {totalArmorDamageDiff}. Mech totalArmorDamage decremented to 0");
                                    BattleArmorAsMech.TakeWeaponDamage(hitInfo, (int) BALocArmor, weapon,
                                        Mathf.Abs(totalArmorDamage), 0, hitIndex, damageType);
                                    ModInit.modLog.LogMessage(
                                        $"[Mech.DamageLocation] Battle Armor at location {BALocArmor} takes {totalArmorDamage} damage");
                                    totalArmorDamage = 0;
                                    __instance.Combat.MessageCenter.PublishMessage(new AddSequenceToStackMessage(new ShowActorInfoSequence(BattleArmorAsMech, Strings.T("Battle Armor Damaged!"), FloatieMessage.MessageNature.CriticalHit, false)));
                                }
                            }
                        }
                    }
                }
            }
        }

        [HarmonyPatch(typeof(Vehicle), "DamageLocation",
            new Type[] { typeof(WeaponHitInfo), typeof(int), typeof(VehicleChassisLocations), typeof(Weapon), typeof(float), typeof(float), typeof(AttackImpactQuality) })]
        public static class Vehicle_DamageLocation_Patch
        {
            public static void Prefix(Vehicle __instance, WeaponHitInfo hitInfo, int originalHitLoc, VehicleChassisLocations vLoc, Weapon weapon, ref float totalArmorDamage, ref float directStructureDamage, AttackImpactQuality impactQuality)
            {
                if (!__instance.HasMountedUnits() && !__instance.HasSwarmingUnits()) return;

                foreach (var squadInfo in ModState.BADamageTrackers.Where(x => x.Value.TargetGUID == __instance.GUID && !x.Value.IsSquadInternal && x.Value.BA_MountedLocations.ContainsValue((int)vLoc)))
                {
                    ModInit.modLog.LogTrace($"[Vehicle.DamageLocation] Evaluating {squadInfo.Key} for {squadInfo.Value.TargetGUID}");
                    if (ModInit.Random.NextDouble() > (double)1 / 3) continue;
                    if (__instance.Combat.FindActorByGUID(squadInfo.Key) is Mech BattleArmorAsMech)
                    {
                        if (BattleArmorAsMech.GUID == hitInfo.attackerId) return;
                        var BattleArmorMounts = squadInfo.Value.BA_MountedLocations.Where(x => x.Value == (int)vLoc);
                        foreach (var mount in BattleArmorMounts)
                        {
                            var BALocArmor = (ArmorLocation)mount.Key;
                            //var BALocArmorString = BattleArmorAsMech.GetStringForArmorLocation(BALocArmor);
                            var BALocStruct = MechStructureRules.GetChassisLocationFromArmorLocation(BALocArmor);
                            //var BALocStructString = BattleArmorAsMech.GetStringForStructureLocation(BALocStruct);

                            var BattleArmorLocArmor = BattleArmorAsMech.ArmorForLocation((int) BALocArmor);
                            var BattleArmorLocStruct = BattleArmorAsMech.StructureForLocation((int) BALocStruct);

                            if (directStructureDamage > 0)
                            {
                                ModInit.modLog.LogMessage(
                                    $"[Vehicle.DamageLocation] directStructureDamage: {directStructureDamage}");
                                var directStructureDiff = directStructureDamage - BattleArmorLocStruct;
                                if (directStructureDiff >= 0)
                                {
                                    directStructureDamage -= BattleArmorLocStruct;
                                    ModInit.modLog.LogMessage(
                                        $"[Vehicle.DamageLocation] directStructureDamage Diff: {directStructureDiff}. Vehicle directStructureDamage decremented to {directStructureDamage}");
                                    BattleArmorAsMech.TakeWeaponDamage(hitInfo, (int)BALocArmor, weapon, 0,
                                        BattleArmorLocStruct, 1, DamageType.Combat);
                                    ModInit.modLog.LogMessage(
                                        $"[Vehicle.DamageLocation] Battle Armor at location {BALocArmor} takes {BattleArmorLocStruct} direct structure damage");
                                    __instance.Combat.MessageCenter.PublishMessage(new AddSequenceToStackMessage(new ShowActorInfoSequence(BattleArmorAsMech, Strings.T("Battle Armor Damaged!"), FloatieMessage.MessageNature.StructureDamage, false)));
                                    continue;
                                }

                                else if (directStructureDiff < 0)
                                {
                                    ModInit.modLog.LogMessage(
                                        $"[Vehicle.DamageLocation] directStructureDamage Diff: {directStructureDiff}. Vehicle directStructureDamage decremented to 0");
                                    BattleArmorAsMech.TakeWeaponDamage(hitInfo, (int)BALocArmor, weapon, 0,
                                        Mathf.Abs(directStructureDamage), 1, DamageType.Combat);
                                    ModInit.modLog.LogMessage(
                                        $"[Vehicle.DamageLocation] Battle Armor at location {BALocArmor} takes {directStructureDamage} direct structure damage");
                                    directStructureDamage = 0;
                                    __instance.Combat.MessageCenter.PublishMessage(new AddSequenceToStackMessage(new ShowActorInfoSequence(BattleArmorAsMech, Strings.T("Battle Armor Damaged!"), FloatieMessage.MessageNature.StructureDamage, false)));
                                }
                            }

                            if (totalArmorDamage > 0)
                            {
                                ModInit.modLog.LogMessage(
                                    $"[Vehicle.DamageLocation] totalArmorDamage: {totalArmorDamage}");
                                var totalArmorDamageDiff =
                                    totalArmorDamage - (BattleArmorLocArmor + BattleArmorLocStruct);
                                if (totalArmorDamageDiff > 0)
                                {
                                    totalArmorDamage -= totalArmorDamageDiff;
                                    ModInit.modLog.LogMessage(
                                        $"[Vehicle.DamageLocation] totalArmorDamageDiff Diff: {totalArmorDamageDiff}. Vehicle totalArmorDamage decremented to {totalArmorDamage}");
                                    BattleArmorAsMech.TakeWeaponDamage(hitInfo, (int)BALocArmor, weapon,
                                        Mathf.Abs(totalArmorDamageDiff), 0, 1, DamageType.Combat);
                                    ModInit.modLog.LogMessage(
                                        $"[Vehicle.DamageLocation] Battle Armor at location {BALocArmor} takes {BattleArmorLocArmor} damage");
                                    __instance.Combat.MessageCenter.PublishMessage(new AddSequenceToStackMessage(new ShowActorInfoSequence(BattleArmorAsMech, Strings.T("Battle Armor Damaged!"), FloatieMessage.MessageNature.ArmorDamage, false)));
                                }

                                else if (totalArmorDamageDiff <= 0)
                                {
                                    ModInit.modLog.LogMessage(
                                        $"[Vehicle.DamageLocation] totalArmorDamageDiff Diff: {totalArmorDamageDiff}. Vehicle totalArmorDamage decremented to 0");
                                    BattleArmorAsMech.TakeWeaponDamage(hitInfo, (int)BALocArmor, weapon,
                                        Mathf.Abs(totalArmorDamage), 0, 1, DamageType.Combat);
                                    ModInit.modLog.LogMessage(
                                        $"[Vehicle.DamageLocation] Battle Armor at location {BALocArmor} takes {totalArmorDamage} damage");
                                    totalArmorDamage = 0;
                                    __instance.Combat.MessageCenter.PublishMessage(new AddSequenceToStackMessage(new ShowActorInfoSequence(BattleArmorAsMech, Strings.T("Battle Armor Damaged!"), FloatieMessage.MessageNature.ArmorDamage, false)));
                                }
                            }
                        }
                    }
                }
            }
        }

        [HarmonyPatch(typeof(Turret), "DamageLocation",
            new Type[] { typeof(WeaponHitInfo), typeof(BuildingLocation), typeof(Weapon), typeof(float), typeof(float) })]
        public static class Turret_DamageLocation_Patch
        {
            public static void Prefix(Turret __instance, WeaponHitInfo hitInfo, BuildingLocation bLoc, Weapon weapon, ref float totalArmorDamage, ref float directStructureDamage)
            {
                if (bLoc == BuildingLocation.None || bLoc == BuildingLocation.Invalid)
                {
                    return;
                }

                if (!__instance.HasMountedUnits() && !__instance.HasSwarmingUnits()) return;

                foreach (var squadInfo in ModState.BADamageTrackers.Where(x => x.Value.TargetGUID == __instance.GUID && !x.Value.IsSquadInternal && x.Value.BA_MountedLocations.ContainsValue((int)bLoc)))
                {
                    ModInit.modLog.LogTrace($"[Turret.DamageLocation] Evaluating {squadInfo.Key} for {squadInfo.Value.TargetGUID}");
                    if (ModInit.Random.NextDouble() > (double)1 / 3) continue;
                    if (__instance.Combat.FindActorByGUID(squadInfo.Key) is Mech BattleArmorAsMech)
                    {
                        var BattleArmorMounts = squadInfo.Value.BA_MountedLocations.Where(x => x.Value == (int)bLoc);
                        foreach (var mount in BattleArmorMounts)
                        {
                            var BALocArmor = (ArmorLocation)mount.Key;
                            //var BALocArmorString = BattleArmorAsMech.GetStringForArmorLocation(BALocArmor);
                            var BALocStruct = MechStructureRules.GetChassisLocationFromArmorLocation(BALocArmor);
                            //var BALocStructString = BattleArmorAsMech.GetStringForStructureLocation(BALocStruct);

                            var BattleArmorLocArmor = BattleArmorAsMech.ArmorForLocation((int)BALocArmor);
                            var BattleArmorLocStruct = BattleArmorAsMech.StructureForLocation((int)BALocStruct);

                            if (directStructureDamage > 0)
                            {
                                ModInit.modLog.LogMessage(
                                    $"[Turret.DamageLocation] directStructureDamage: {directStructureDamage}");
                                var directStructureDiff = directStructureDamage - BattleArmorLocStruct;
                                if (directStructureDiff >= 0)
                                {
                                    directStructureDamage -= BattleArmorLocStruct;
                                    ModInit.modLog.LogMessage(
                                        $"[Turret.DamageLocation] directStructureDamage Diff: {directStructureDiff}. Turret directStructureDamage decremented to {directStructureDamage}");
                                    BattleArmorAsMech.TakeWeaponDamage(hitInfo, (int) BALocArmor, weapon, 0,
                                        BattleArmorLocStruct, 1, DamageType.Combat);
                                    ModInit.modLog.LogMessage(
                                        $"[Turret.DamageLocation] Battle Armor at location {BALocArmor} takes {BattleArmorLocStruct} direct structure damage");
                                    __instance.Combat.MessageCenter.PublishMessage(new AddSequenceToStackMessage(new ShowActorInfoSequence(BattleArmorAsMech, Strings.T("Battle Armor Damaged!"), FloatieMessage.MessageNature.StructureDamage, false)));
                                    continue;
                                }

                                else if (directStructureDiff < 0)
                                {
                                    ModInit.modLog.LogMessage(
                                        $"[Turret.DamageLocation] directStructureDamage Diff: {directStructureDiff}. Turret directStructureDamage decremented to 0");
                                    BattleArmorAsMech.TakeWeaponDamage(hitInfo, (int) BALocArmor, weapon, 0,
                                        Mathf.Abs(directStructureDamage), 1, DamageType.Combat);
                                    ModInit.modLog.LogMessage(
                                        $"[Turret.DamageLocation] Battle Armor at location {BALocArmor} takes {directStructureDamage} direct structure damage");
                                    directStructureDamage = 0;
                                    __instance.Combat.MessageCenter.PublishMessage(new AddSequenceToStackMessage(new ShowActorInfoSequence(BattleArmorAsMech, Strings.T("Battle Armor Damaged!"), FloatieMessage.MessageNature.StructureDamage, false)));
                                }
                            }

                            if (totalArmorDamage > 0)
                            {
                                ModInit.modLog.LogMessage(
                                    $"[Turret.DamageLocation] totalArmorDamage: {totalArmorDamage}");
                                var totalArmorDamageDiff =
                                    totalArmorDamage - (BattleArmorLocArmor + BattleArmorLocStruct);
                                if (totalArmorDamageDiff > 0)
                                {
                                    totalArmorDamage -= totalArmorDamageDiff;
                                    ModInit.modLog.LogMessage(
                                        $"[Turret.DamageLocation] totalArmorDamageDiff Diff: {totalArmorDamageDiff}. Turret totalArmorDamage decremented to {totalArmorDamage}");
                                    BattleArmorAsMech.TakeWeaponDamage(hitInfo, (int)BALocArmor, weapon,
                                        Mathf.Abs(totalArmorDamageDiff), 0, 1, DamageType.Combat);
                                    ModInit.modLog.LogMessage(
                                        $"[Turret.DamageLocation] Battle Armor at location {BALocArmor} takes {BattleArmorLocArmor} damage");
                                    __instance.Combat.MessageCenter.PublishMessage(new AddSequenceToStackMessage(new ShowActorInfoSequence(BattleArmorAsMech, Strings.T("Battle Armor Damaged!"), FloatieMessage.MessageNature.ArmorDamage, false)));
                                }

                                else if (totalArmorDamageDiff <= 0)
                                {
                                    ModInit.modLog.LogMessage(
                                        $"[Turret.DamageLocation] totalArmorDamageDiff Diff: {totalArmorDamageDiff}. Turret totalArmorDamage decremented to 0");
                                    BattleArmorAsMech.TakeWeaponDamage(hitInfo, (int)BALocArmor, weapon,
                                        Mathf.Abs(totalArmorDamage), 0, 1, DamageType.Combat);
                                    ModInit.modLog.LogMessage(
                                        $"[Turret.DamageLocation] Battle Armor at location {BALocArmor} takes {totalArmorDamage} damage");
                                    totalArmorDamage = 0;
                                    __instance.Combat.MessageCenter.PublishMessage(new AddSequenceToStackMessage(new ShowActorInfoSequence(BattleArmorAsMech, Strings.T("Battle Armor Damaged!"), FloatieMessage.MessageNature.ArmorDamage, false)));
                                }
                            }
                        }
                    }
                }
            }
        }

        [HarmonyPatch(typeof(CombatHUDMechTrayArmorHover), "setToolTipInfo",
            new Type[] {typeof(Mech), typeof(ArmorLocation)})]
        public static class CombatHUDMechTrayArmorHover_setToolTipInfo
        {
            public static void Postfix(CombatHUDMechTrayArmorHover __instance, Mech mech, ArmorLocation location)
            {
                if (!mech.HasSwarmingUnits() && !mech.HasMountedUnits()) return;
                var tooltip = Traverse.Create(__instance).Property("ToolTip").GetValue<CombatHUDTooltipHoverElement>();
                foreach (var squadInfo in ModState.BADamageTrackers.Where(x =>
                    x.Value.TargetGUID == mech.GUID && !x.Value.IsSquadInternal &&
                    x.Value.BA_MountedLocations.ContainsValue((int)location)))
                {
                    ModInit.modLog.LogTrace(
                        $"[CombatHUDMechTrayArmorHover.setToolTipInfo] Evaluating {squadInfo.Key} for {squadInfo.Value.TargetGUID} for tooltip infos");
                    
                    if (mech.Combat.FindActorByGUID(squadInfo.Key) is Mech BattleArmorAsMech)
                    {
                        var BattleArmorMounts = squadInfo.Value.BA_MountedLocations.Where(x => x.Value == (int) location);
                        foreach (var mount in BattleArmorMounts)
                        {

                            var BALocArmor = (ArmorLocation)mount.Key;
                            //var BALocArmorString = BattleArmorAsMech.GetStringForArmorLocation(BALocArmor);
                            var BALocStruct = MechStructureRules.GetChassisLocationFromArmorLocation(BALocArmor);
                            //var BALocStructString = BattleArmorAsMech.GetStringForStructureLocation(BALocStruct);

                            var BattleArmorLocArmor = BattleArmorAsMech.ArmorForLocation((int)BALocArmor);
                            var BattleArmorLocStruct = BattleArmorAsMech.StructureForLocation((int)BALocStruct);
                            var newText =
                                new Localize.Text(
                                    $"Battle Armor: Arm. {Mathf.RoundToInt(BattleArmorLocArmor)} / Str. {Mathf.RoundToInt(BattleArmorLocStruct)}",
                                    Array.Empty<object>());
                            if (mech.team.IsFriendly(BattleArmorAsMech.team))
                            {
                                tooltip.BuffStrings.Add(newText);
                            }
                            else
                            {
                                tooltip.DebuffStrings.Add(newText);
                            }
                        }
                    }
                }
            }
        }

        [HarmonyPatch(typeof(CombatHUDVehicleArmorHover), "setToolTipInfo",
            new Type[] { typeof(Vehicle), typeof(VehicleChassisLocations) })]
        public static class CombatHUDVehicleArmorHover_setToolTipInfo
        {
            public static void Postfix(CombatHUDVehicleArmorHover __instance, Vehicle vehicle, VehicleChassisLocations location)
            {
                if (!vehicle.HasSwarmingUnits() && !vehicle.HasMountedUnits()) return;
                var tooltip = Traverse.Create(__instance).Property("ToolTip").GetValue<CombatHUDTooltipHoverElement>();
                foreach (var squadInfo in ModState.BADamageTrackers.Where(x =>
                    x.Value.TargetGUID == vehicle.GUID && !x.Value.IsSquadInternal &&
                    x.Value.BA_MountedLocations.ContainsValue((int)location)))
                {
                    ModInit.modLog.LogTrace(
                        $"[CombatHUDMechTrayArmorHover.setToolTipInfo] Evaluating {squadInfo.Key} for {squadInfo.Value.TargetGUID} for tooltip infos");

                    if (vehicle.Combat.FindActorByGUID(squadInfo.Key) is Mech BattleArmorAsMech)
                    {
                        var BattleArmorMounts = squadInfo.Value.BA_MountedLocations.Where(x => x.Value == (int)location);
                        foreach (var mount in BattleArmorMounts)
                        {

                            var BALocArmor = (VehicleChassisLocations)mount.Key;
                            //var BALocArmorString = BattleArmorAsMech.GetStringForArmorLocation(BALocArmor);
                            //var BALocStructString = BattleArmorAsMech.GetStringForStructureLocation(BALocStruct);

                            var BattleArmorLocArmor = BattleArmorAsMech.ArmorForLocation((int)BALocArmor);
                            var BattleArmorLocStruct = BattleArmorAsMech.StructureForLocation((int)BALocArmor);
                            var newText =
                                new Localize.Text(
                                    $"Battle Armor: Arm. {Mathf.RoundToInt(BattleArmorLocArmor)} / Str. {Mathf.RoundToInt(BattleArmorLocStruct)}",
                                    Array.Empty<object>());
                            if (vehicle.team.IsFriendly(BattleArmorAsMech.team))
                            {
                                tooltip.BuffStrings.Add(newText);
                            }
                            else
                            {
                                tooltip.DebuffStrings.Add(newText);
                            }
                        }
                    }
                }
            }
        }

        [HarmonyPatch(typeof(LineOfSight), "GetLineOfFireUncached")]
        public static class LineOfSight_GetLineOfFireUncached
        {
            public static bool Prefix(LineOfSight __instance, AbstractActor source, Vector3 sourcePosition, ICombatant target, Vector3 targetPosition, Quaternion targetRotation, out Vector3 collisionWorldPos, ref LineOfFireLevel __result)
            {
                collisionWorldPos = new Vector3();
                if (!(target is AbstractActor actorTarget))
                {
                    return true;
                }

                if (!actorTarget.HasSwarmingUnits() && !actorTarget.HasMountedUnits())
                {
                    return true;
                }

                Vector3 forward = targetPosition - sourcePosition;
                forward.y = 0f;
                Quaternion rotation = Quaternion.LookRotation(forward);
                Vector3[] lossourcePositions = source.GetLOSSourcePositions(sourcePosition, rotation);
                Vector3[] lostargetPositions = target.GetLOSTargetPositions(targetPosition, targetRotation);


                List<AbstractActor> list = new List<AbstractActor>(source.Combat.AllActors);
                list.Remove(source);

                var unitGUIDs = new List<string>(ModState.PositionLockSwarm.Keys);
                unitGUIDs.AddRange(ModState.PositionLockMount.Keys);
                foreach (var actorGUID in unitGUIDs)
                {
                    list.Remove(source.Combat.FindActorByGUID(actorGUID));
                }

                AbstractActor abstractActor = actorTarget;
                string text = null;
                if (abstractActor != null)
                {
                    list.Remove(abstractActor);
                }
                else
                {
                    text = target.GUID;
                }
                LineSegment lineSegment = new LineSegment(sourcePosition, targetPosition);
                list.Sort((AbstractActor x, AbstractActor y) => Vector3.SqrMagnitude(x.CurrentPosition - sourcePosition).CompareTo(Vector3.SqrMagnitude(y.CurrentPosition - sourcePosition)));
                float num = Vector3.SqrMagnitude(sourcePosition - targetPosition);
                for (int i = list.Count - 1; i >= 0; i--)
                {
                    if (list[i].IsDead || Vector3.SqrMagnitude(list[i].CurrentPosition - sourcePosition) > num || lineSegment.DistToPoint(list[i].CurrentPosition) > list[i].Radius * 5f)
                    {
                        list.RemoveAt(i);
                    }
                }
                float num2 = 0f;
                float num3 = 0f;
                float num4 = 0f;
                collisionWorldPos = targetPosition;
                float num5 = 999999.9f;
                Weapon longestRangeWeapon = source.GetLongestRangeWeapon(false, false);
                float num6 = (longestRangeWeapon == null) ? 0f : longestRangeWeapon.MaxRange;
                float adjustedSpotterRange = source.Combat.LOS.GetAdjustedSpotterRange(source, abstractActor);
                num6 = Mathf.Max(num6, adjustedSpotterRange);
                float num7 = Mathf.Pow(num6, 2f);
                for (int j = 0; j < lossourcePositions.Length; j++)
                {
                    for (int k = 0; k < lostargetPositions.Length; k++)
                    {
                        num3 += 1f;
                        if (Vector3.SqrMagnitude(lossourcePositions[j] - lostargetPositions[k]) <= num7)
                        {
                            lineSegment = new LineSegment(lossourcePositions[j], lostargetPositions[k]);
                            bool flag = false;
                            Vector3 vector;
                            if (text == null)
                            {
                                for (int l = 0; l < list.Count; l++)
                                {
                                    if (lineSegment.DistToPoint(list[l].CurrentPosition) < list[l].Radius)
                                    {
                                        vector = NvMath.NearestPointStrict(lossourcePositions[j], lostargetPositions[k], list[l].CurrentPosition);
                                        float num8 = Vector3.Distance(vector, list[l].CurrentPosition);
                                        if (num8 < list[l].HighestLOSPosition.y)
                                        {
                                            flag = true;
                                            num4 += 1f;
                                            if (num8 < num5)
                                            {
                                                num5 = num8;
                                                collisionWorldPos = vector;
                                                break;
                                            }
                                            break;
                                        }
                                    }
                                }
                            }
                            if (__instance.HasLineOfFire(lossourcePositions[j], lostargetPositions[k], text, num6, out vector))
                            {
                                num2 += 1f;
                                if (text != null)
                                {
                                    break;
                                }
                            }
                            else
                            {
                                if (flag)
                                {
                                    num4 -= 1f;
                                }
                                float num8 = Vector3.Distance(vector, sourcePosition);
                                if (num8 < num5)
                                {
                                    num5 = num8;
                                    collisionWorldPos = vector;
                                }
                            }
                        }
                    }
                    if (text != null && num2 > 0.5f)
                    {
                        break;
                    }
                }
                float num9 = (text == null) ? (num2 / num3) : num2;
                float b = num9 - source.Combat.Constants.Visibility.MinRatioFromActors;
                float num10 = Mathf.Min(num4 / num3, b);
                if (num10 > 0.001f)
                {
                    num9 -= num10;
                }
                if (num9 >= source.Combat.Constants.Visibility.RatioFullVis)
                {
                    __result = LineOfFireLevel.LOFClear;
                    return false;
                }
                if (num9 >= source.Combat.Constants.Visibility.RatioObstructedVis)
                {
                    __result = LineOfFireLevel.LOFObstructed;
                    return false;
                }
                __result = LineOfFireLevel.LOFBlocked;
                return false;
            }
        }
    }
}