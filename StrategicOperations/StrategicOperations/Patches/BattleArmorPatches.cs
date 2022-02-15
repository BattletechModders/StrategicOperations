using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Abilifier;
using Abilifier.Patches;
using BattleTech;
using BattleTech.Rendering;
using BattleTech.UI;
using CustAmmoCategories;
using CustomActivatableEquipment;
using CustomComponents;
using CustomUnits;
using DG.Tweening;
using Harmony;
using HBS.Math;
using HBS.Pooling;
using Localize;
using StrategicOperations.Framework;
using SVGImporter;
using UnityEngine;
using UnityEngine.UI;
using MechStructureRules = BattleTech.MechStructureRules;
using ObjectSpawnDataSelf = CustAmmoCategories.ObjectSpawnDataSelf;
using Random = System.Random;
using Text = Localize.Text;
using TrooperSquad = CustomUnits.TrooperSquad;

namespace StrategicOperations.Patches
{
    public class BattleArmorSelection
    {
        public static class BA_TargetIndicatorsManager
        {
            public static List<GameObject> ReticleGOs = new List<GameObject>();
            public static CombatHUD HUD = null;
            public static GameObject BaseReticleObject = null;

            public static void Clear()
            {
                BA_TargetIndicatorsManager.ReticleGOs.Clear();
                UnityEngine.Object.Destroy(BA_TargetIndicatorsManager.BaseReticleObject);
                BA_TargetIndicatorsManager.BaseReticleObject = null;
                BA_TargetIndicatorsManager.HUD = null;
            }
            public static void HideReticles()
            {
                foreach (var reticle in BA_TargetIndicatorsManager.ReticleGOs)
                {
                    reticle.gameObject.SetActive(false);
                }
            }
            public static void InitReticles(CombatHUD hud, int count)
            {
                if (BA_TargetIndicatorsManager.BaseReticleObject == null)
                {
                    BA_TargetIndicatorsManager.BaseReticleObject = new GameObject("BATargetingReticles"); //was TargetingCircles
                }
                BA_TargetIndicatorsManager.HUD = hud;

                while (ReticleGOs.Count < count)
                {
                    ModInit.modLog.LogTrace($"[InitReticle] Need to init new reticles; have {ReticleGOs.Count}");
                    GameObject reticleGO = new GameObject("BAReticle"); //was Circle
                    BattleArmorTargetReticle reticle = reticleGO.AddComponent<BattleArmorTargetReticle>();
                    //reticle.transform.SetParent(reticleGO.transform);
                    reticle.Init(BA_TargetIndicatorsManager.HUD);
                    BA_TargetIndicatorsManager.ReticleGOs.Add(reticleGO);
                }
            }
            public static void ShowRoot()
            {
                BA_TargetIndicatorsManager.BaseReticleObject.SetActive(true);
            }
        }

        public class BattleArmorTargetReticle : MonoBehaviour
        {
            public CombatHUD HUD;
            public GameObject ReticleObject;


            public void Init(CombatHUD hud)
            {
                this.HUD = hud;
                ReticleObject = UnityEngine.Object.Instantiate<GameObject>(CombatTargetingReticle.Instance.Circles[0]);
                ReticleObject.transform.SetParent(base.transform);
                Vector3 localScale = ReticleObject.transform.localScale;
                localScale.x = 2f;
                localScale.z = 2f;
                ReticleObject.transform.localScale = localScale;
                ReticleObject.transform.localPosition = Vector3.zero;
            }

            public void SetScaleAndLocation(Vector3 loc, float radius)
            {
                Vector3 localScale = ReticleObject.transform.localScale;
                localScale.x = radius * 2f;
                localScale.z = radius * 2f;
                ReticleObject.transform.localScale = localScale;
                base.transform.position = loc;
                ReticleObject.SetActive(true);
                ReticleObject.gameObject.SetActive(true);
                ModInit.modLog.LogTrace($"[SetScaleAndLocation] Set location to {loc}");
            }

            public void UpdateColorAndStyle(bool IsFriendly)
            {
                var dm = UnityGameInstance.BattleTechGame.DataManager;
                
                Transform[] childComponents;

                childComponents = ReticleObject.GetComponentsInChildren<Transform>(true);
                
                for (int i = 0; i < childComponents.Length; i++)
                {
                    if (childComponents[i].name == "Thumper1")
                    {
                        childComponents[i].gameObject.SetActive(false);
                        continue;
                    }

                    if (childComponents[i].name == "Mortar1")
                    {
                        childComponents[i].gameObject.SetActive(true);
                        var decalsFromCircle = childComponents[i].GetComponentsInChildren<BTUIDecal>();
                        for (int j = 0; j < decalsFromCircle.Length; j++)
                        {
                            if (decalsFromCircle[j].name == "ReticleDecalCircle")
                            {
                                if (IsFriendly)
                                {
                                    if (!string.IsNullOrEmpty(ModInit.modSettings.MountIndicatorAsset))
                                    {
                                        var newTexture = dm.GetObjectOfType<Texture2D>(ModInit.modSettings.MountIndicatorAsset,
                                            BattleTechResourceType.Texture2D);
                                        if (newTexture != null) decalsFromCircle[j].DecalPropertyBlock.SetTexture("_MainTex", newTexture);
                                    }
                                    
                                    if (ModInit.modSettings.MountIndicatorColor != null)
                                    {
                                        var customColor = new Color(ModInit.modSettings.MountIndicatorColor.Rf,
                                            ModInit.modSettings.MountIndicatorColor.Gf,
                                            ModInit.modSettings.MountIndicatorColor.Bf);
                                        decalsFromCircle[j].DecalPropertyBlock.SetColor("_Color", customColor);
                                        
                                    }
                                    else
                                    {
                                        decalsFromCircle[j].DecalPropertyBlock.SetColor("_Color", Color.blue);
                                    }
                                }
                                decalsFromCircle[j].gameObject.SetActive(true);
                            }
                            else
                            {
                                decalsFromCircle[j].gameObject.SetActive(false);
                            }
                        }
                    }
                }
            }
        }

        public class SelectionStateMWTargetSingle_BA : AbilityExtensions.SelectionStateMWTargetSingle
        {
            public SelectionStateMWTargetSingle_BA(CombatGameState Combat, CombatHUD HUD,
                CombatHUDActionButton FromButton) : base(Combat, HUD, FromButton)
            {
            }
            
            public void HighlightPotentialTargets()
            {
                var jumpdist = 0f;
                if (SelectedActor is Mech mech)
                {
                    jumpdist = mech.JumpDistance;
                    if (float.IsNaN(jumpdist)) jumpdist = 0f;
                }

                var ranges = new List<float>()
                {
                    SelectedActor.MaxWalkDistance,
                    SelectedActor.MaxSprintDistance,
                    jumpdist,
                    this.FromButton.Ability.Def.IntParam2
                };
                var maxRange = ranges.Max();

                if (!SelectedActor.IsMountedUnit() && !SelectedActor.IsSwarmingUnit())
                {
                    var mountTargets= SelectedActor.GetAllFriendliesWithinRange(maxRange);
                    var swarmTargets = SelectedActor.GetAllEnemiesWithinRange(maxRange);

                    mountTargets.RemoveAll(x => !this.CanTargetCombatant(x));
                    swarmTargets.RemoveAll(x => !this.CanTargetCombatant(x));

                    HUD.InWorldMgr.ShowBATargetsMeleeIndicator(swarmTargets, SelectedActor);

                    BA_TargetIndicatorsManager.InitReticles(HUD, mountTargets.Count);
                    for (var index = 0; index < mountTargets.Count; index++)
                    {
                        BA_TargetIndicatorsManager.ReticleGOs[index].SetActive(true);
                        var reticle = BA_TargetIndicatorsManager.ReticleGOs[index]
                            .GetComponent<BattleArmorTargetReticle>();
                        var isFriendly = mountTargets[index].team.IsFriendly(SelectedActor.team);
                        reticle.SetScaleAndLocation(mountTargets[index].CurrentPosition, 10f);
                        reticle.UpdateColorAndStyle(isFriendly);
                        ModInit.modLog.LogTrace($"[HighlightPotentialTargets] Updating reticle at index {index}, isFriendly {isFriendly}.");
                    }
                    BA_TargetIndicatorsManager.ShowRoot();
                }
            }

            public override void OnAddToStack()
            {
                base.OnAddToStack();
                this.showTargetingText(this.abilitySelectionText);
                var jumpdist = 0f;
                if (SelectedActor is Mech mech)
                {
                    jumpdist = mech.JumpDistance;
                    if (float.IsNaN(jumpdist)) jumpdist = 0f;
                }

                var ranges = new List<float>()
                {
                    SelectedActor.MaxWalkDistance,
                    SelectedActor.MaxSprintDistance,
                    jumpdist,
                    this.FromButton.Ability.Def.IntParam2
                };
                var maxRange = ranges.Max();
                //CombatTargetingReticle.Instance.UpdateReticle(SelectedActor.CurrentPosition, maxRange, false);
                CombatTargetingReticle.Instance.ShowRangeIndicators(SelectedActor.CurrentPosition, 0f, maxRange, false, true);
                CombatTargetingReticle.Instance.UpdateRangeIndicator(SelectedActor.CurrentPosition, false, true);
                CombatTargetingReticle.Instance.ShowReticle();

                if (SelectedActor.IsMountedUnit())
                {
                    var carrier = Combat.FindActorByGUID(ModState.PositionLockMount[SelectedActor.GUID]);
                    this.ProcessClickedCombatant(carrier);
                }
                else if (SelectedActor.IsSwarmingUnit())
                {
                    var carrier = Combat.FindActorByGUID(ModState.PositionLockSwarm[SelectedActor.GUID]);
                    this.ProcessClickedCombatant(carrier);
                }
                else
                {
                    this.HighlightPotentialTargets();
                }
            }
            protected override bool CanTargetCombatant(ICombatant potentialTarget)
            {
                if (!base.CanTargetCombatant(potentialTarget)) return false;
                if (FromButton.Ability.Def.Id != ModInit.modSettings.BattleArmorMountAndSwarmID)
                {
                    return true;
                }
                if (potentialTarget is AbstractActor targetActor)
                {
                    if (FromButton.Ability.Def.Id == ModInit.modSettings.BattleArmorMountAndSwarmID && (SelectedActor == targetActor || targetActor is TrooperSquad))
                    {
                        return false;
                    }

                    if (SelectedActor.team.IsFriendly(targetActor.team))
                    {
                        if (SelectedActor.IsSwarmingUnit())
                        {
                            return false;
                        }
                        if (SelectedActor.IsMountedToUnit(targetActor))
                        {
                            return true;
                        }

                        if (SelectedActor.IsMountedUnit() && !SelectedActor.IsMountedToUnit(targetActor))
                        {
                            return false;
                        }

                        if (targetActor.getIsUnMountable())
                        {
                            return false;
                        }

                        if (!SelectedActor.getIsBattleArmorHandsy() && !targetActor.getHasBattleArmorMounts() && targetActor.getAvailableInternalBASpace() <= 0)
                        {
                            return false;
                        }

                        if (SelectedActor.IsMountedUnit() && !targetActor.HasMountedUnits())
                        {
                            return false;
                        }

                        if (!SelectedActor.IsMountedUnit() && SelectedActor.canRideInternalOnly() && targetActor.getAvailableInternalBASpace() <= 0)
                        {
                            return false;
                        }

                        if (!SelectedActor.IsMountedUnit())
                        {
                            if (!targetActor.HasMountedUnits() || targetActor.getAvailableInternalBASpace() > 0 || (targetActor.getHasBattleArmorMounts() && !targetActor.getHasExternalMountedBattleArmor()))
                            {
                                return true;
                            }
                            return false;
                        }
                        return true;
                    }

                    if (SelectedActor.team.IsEnemy(targetActor.team))
                    {
                        if (SelectedActor.IsMountedUnit())
                        {
                            return false;
                        }
                        if (SelectedActor.IsSwarmingTargetUnit(targetActor))
                        {
                            return true;
                        }

                        if (SelectedActor.IsSwarmingUnit() && !SelectedActor.IsSwarmingTargetUnit(targetActor))
                        {
                            return false;
                        }

                        if (targetActor.getIsUnSwarmable() || !SelectedActor.canSwarm())
                        {
                            return false;
                        }

                        if (SelectedActor.IsSwarmingUnit() && !targetActor.HasSwarmingUnits())
                        {
                            return false;
                        }

                        if (!SelectedActor.IsSwarmingUnit())
                        {
                            return true;
                        }
                        return true;
                    }
                }
                return true;
            }
        

            public override bool ProcessClickedCombatant(ICombatant combatant)
            {
                var sourcePos = this.HUD.SelectedActor.CurrentPosition;
                sourcePos.y = 0f;
                var targetPos = combatant.CurrentPosition;
                targetPos.y = 0f;
                var distance = Mathf.RoundToInt(Vector3.Distance(sourcePos, targetPos));

                var jumpdist = 0f;
                if (this.HUD.SelectedActor is Mech mech)
                {
                    jumpdist = mech.JumpDistance;
                    if (float.IsNaN(jumpdist)) jumpdist = 0f;
                }

                var ranges = new List<float>()
                {
                    this.HUD.SelectedActor.MaxWalkDistance,
                    this.HUD.SelectedActor.MaxSprintDistance,
                    jumpdist,
                    this.FromButton.Ability.Def.IntParam2
                };
                var maxRange = ranges.Max();
                if (distance > maxRange)
                {
                    return false;
                }

                if (base.ProcessClickedCombatant(combatant))
                {
                    if (FromButton.Ability.Def.Id == ModInit.modSettings.BattleArmorMountAndSwarmID)
                    {

                        if (SelectedActor is Mech creatorMech && combatant != null && combatant.team.IsEnemy(creatorMech.team))
                        {
                            var chance = creatorMech.Combat.ToHit.GetToHitChance(creatorMech, creatorMech.MeleeWeapon, combatant, creatorMech.CurrentPosition, combatant.CurrentPosition, 1, MeleeAttackType.Charge, false);
                            ModInit.modLog.LogTrace($"[SelectionState.ShowFireButton - Swarm Success calculated as {chance}, storing in state.");
                            ModState.SwarmSuccessChance = chance;
                            var chanceDisplay = (float)Math.Round(chance, 2) * 100;
                            HUD.AttackModeSelector.FireButton.FireText.SetText($"{chanceDisplay}% - Confirm", Array.Empty<object>());
                        }
                    }
                }

                return false;
            }

            public override void OnInactivate()
            {
                base.OnInactivate();
                CombatTargetingReticle.Instance.HideReticle();
                BA_TargetIndicatorsManager.HideReticles();
                HUD.InWorldMgr.HideMeleeTargets();
            }

            [HarmonyPatch(typeof(SelectionState), "GetNewSelectionStateByType",
                new Type[]
                {
                    typeof(SelectionType), typeof(CombatGameState), typeof(CombatHUD), typeof(CombatHUDActionButton),
                    typeof(AbstractActor)
                })]
            public static class SelectionState_GetNewSelectionStateByType
            {
                public static void Postfix(SelectionState __instance, SelectionType type, CombatGameState Combat,
                    CombatHUD HUD, CombatHUDActionButton FromButton, AbstractActor actor, ref SelectionState __result)
                {
                    if (!FromButton || FromButton.Ability == null) return;
                    if (__result is AbilityExtensions.SelectionStateMWTargetSingle selectState)
                    {
                        if (FromButton.Ability.Def.Id == ModInit.modSettings.BattleArmorMountAndSwarmID)
                        {
                            __result = new SelectionStateMWTargetSingle_BA(Combat, HUD, FromButton);
                            return;
                        }
                    }
                }
            }
        }
    }
    public class BattleArmorPatches
    {
        [HarmonyPatch(typeof(AbstractActor), "InitEffectStats",
            new Type[] {})]
        public static class AbstractActor_InitEffectStats
        {
            public static void Postfix(AbstractActor __instance)
            {
                __instance.StatCollection.AddStatistic<bool>("CanSwarm", false);
                __instance.StatCollection.AddStatistic<bool>("BattleArmorInternalMountsOnly", false);
                __instance.StatCollection.AddStatistic<int>("InternalBattleArmorSquadCap", 0);
                __instance.StatCollection.AddStatistic<int>("InternalBattleArmorSquads", 0);
                __instance.StatCollection.AddStatistic<bool>("HasBattleArmorMounts", false);
                __instance.StatCollection.AddStatistic<bool>("HasExternalMountedBattleArmor", false);
                __instance.StatCollection.AddStatistic<bool>("IsBattleArmorHandsy", false);
                __instance.StatCollection.AddStatistic<bool>("IsUnmountableBattleArmor", false);
                __instance.StatCollection.AddStatistic<bool>("IsUnswarmableBattleArmor", false);
                __instance.StatCollection.AddStatistic<bool>("BattleArmorMount", false);
                __instance.StatCollection.AddStatistic<float>("BattleArmorDeSwarmerSwat", 0.3f);
                __instance.StatCollection.AddStatistic<int>("BattleArmorDeSwarmerRollInitPenalty", 0);
                __instance.StatCollection.AddStatistic<int>("BattleArmorDeSwarmerSwatInitPenalty", 0);
                __instance.StatCollection.AddStatistic<float>("BattleArmorDeSwarmerSwatDamage", 0f);
                __instance.StatCollection.AddStatistic<float>("BattleArmorDeSwarmerRoll", 0.5f);
                __instance.StatCollection.AddStatistic<bool>("HasFiringPorts",false);
                __instance.StatCollection.AddStatistic<float>("MovementDeSwarmMinChance", 0.0f);
                __instance.StatCollection.AddStatistic<float>("MovementDeSwarmMaxChance", 1.0f);
                __instance.StatCollection.AddStatistic<float>("MovementDeSwarmEvasivePipsFactor", 0f);
                __instance.StatCollection.AddStatistic<float>("MovementDeSwarmEvasiveJumpMovementMultiplier", 1.0f);
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
                    var enemyActors = component.parent.GetAllEnemiesWithinRange(activatableComponent.Explosion.Range);
                    foreach (var enemyActor in enemyActors)
                    {
                        if (enemyActor is TrooperSquad trooperSquad)
                        {
                            if (trooperSquad.IsSwarmingUnit() && ModState.PositionLockSwarm[trooperSquad.GUID] == component.parent.GUID)
                            {
                                var loc = Vector3.zero;
                                trooperSquad.DismountBA(component.parent, loc, true);
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
            private static bool Prepare() => false;
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
            private static bool Prepare() => false;
            public static bool Prefix(AbilityExtensions.SelectionStateMWTargetSingle __instance, ICombatant potentialTarget, ref bool __result)
            {
                if (__instance.FromButton.Ability.Def.Id != ModInit.modSettings.BattleArmorMountAndSwarmID)
                {
                    return true;
                }
                if (potentialTarget is AbstractActor targetActor)
                {
                    if (__instance.FromButton.Ability.Def.Id == ModInit.modSettings.BattleArmorMountAndSwarmID && (__instance.SelectedActor == targetActor || targetActor is TrooperSquad))
                    {
                        __result = false;
                        return false;
                    }

                    if (__instance.SelectedActor.team.IsFriendly(targetActor.team))
                    {

                        if (__instance.SelectedActor.IsMountedToUnit(targetActor))
                        {
                            __result = true;
                            return false;
                        }

                        if (__instance.SelectedActor.IsMountedUnit() && !__instance.SelectedActor.IsMountedToUnit(targetActor))
                        {
                            __result = false;
                            return false;
                        }

                        if (targetActor.getIsUnMountable())
                        {
                            __result = false;
                            return false;
                        }

                        if (!__instance.SelectedActor.getIsBattleArmorHandsy() && !targetActor.getHasBattleArmorMounts() && targetActor.getAvailableInternalBASpace() <= 0)
                        {
                            __result = false;
                            return false;
                        }

                        if (__instance.SelectedActor.IsMountedUnit() && !targetActor.HasMountedUnits())
                        {
                            __result = false;
                            return false;
                        }

                        if (!__instance.SelectedActor.IsMountedUnit() && __instance.SelectedActor.canRideInternalOnly() && targetActor.getAvailableInternalBASpace() <= 0)
                        {
                            __result = false;
                            return false;
                        }

                        if (!__instance.SelectedActor.IsMountedUnit())
                        {
                            if (!targetActor.HasMountedUnits() || targetActor.getAvailableInternalBASpace() > 0 || (targetActor.getHasBattleArmorMounts() && !targetActor.getHasExternalMountedBattleArmor()))
                            {
                                __result = true;
                                return false;
                            }
                            // figure out carrying capacity here and set true
                            __result = false;
                            return false;
                        }
                        __result = true;
                        return false;
                    }

                    if (__instance.SelectedActor.team.IsEnemy(targetActor.team))
                    {

                        if (__instance.SelectedActor.IsSwarmingTargetUnit(targetActor))
                        {
                            __result = true;
                            return false;
                        }

                        if (__instance.SelectedActor.IsSwarmingUnit() && !__instance.SelectedActor.IsSwarmingTargetUnit(targetActor))
                        {
                            __result = false;
                            return false;
                        }

                        if (targetActor.getIsUnSwarmable() || !__instance.SelectedActor.canSwarm())
                        {
                            __result = false;
                            return false;
                        }

                        if (__instance.SelectedActor.IsSwarmingUnit() && !targetActor.HasSwarmingUnits())
                        {
                            __result = false;
                            return false;
                        }

                        if (!__instance.SelectedActor.IsSwarmingUnit())
                        {
                            __result = true;
                            return false;
                        }
                        __result = true;
                        return false;
                    }
                }
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
                            ModInit.modLog.LogTrace($"[Ability.Activate - Unit has sawemers].");
                            var swarmingUnits = ModState.PositionLockSwarm.Where(x => x.Value == creator.GUID).ToList();

                            if (__instance.Def.Id == ModInit.modSettings.BattleArmorDeSwarmRoll)
                            {
                                creator.ProcessDeswarmRoll(swarmingUnits);
                            }

                            else if (__instance.Def.Id == ModInit.modSettings.BattleArmorDeSwarmSwat)
                            {
                                creator.ProcessDeswarmSwat(swarmingUnits);
                            }

                            else if (__instance.Def.Id == ModInit.modSettings.BattleArmorDeSwarmMovement)
                            {
                                ModInit.modLog.LogTrace($"[Ability.Activate - BattleArmorDeSwarm Movement].");
                                creator.ProcessDeswarmMovement(swarmingUnits); // need to patch ActorMovementSequence complete AND JumpSequence complete AND DFASequencecomplete, and then do magic logic in there. or just do it on
                                return; //return to avoid ending turn for player below. making AI use this properly is gonna suck hind tit.
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
                                //creator.OnActivationEnd(creator.GUID, -1);
                            }
                            return;
                        }

                        if (!creator.IsSwarmingUnit() && !creator.IsMountedUnit())
                        {
                            if (__instance.Def.Id == ModInit.modSettings.BattleArmorMountAndSwarmID && target.team.IsFriendly(creator.team))
                            {
                                creator.ProcessMountFriendly(targetActor);
                            }

                            else if (__instance.Def.Id == ModInit.modSettings.BattleArmorMountAndSwarmID && target.team.IsEnemy(creator.team) && creator is Mech creatorMech && creatorMech.canSwarm())
                            {
                                creatorMech.ProcessSwarmEnemy(targetActor);
                            }
                        }

                        else if (creator.IsSwarmingUnit())
                        {
                            if (__instance.Def.Id == ModInit.modSettings.BattleArmorMountAndSwarmID)
                            {
                                var loc = Vector3.zero;
                                creator.DismountBA(targetActor, loc);
                            }
                        }
                        else if (creator.IsMountedUnit())
                        {
                            if (__instance.Def.Id == ModInit.modSettings.BattleArmorMountAndSwarmID)
                            {
                                if (creator is TrooperSquad squad)
                                {
                                    var loc = Vector3.zero;
                                    //ModInit.modLog.LogTrace($"[Ability.Activate] Called DetachFromCarrier.");
                                    squad.DismountBA(targetActor, loc, false, false, false);
                                    squad.DetachFromCarrier(targetActor);
                                }
                                //creator.DismountBA(targetActor);
                            }
                        }
                    }
                }
            }
        }

        [HarmonyPatch(typeof(AttackDirector.AttackSequence), "IsBreachingShot", MethodType.Getter)]
        public static class AttackDirector_AttackSequence_IsBreachingShot
        {
            public static void Postfix(AttackDirector.AttackSequence __instance, ref bool __result)
            {
                if (!__result)
                {
                    if (__instance.chosenTarget is AbstractActor targetActor)
                    {
                        if (__instance.attacker.IsSwarmingTargetUnit(targetActor))
                        {
                            __result = true;
                        }
                    }
                }
            }
        }


        [HarmonyPatch(typeof(ActorMovementSequence), "CompleteOrders")]
        public static class ActorMovementSequence_CompleteOrders
        {
            public static void Postfix(ActorMovementSequence __instance)
            {
                try
                {
                    if (ModState.DeSwarmMovementInfo?.Carrier?.GUID == __instance?.owningActor?.GUID ||
                        ModState.DeSwarmMovementInfo?.Carrier?.GUID == __instance?.OwningMech?.GUID ||
                        ModState.DeSwarmMovementInfo?.Carrier?.GUID == __instance?.OwningVehicle?.GUID)
                    {
                        var baseChance = __instance.owningActor.getMovementDeSwarmMinChance();
                        var chanceFromPips = __instance.owningActor.EvasivePipsCurrent *
                                             __instance.owningActor.getMovementDeSwarmEvasivePipsFactor();
                        var finalChance = Mathf.Min(baseChance + chanceFromPips,
                            __instance.owningActor.getMovementDeSwarmMaxChance());
                        var roll = ModInit.Random.NextDouble();
                        ModInit.modLog.LogMessage(
                            $"[ActorMovementSequence.CompleteOrders] Found DeSwarmMovementInfo for unit {__instance.owningActor.DisplayName} {__instance.owningActor.GUID}. Rolled {roll} vs finalChance {finalChance} from baseChance {baseChance} and evasive chance {chanceFromPips}");
                        if (roll <= finalChance)
                        {
                            var waypoints = Traverse.Create(__instance).Property("Waypoints")
                                .GetValue<List<WayPoint>>();
                            foreach (var swarmingUnit in ModState.DeSwarmMovementInfo?.SwarmingUnits)
                            {
                                var selectedWaypoint = waypoints.GetRandomElement();
                                ModInit.modLog.LogMessage(
                                    $"[ActorMovementSequence.CompleteOrders] Roll succeeded, plonking {swarmingUnit.DisplayName} at {selectedWaypoint.Position}");
                                swarmingUnit.DismountBA(__instance.owningActor, selectedWaypoint.Position, true);
                            }

                            ModState.DeSwarmMovementInfo = new Classes.BA_DeswarmMovementInfo();
                        }
                    }
                }
                catch (Exception ex)
                {
                    ModInit.modLog.LogError(ex.ToString());
                }
            }
        }

        [HarmonyPatch(typeof(MechJumpSequence), "CompleteOrders")]
        public static class MechJumpSequence_CompleteOrders
        {
            public static void Postfix(MechJumpSequence __instance)
            {
                if (ModState.DeSwarmMovementInfo?.Carrier?.GUID == __instance?.OwningMech?.GUID)
                {
                    var baseChance = __instance.owningActor.getMovementDeSwarmMinChance();
                    var chanceFromPips = __instance.owningActor.EvasivePipsCurrent *
                                         __instance.owningActor.getMovementDeSwarmEvasivePipsFactor();
                    var finalChance = Mathf.Min((baseChance + chanceFromPips) * __instance.owningActor.getMovementDeSwarmEvasiveJumpMovementMultiplier(),
                        __instance.owningActor.getMovementDeSwarmMaxChance());
                    var roll = ModInit.Random.NextDouble();
                    ModInit.modLog.LogMessage($"[ActorMovementSequence.CompleteOrders] Found DeSwarmMovementInfo for unit {__instance.owningActor.DisplayName} {__instance.owningActor.GUID}. Rolled {roll} vs finalChance {finalChance} from (baseChance {baseChance} + evasive chance {chanceFromPips}) x JumpMovementMulti {__instance.owningActor.getMovementDeSwarmEvasiveJumpMovementMultiplier()}");
                    if (roll <= finalChance)
                    {
                        var baseDistance = Vector3.Distance(__instance.StartPos, __instance.FinalPos);

                        foreach (var swarmingUnit in ModState.DeSwarmMovementInfo.SwarmingUnits)
                        {
                            var finalDist = (float)(baseDistance * ModInit.Random.NextDouble());
                            var finalDestination =
                                Utils.LerpByDistance(__instance.StartPos, __instance.FinalPos, finalDist);
                            finalDestination.y = swarmingUnit.Combat.MapMetaData.GetLerpedHeightAt(finalDestination, false); //set proper height on ground.
                            ModInit.modLog.LogMessage(
                                $"[ActorMovementSequence.CompleteOrders] Roll succeeded, plonking {swarmingUnit.DisplayName} at {finalDestination}");
                            swarmingUnit.DismountBA(__instance.owningActor, finalDestination, true);
                            if (swarmingUnit is TrooperSquad swarmingUnitSquad)
                            {
                                var trooperLocs = swarmingUnitSquad.GetPossibleHitLocations(__instance.owningActor);
                                for (int i = 0; i < trooperLocs.Count; i++)
                                {
                                    var hitinfo = new WeaponHitInfo(-1, -1, 0, 0, __instance.owningActor.GUID,
                                        swarmingUnitSquad.GUID, 1, new float[1], new float[1], new float[1],
                                        new bool[1], new int[trooperLocs[i]], new int[1], new AttackImpactQuality[1],
                                        new AttackDirection[1], new Vector3[1], new string[1], new int[trooperLocs[i]]);

                                    swarmingUnitSquad.TakeWeaponDamage(hitinfo, trooperLocs[i],
                                        swarmingUnitSquad.MeleeWeapon, swarmingUnitSquad.MechDef.Chassis.DFASelfDamage,
                                        0, 0, DamageType.DFASelf);
                                }
                            }
                        }
                        ModState.DeSwarmMovementInfo = new Classes.BA_DeswarmMovementInfo();
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
                    var carrier = actor.Combat.FindActorByGUID(ModState.PositionLockMount[actor.GUID]);
                    if (!carrier.hasFiringPorts()) __instance.FireButton.DisableButton();
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
            static bool Prepare() => true; //disabled for now. why?
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
                if (targetUnit is AbstractActor targetActor)
                {
                    if (__instance.IsSwarmingUnit())
                    {
                        if (ModState.PositionLockSwarm[__instance.GUID] == targetActor.GUID)
                        {
//                        ModInit.modLog.LogTrace($"[AbstractActor.HasIndirectLOFToTargetUnit] {__instance.DisplayName} is swarming {targetActor.DisplayName}, forcing direct LOS for weapons");
                            __result = false;
                        }
                    }

                    if (targetActor.IsSwarmingUnit() || targetActor.IsMountedUnit())
                    {
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
                        BattleArmorAsMech.FlagForDeath("Killed When Mount Died", DeathMethod.VitalComponentDestroyed, DamageType.Melee, 0, -1, __instance.GUID, false);
                        BattleArmorAsMech.HandleDeath(__instance.GUID);
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
                var dismount = false || (__instance.DeathMethod == DeathMethod.PilotEjection ||
                                         __instance.DeathMethod == DeathMethod.PilotEjectionActorDisabled ||
                                         __instance.DeathMethod == DeathMethod.PilotEjectionNoMessage ||
                                         __instance.DeathMethod == DeathMethod.DespawnedNoMessage ||
                                         __instance.DeathMethod == DeathMethod.DespawnedEscaped);

                if (__instance.HasSwarmingUnits())
                {
                    var swarmingUnits = new List<KeyValuePair<string, string>>(ModState.PositionLockSwarm.Where(x => x.Value == __instance.GUID).ToList());
                    var wereSwarmingUnitsResponsible = swarmingUnits.Any(x => x.Key == attackerGUID);
                    foreach (var swarmingUnit in swarmingUnits)
                    {
                        var actor = __instance.Combat.FindActorByGUID(swarmingUnit.Key);
                        var squad = actor as TrooperSquad;
                        if (ModInit.Random.NextDouble() <= (double)1 / 3 && !wereSwarmingUnitsResponsible && !dismount)
                        {
                            var trooperLocs = squad.GetPossibleHitLocations(__instance);
                            for (int i = 0; i < trooperLocs.Count; i++)
                            {
                                var cLoc = (ChassisLocations)trooperLocs[i];
                                var hitinfo = new WeaponHitInfo(-1, -1, 0, 0, __instance.GUID, squad.GUID, 1, new float[1], new float[1], new float[1], new bool[1], new int[trooperLocs[i]], new int[1], new AttackImpactQuality[1], new AttackDirection[1], new Vector3[1], new string[1], new int[trooperLocs[i]]);
                                squad.NukeStructureLocation(hitinfo, trooperLocs[i], cLoc, Vector3.up, DamageType.ComponentExplosion);
                            }
                            actor.FlagForDeath("Killed When Mount Died", DeathMethod.VitalComponentDestroyed, DamageType.Melee, 0, -1, __instance.GUID, false);
                            actor.HandleDeath(__instance.GUID);
                            continue;
                        }
                        var loc = Vector3.zero;
                        ModInit.modLog.LogTrace($"[AbstractActor.HandleDeath] Swarmed unit {__instance.DisplayName} destroyed, calling dismount.");
                        actor.DismountBA(__instance, loc, false, true);
                    }
                }

                if (__instance.HasMountedUnits())
                {
                    var mountedUnits = new List<KeyValuePair<string,string>>(ModState.PositionLockMount.Where(x => x.Value == __instance.GUID).ToList());
                    foreach (var mountedUnit in mountedUnits)
                    {
                        var actor = __instance.Combat.FindActorByGUID(mountedUnit.Key);
                        var squad = actor as TrooperSquad;
                        if (ModInit.Random.NextDouble() <= (double)1 / 3 && !dismount)
                        {
                            var trooperLocs = squad.GetPossibleHitLocations(__instance);
                            for (int i = 0; i < trooperLocs.Count; i++)
                            {
                                var cLoc = (ChassisLocations)trooperLocs[i];
                                var hitinfo = new WeaponHitInfo(-1, -1, 0, 0, __instance.GUID, squad.GUID, 1, new float[1], new float[1], new float[1], new bool[1], new int[trooperLocs[i]], new int[1], new AttackImpactQuality[1], new AttackDirection[1], new Vector3[1], new string[1], new int[trooperLocs[i]]);
                                squad.NukeStructureLocation(hitinfo, trooperLocs[i], cLoc, Vector3.up, DamageType.ComponentExplosion);
                            }
                            actor.FlagForDeath("Killed When Mount Died", DeathMethod.VitalComponentDestroyed, DamageType.Melee, 0, -1, __instance.GUID, false);
                            actor.HandleDeath(__instance.GUID);
                            continue;
                        }
                        var loc = Vector3.zero;
                        ModInit.modLog.LogTrace($"[AbstractActor.HandleDeath] Mount {__instance.DisplayName} destroyed, calling dismount.");
                        actor.DismountBA(__instance, loc, false, true);
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


        [HarmonyPatch(typeof(CombatHUDEquipmentSlot), "InitButton",
            new Type[] {typeof(SelectionType), typeof(Ability), typeof(SVGAsset), typeof(string), typeof(string), typeof(AbstractActor) })]
        public static class CombatHUDEquipmentSlot_InitButton
        {
            public static void Postfix(CombatHUDEquipmentSlot __instance, SelectionType SelectionType, Ability Ability, SVGAsset Icon, string GUID, string Tooltip, AbstractActor actor)
            {
                if (actor == null) return;
                if (Ability == null || Ability.Def?.Id != ModInit.modSettings.BattleArmorMountAndSwarmID) return;
                if (actor.IsMountedUnit())
                {
                    __instance.Text.SetText("DISMOUNT BATTLEARMOR", Array.Empty<object>());
                }
                else if (actor.IsSwarmingUnit())
                {
                    __instance.Text.SetText("HALT SWARM ATTACK", Array.Empty<object>());
                }
            }
        }

        [HarmonyPatch(typeof(CombatHUDEquipmentSlot), "ConfirmAbility",
            new Type[] { typeof(AbilityDef.ActivationTiming) })]
        public static class CombatHUDEquipmentSlot_ConfirmAbility
        {
            public static void Postfix(CombatHUDEquipmentSlot __instance, AbilityDef.ActivationTiming timing)
            {
                var HUD = Traverse.Create(__instance).Property("HUD").GetValue<CombatHUD>();
                var theActor = HUD.SelectedActor;
                if (theActor == null) return;
                if (__instance.Ability == null || __instance.Ability?.Def?.Id != ModInit.modSettings.BattleArmorMountAndSwarmID) return;
                if (theActor.IsMountedUnit())
                {
                    __instance.Text.SetText("DISMOUNT BATTLEARMOR", Array.Empty<object>());
                }
                else if (theActor.IsSwarmingUnit())
                {
                    __instance.Text.SetText("HALT SWARM ATTACK", Array.Empty<object>());
                }
                else
                {
                    __instance.Text.SetText(__instance.Ability.Def?.Description.Name);
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
                if (actor.IsSwarmingUnit() || actor.IsMountedUnit())
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

                if (actorTarget.IsSwarmingUnit() || actorTarget.IsMountedUnit())
                {
                    __result = LineOfFireLevel.NotSet; // added 1/11 to block all LOF to swarming/mounted units. NotSet, or should it be LOS.Blocked?
                    return false;
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

                if (source.IsMountedUnit())
                {
                    var carrier = source.Combat.FindActorByGUID(ModState.PositionLockMount[source.GUID]);
                    if (carrier.hasFiringPorts())
                    {
                        list.Remove(source.Combat.FindActorByGUID(ModState.PositionLockMount[source.GUID])); // remove mound from LOS blocking (i have no idea if this will work or is even needed)
                    }
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

        [HarmonyPatch(typeof(WeaponRangeIndicators), "DrawLine")]
        public static class FiringPreviewManager_AllPossibleTargets
        {
            static bool Prepare() => false; //disabled for now. might not be needed?
            public static bool Prefix(WeaponRangeIndicators __instance, Vector3 position, Quaternion rotation, bool isPositionLocked, AbstractActor selectedActor, ICombatant target, bool usingMultifire, bool isLocked, bool isMelee)
            {
                if (target is AbstractActor targetActor && targetActor.IsSwarmingUnit())
                {
                    if (ModState.PositionLockSwarm.ContainsKey(targetActor.GUID) &&
                        ModState.PositionLockSwarm[targetActor.GUID] == selectedActor.GUID)
                    {
                        return false;
                    }
                }
                //alter result to remove currently swarming units from firinglines (this might not be the best place to do it)
                return true;
            }
        }

        [HarmonyPatch(typeof(LOFCache), "GetLineOfFire")]
        public static class LOFCache_GetLineOfFire
        {
            //static bool Prepare() => false;
            public static void Postfix(LOFCache __instance, AbstractActor source, Vector3 sourcePosition, ICombatant target, Vector3 targetPosition, Quaternion targetRotation, out Vector3 collisionWorldPos, ref LineOfFireLevel __result)
            {
                collisionWorldPos = targetPosition;
                if (source.IsMountedUnit())
                {
                    var carrier = source.Combat.FindActorByGUID(ModState.PositionLockMount[source.GUID]);
                    if (carrier.hasFiringPorts())
                    {
                        __result = source.Combat.LOFCache.GetLineOfFire(carrier, carrier.CurrentPosition, target,
                            targetPosition, targetRotation, out collisionWorldPos);
                        //ModInit.modLog.LogDev($"[LOFCache.GetLineOfFire] returning LOF {__result} from carrier {carrier.DisplayName} for squad {source.DisplayName}");
                    }
                }
                //__result = LineOfFireLevel.LOFClear;
            }
        }

        [HarmonyPatch(typeof(MechRepresentation), "ToggleHeadlights")]
        public static class MechRepresentation_ToggleHeadlights
        {
            public static void Postfix(MechRepresentation __instance, bool headlightsActive, List<GameObject> ___headlightReps)
            {
                if (__instance.parentActor.IsSwarmingUnit() || __instance.parentActor.IsMountedUnit())
                {
                    var customRep = __instance as CustomMechRepresentation;
                    if (customRep != null)
                    {
                        customRep._ToggleHeadlights(false);
                    }
                    else
                    {
                        for (int i = 0; i < ___headlightReps.Count; i++)
                        {
                            ___headlightReps[i].SetActive(false);
                        }
                    }
                }
            }
        }
    }
}