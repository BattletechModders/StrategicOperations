﻿using System;
using System.Collections.Generic;
using System.Linq;
using Abilifier.Patches;
using BattleTech;
using BattleTech.Rendering;
using BattleTech.UI;
using CustomUnits;
using HBS;
using StrategicOperations.Framework;
using UnityEngine;

namespace StrategicOperations.Patches
{
    public class StrategicSelection
    {
        public class SelectionStateMWTargetSingle_Stratops : AbilityExtensions.SelectionStateMWTargetSingle
        {
            public SelectionStateMWTargetSingle_Stratops(CombatGameState Combat, CombatHUD HUD,
                CombatHUDActionButton FromButton) : base(Combat, HUD, FromButton)
            {
            }

            public override bool CanTargetCombatant(ICombatant potentialTarget)
            {
                if (SelectedActor.VisibilityToTargetUnit(potentialTarget) == VisibilityLevel.None)
                {
                    return false;
                }

                if (FromButton.Ability.Def.Id == ModInit.modSettings.AirliftAbilityID)
                {
                    if (SelectedActor.GUID == potentialTarget.GUID && SelectedActor.HasAirliftedUnits())
                    {
                        return true;
                        //ignore base CanTarget if self-selecting, only for Airlift (to give popup for drop selection)
                    }
                }

                if (!base.CanTargetCombatant(potentialTarget)) return false;

                if (FromButton.Ability.Def.Id == ModInit.modSettings.ResupplyConfig.ResupplyAbilityID)
                {
                    if (potentialTarget is AbstractActor targetActor && targetActor.IsResupplyUnit)
                    {
                        return true;
                    }
                    return false;
                }

                if (FromButton.Ability.Def.Id == ModInit.modSettings.BattleArmorMountAndSwarmID)
                {

                    if (potentialTarget is BattleTech.Building building)
                    {
                        if (ModInit.modSettings.DisableGarrisons) return false;
                        if (!Combat.EncounterLayerData.IsInEncounterBounds(building.CurrentPosition)) return false;
                        if (SelectedActor.IsGarrisonedInTargetBuilding(building)) return true;
                        if (building.HasGarrisonedUnits()) return false;
                        if (building.team.IsNeutral(SelectedActor.team) || building.team.IsFriendly(SelectedActor.team))
                        {
                            return true;
                        }
                        return false;
                    }

                    else if (potentialTarget is AbstractActor targetActor)
                    {
                        if (SelectedActor == targetActor || targetActor is TrooperSquad)
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

                            if (targetActor.GetIsUnMountable())
                            {
                                return false;
                            }

                            if (!SelectedActor.GetIsBattleArmorHandsy() && !targetActor.GetHasBattleArmorMounts() &&
                                targetActor.GetAvailableInternalBASpace() <= 0)
                            {
                                return false;
                            }

                            if (SelectedActor.IsMountedUnit() && !targetActor.HasMountedUnits())
                            {
                                return false;
                            }

                            if (!SelectedActor.IsMountedUnit() && SelectedActor.CanRideInternalOnly() &&
                                targetActor.GetAvailableInternalBASpace() <= 0 &&
                                (!ModInit.modSettings.BattleArmorHandsyOverridesInternalOnly ||
                                 !SelectedActor.GetIsBattleArmorHandsy()))
                            {
                                return false;
                            }

                            if (!SelectedActor.IsMountedUnit())
                            {
                                if (!targetActor.HasMountedUnits() ||
                                    targetActor.GetAvailableInternalBASpace() > 0 ||
                                    (targetActor.GetHasBattleArmorMounts() &&
                                     !targetActor.GetHasExternalMountedBattleArmor()))
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

                            if (targetActor.GetIsUnSwarmable() || !SelectedActor.CanSwarm())
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
                }
                else if (FromButton.Ability.Def.Id == ModInit.modSettings.AirliftAbilityID)
                {
                    if (potentialTarget is AbstractActor targetActor)
                    {
                        if (targetActor.GetTags().Any(x => ModInit.modSettings.AirliftImmuneTags.Contains(x)))
                            return false;
                        if (SelectedActor.HasActivatedThisRound) return false;
                        if (SelectedActor.IsAirliftingTargetUnit(targetActor))
                        {
                            return true;
                        }
                        if (targetActor.IsMountedUnit() || targetActor.IsSwarmingUnit() ||
                            targetActor.HasSwarmingUnits() || targetActor.HasMountedUnits() ||
                            targetActor.IsAirlifted() || targetActor.HasAirliftedUnits())
                        {
                            return false;
                        }
                        if (targetActor.team.IsFriendly(SelectedActor.team))
                        {
                            if (SelectedActor.GetHasAvailableInternalLiftCapacityForTarget(targetActor) ||
                                SelectedActor.GetHasAvailableExternalLiftCapacityForTarget(targetActor))
                            {
                                return true;
                            }

                            return false;
                        }
                        else
                        {
                            if (SelectedActor.GetHasAvailableExternalLiftCapacityForTarget(targetActor) && SelectedActor.GetCanAirliftHostiles())
                            {
                                return true;
                            }

                            return false;
                        }
                    }
                }
                return true;
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

                if (this.FromButton.Ability.Def.Id == ModInit.modSettings.BattleArmorMountAndSwarmID)
                {
                    if (!SelectedActor.IsMountedUnit() && !SelectedActor.IsSwarmingUnit() &&
                        !SelectedActor.IsGarrisoned())
                    {
                        var mountTargets = SelectedActor.GetAllFriendliesWithinRange(maxRange);
                        var swarmTargets = SelectedActor.GetAllEnemiesWithinRange(maxRange);

                        mountTargets.RemoveAll(x => !this.CanTargetCombatant(x));
                        swarmTargets.RemoveAll(x => !this.CanTargetCombatant(x));

                        HUD.InWorldMgr.HideMeleeTargets();
                        HUD.InWorldMgr.ShowBATargetsMeleeIndicator(swarmTargets, SelectedActor);

                        StrategicTargetIndicatorsManager.InitReticles(HUD, mountTargets.Count);
                        for (var index = 0; index < mountTargets.Count; index++)
                        {
                            StrategicTargetIndicatorsManager.ReticleGOs[index].SetActive(true);
                            var reticle = StrategicTargetIndicatorsManager.ReticleGOs[index]
                                .GetComponent<StrategicTargetReticle>();
                            var isFriendly = mountTargets[index].team.IsFriendly(SelectedActor.team);
                            reticle.SetScaleAndLocation(mountTargets[index].CurrentPosition, 10f);
                            reticle.UpdateColorAndStyle(isFriendly, false, false);
                            ModInit.modLog?.Trace?.Write(
                                $"[HighlightPotentialTargets - BattleArmorMountAndSwarmID] Updating reticle at index {index}, isFriendly {isFriendly}.");
                        }

                        StrategicTargetIndicatorsManager.ShowRoot();
                    }
                }
                else if (this.FromButton.Ability.Def.Id == ModInit.modSettings.ResupplyConfig.ResupplyAbilityID)
                {
                    if (!SelectedActor.IsMountedUnit() && !SelectedActor.IsSwarmingUnit() &&
                        !SelectedActor.IsGarrisoned())
                    {
                        var resupplyTargets = SelectedActor.GetAllFriendliesWithinRange(1000f);

                        resupplyTargets.RemoveAll(x => !this.CanTargetCombatant(x));

                        HUD.InWorldMgr.HideMeleeTargets();
                        StrategicTargetIndicatorsManager.InitReticles(HUD, resupplyTargets.Count);
                        for (var index = 0; index < resupplyTargets.Count; index++)
                        {
                            if (Vector3.Distance(SelectedActor.CurrentPosition,
                                    resupplyTargets[index].CurrentPosition) <= this.FromButton.Ability.Def.IntParam2)
                            {
                                StrategicTargetIndicatorsManager.ReticleGOs[index].SetActive(true);
                                var reticle = StrategicTargetIndicatorsManager.ReticleGOs[index]
                                    .GetComponent<StrategicTargetReticle>();
                                var isFriendly = resupplyTargets[index].team.IsFriendly(SelectedActor.team);
                                reticle.SetScaleAndLocation(resupplyTargets[index].CurrentPosition, 10f);
                                reticle.UpdateColorAndStyle(isFriendly, true, true);
                                ModInit.modLog?.Trace?.Write(
                                    $"[HighlightPotentialTargets - ResupplyAbility INRANGE] Updating reticle at index {index}, isFriendly {isFriendly}.");
                            }
                            else
                            {
                                StrategicTargetIndicatorsManager.ReticleGOs[index].SetActive(true);
                                var reticle = StrategicTargetIndicatorsManager.ReticleGOs[index]
                                    .GetComponent<StrategicTargetReticle>();
                                var isFriendly = resupplyTargets[index].team.IsFriendly(SelectedActor.team);
                                reticle.SetScaleAndLocation(resupplyTargets[index].CurrentPosition, 10f);
                                reticle.UpdateColorAndStyle(isFriendly, true, false);
                                ModInit.modLog?.Trace?.Write(
                                    $"[HighlightPotentialTargets - ResupplyAbility ANY] Updating reticle at index {index}, isFriendly {isFriendly}.");
                            }
                        }
                        
                        StrategicTargetIndicatorsManager.ShowRoot();
                    }
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
                    var carrier = Combat.FindCombatantByGUID(ModState.PositionLockMount[SelectedActor.GUID]);
                    this.ProcessClickedCombatant(carrier);
                }
                else if (SelectedActor.IsSwarmingUnit())
                {
                    var carrier = Combat.FindCombatantByGUID(ModState.PositionLockSwarm[SelectedActor.GUID]);
                    this.ProcessClickedCombatant(carrier);
                }
                else if (SelectedActor.IsGarrisoned())
                {
                    var carrier = Combat.FindCombatantByGUID(ModState.PositionLockGarrison[SelectedActor.GUID].BuildingGUID);
                    this.ProcessClickedCombatant(carrier);
                }
                else
                {
                    this.HighlightPotentialTargets();
                }
            }

            public override void OnInactivate()
            {
                base.OnInactivate();
                CombatTargetingReticle.Instance.HideReticle();
                StrategicTargetIndicatorsManager.HideReticles();
                HUD.InWorldMgr.HideMeleeTargets();
            }

            public override bool ProcessClickedCombatant(ICombatant combatant)
            {
                if (FromButton.Ability.Def.Id == ModInit.modSettings.ResupplyConfig.ResupplyAbilityID)
                {
                    var resupplyDist = Mathf.RoundToInt(Vector3.Distance(this.HUD.SelectedActor.CurrentPosition, combatant.CurrentPosition));
                    if (resupplyDist > FromButton.Ability.Def.IntParam2)
                    {
                        return false;
                    }
                }
                if (FromButton.Ability.Def.Id == ModInit.modSettings.AirliftAbilityID)
                {
                    if (SelectedActor.GUID == combatant.GUID)
                    {
                        ICombatant newUnitSelection = null;
                        var unitsCarried = SelectedActor.GetAirliftedUnits();
                        //handle 0 units carried here
                        if (unitsCarried.Count < 1) return false;
                        var unitsCarriedDesc = "";
                        for (var index = 0; index < unitsCarried.Count; index++)
                        {
                            var unit = unitsCarried[index];
                            var IFF = "ENEMY";
                            if (unit.team.IsFriendly(SelectedActor.team)) IFF = "FRIENDLY";
                            unitsCarriedDesc += $"{index + 1}: {IFF} {unit.DisplayName}\n\n";
                        }

                        var popup = GenericPopupBuilder
                        .Create("Select a unit to drop off",
                            unitsCarriedDesc)
                        .AddFader(new UIColorRef?(LazySingletonBehavior<UIManager>.Instance.UILookAndColorConstants
                            .PopupBackfill));
                        popup.AlwaysOnTop = true;
                        switch (unitsCarried.Count)
                        {
                            case 0:
                                {
                                    goto RenderNow;
                                }
                            case 1:
                                {
                                    var unit = unitsCarried[0];

                                    popup.AddButton("1.", (Action)(() =>
                                    {
                                        base.ProcessClickedCombatant(unit);
                                        newUnitSelection = unit;
                                    }));
                                    goto RenderNow;
                                }
                            case 2:
                                {
                                    var unit = unitsCarried[0];
                                    var unit1 = unit;
                                    popup.AddButton("1.", (Action)(() =>
                                    {
                                        base.ProcessClickedCombatant(unit1);
                                        newUnitSelection = unit1;
                                    }));

                                    unit = unitsCarried[1];
                                    popup.AddButton("2.", (Action)(() =>
                                    {
                                        base.ProcessClickedCombatant(unit);
                                        newUnitSelection = unit;
                                    }));
                                    goto RenderNow;
                                }
                            case 3:
                            {
                                {
                                    var unit = unitsCarried[0];
                                    var unit1 = unit;
                                    popup.AddButton("1.", (Action)(() =>
                                    {
                                        base.ProcessClickedCombatant(unit1);
                                        newUnitSelection = unit1;
                                    }));

                                    unit = unitsCarried[2];
                                    var unit2 = unit;
                                    popup.AddButton("3.", (Action)(() =>
                                    {
                                        base.ProcessClickedCombatant(unit2);
                                        newUnitSelection = unit2;
                                    }));

                                    unit = unitsCarried[1];
                                    var unit3 = unit;
                                    popup.AddButton("2.", (Action)(() =>
                                    {
                                        base.ProcessClickedCombatant(unit3);
                                        newUnitSelection = unit3;
                                    }));
                                    goto RenderNow;
                                }
                            }
                        }

                        if (unitsCarried.Count > 3)
                        {
                            var unit = unitsCarried[0];
                            var unit1 = unit;
                            popup.AddButton("1.", (Action)(() =>
                            {
                                base.ProcessClickedCombatant(unit1);
                                newUnitSelection = unit1;
                            }));

                            unit = unitsCarried[2];
                            var unit2 = unit;
                            popup.AddButton("3.", (Action)(() =>
                            {
                                base.ProcessClickedCombatant(unit2);
                                newUnitSelection = unit2;
                            }));

                            unit = unitsCarried[1];
                            var unit3 = unit;
                            popup.AddButton("2.", (Action)(() =>
                            {
                                base.ProcessClickedCombatant(unit3);
                                newUnitSelection = unit3;
                            }));

                            for (var index = 3; index < unitsCarried.Count; index++)
                            {
                                unit = unitsCarried[index];
                                var buttonName = $"{index + 1}.";
                                var unit4 = unit;
                                popup.AddButton(buttonName,
                                    (Action)(() =>
                                    {
                                        base.ProcessClickedCombatant(unit4);
                                        newUnitSelection = unit4;
                                    }));
                            }
                        }
                        RenderNow:
                        popup.CancelOnEscape();
                        popup.Render();

                        if (newUnitSelection != null)
                        {
                            var result = base.ProcessClickedCombatant(newUnitSelection);
                            //return false;  // should i be returning true here?
                            ModInit.modLog?.Trace?.Write($"[ProcessClickedCombatant] Selected combatant should now be {newUnitSelection.DisplayName}. base ProcessClickedCombatant {result}");
                            return result;
                        }
                        return false;
                    }
                }

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

                if (FromButton.Ability.Def.Id == ModInit.modSettings.BattleArmorMountAndSwarmID)
                {
                    if (base.ProcessClickedCombatant(combatant))
                    {
                        if (SelectedActor is Mech creatorMech &&
                            combatant.team.IsEnemy(creatorMech.team))
                        {
                            var chance = creatorMech.Combat.ToHit.GetToHitChance(creatorMech,
                                creatorMech.MeleeWeapon, combatant, creatorMech.CurrentPosition,
                                combatant.CurrentPosition, 1, MeleeAttackType.Charge, false);
                            ModInit.modLog?.Trace?.Write(
                                $"[SelectionState.ShowFireButton - Swarm Success calculated as {chance}, storing in state.");
                            ModState.SwarmSuccessChance = chance;
                            var chanceDisplay = (float) Math.Round(chance, 2) * 100;
                            HUD.AttackModeSelector.FireButton.FireText.SetText($"{chanceDisplay}% - Confirm",
                                Array.Empty<object>());
                        }
                    }
                }
                var result2 = base.ProcessClickedCombatant(combatant);
                ModInit.modLog?.Trace?.Write($"[ProcessClickedCombatant] base ProcessClickedCombatant {result2}");
                return result2;
                //return false;  // should i be returning true here?
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
                        if (FromButton.Ability.Def.Id == ModInit.modSettings.BattleArmorMountAndSwarmID || FromButton.Ability.Def.Id == ModInit.modSettings.AirliftAbilityID || FromButton.Ability.Def.Id == ModInit.modSettings.ResupplyConfig.ResupplyAbilityID)
                        {
                            __result = new SelectionStateMWTargetSingle_Stratops(Combat, HUD, FromButton);
                            return;
                        }
                    }
                }
            }
        }

        public static class StrategicTargetIndicatorsManager
        {
            public static GameObject BaseReticleObject = null;
            public static CombatHUD HUD = null;
            public static List<GameObject> ReticleGOs = new List<GameObject>();

            public static void Clear()
            {
                StrategicTargetIndicatorsManager.ReticleGOs.Clear();
                UnityEngine.Object.Destroy(StrategicTargetIndicatorsManager.BaseReticleObject);
                StrategicTargetIndicatorsManager.BaseReticleObject = null;
                StrategicTargetIndicatorsManager.HUD = null;
            }

            public static void HideReticles()
            {
                foreach (var reticle in StrategicTargetIndicatorsManager.ReticleGOs)
                {
                    reticle.gameObject.SetActive(false);
                }
            }

            public static void InitReticles(CombatHUD hud, int count)
            {
                if (StrategicTargetIndicatorsManager.BaseReticleObject == null)
                {
                    StrategicTargetIndicatorsManager.BaseReticleObject = new GameObject("StrategicTargetingReticles"); //was TargetingCircles
                }
                StrategicTargetIndicatorsManager.HUD = hud;

                while (ReticleGOs.Count < count)
                {
                    ModInit.modLog?.Trace?.Write($"[InitReticle] Need to init new reticles; have {ReticleGOs.Count}");
                    GameObject reticleGO = new GameObject("StrategicReticle"); //was Circle
                    StrategicTargetReticle reticle = reticleGO.AddComponent<StrategicTargetReticle>();
                    //reticle.transform.SetParent(reticleGO.transform);
                    reticle.Init(StrategicTargetIndicatorsManager.HUD);
                    StrategicTargetIndicatorsManager.ReticleGOs.Add(reticleGO);
                }
            }

            public static void ShowRoot()
            {
                StrategicTargetIndicatorsManager.BaseReticleObject.SetActive(true);
            }
        }

        public class StrategicTargetReticle : MonoBehaviour
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
                ModInit.modLog?.Trace?.Write($"[SetScaleAndLocation] Set location to {loc}");
            }

            public void UpdateColorAndStyle(bool IsFriendly, bool IsResupply, bool IsResupplyInRange)
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
                                if (IsFriendly && !IsResupply)
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

                                if (IsResupply && !IsResupplyInRange)
                                {
                                    if (!string.IsNullOrEmpty(ModInit.modSettings.ResupplyConfig.ResupplyIndicatorAsset))
                                    {
                                        var newTexture = dm.GetObjectOfType<Texture2D>(ModInit.modSettings.ResupplyConfig.ResupplyIndicatorAsset,
                                            BattleTechResourceType.Texture2D);
                                        if (newTexture != null) decalsFromCircle[j].DecalPropertyBlock.SetTexture("_MainTex", newTexture);
                                    }

                                    if (ModInit.modSettings.MountIndicatorColor != null)
                                    {
                                        var customColor = new Color(ModInit.modSettings.ResupplyConfig.ResupplyIndicatorColor.Rf,
                                            ModInit.modSettings.ResupplyConfig.ResupplyIndicatorColor.Gf,
                                            ModInit.modSettings.ResupplyConfig.ResupplyIndicatorColor.Bf);
                                        decalsFromCircle[j].DecalPropertyBlock.SetColor("_Color", customColor);

                                    }
                                    else
                                    {
                                        decalsFromCircle[j].DecalPropertyBlock.SetColor("_Color", Color.blue);
                                    }
                                }
                                else if (IsResupply)
                                {
                                    if (!string.IsNullOrEmpty(ModInit.modSettings.ResupplyConfig.ResupplyIndicatorInRangeAsset))
                                    {
                                        var newTexture = dm.GetObjectOfType<Texture2D>(ModInit.modSettings.ResupplyConfig.ResupplyIndicatorInRangeAsset,
                                            BattleTechResourceType.Texture2D);
                                        if (newTexture != null) decalsFromCircle[j].DecalPropertyBlock.SetTexture("_MainTex", newTexture);
                                    }

                                    if (ModInit.modSettings.MountIndicatorColor != null)
                                    {
                                        var customColor = new Color(ModInit.modSettings.ResupplyConfig.ResupplyIndicatorInRangeColor.Rf,
                                            ModInit.modSettings.ResupplyConfig.ResupplyIndicatorInRangeColor.Gf,
                                            ModInit.modSettings.ResupplyConfig.ResupplyIndicatorInRangeColor.Bf);
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
    }
}
