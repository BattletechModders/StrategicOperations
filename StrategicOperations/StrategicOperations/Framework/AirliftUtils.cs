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
using UnityEngine;

namespace StrategicOperations.Framework
{
    public static class AirliftUtils
    {
        public static int getInternalLiftCapacity(this AbstractActor actor)
        {
            return actor.StatCollection.GetValue<int>("InternalLiftCapacity");
        }
        public static int getInternalLiftCapacityUsed(this AbstractActor actor)
        {
            return actor.StatCollection.GetValue<int>("InternalLiftCapacityUsed");
        }
        public static void modifyUsedInternalLiftCapacity(this AbstractActor actor, int value)
        {
            actor.StatCollection.ModifyStat("modifyUsedInternalLiftCapacity", -1, "InternalLiftCapacityUsed", StatCollection.StatOperation.Int_Add, value);
        }
        public static int getAvailableInternalLiftCapacity(this AbstractActor actor)
        {
            return actor.StatCollection.GetValue<int>("InternalLiftCapacity") - actor.StatCollection.GetValue<int>("InternalLiftCapacityUsed");
        }

        public static int getExternalLiftCapacity(this AbstractActor actor)
        {
            return actor.StatCollection.GetValue<int>("ExternalLiftCapacity");
        }
        public static int getExternalLiftCapacityUsed(this AbstractActor actor)
        {
            return actor.StatCollection.GetValue<int>("ExternalLiftCapacityUsed");
        }
        public static void modifyUsedExternalLiftCapacity(this AbstractActor actor, int value)
        {
            actor.StatCollection.ModifyStat("modifyUsedExternalLiftCapacity", -1, "ExternalLiftCapacityUsed", StatCollection.StatOperation.Int_Add, value);
        }
        public static int getAvailableExternalLiftCapacity(this AbstractActor actor)
        {
            return actor.StatCollection.GetValue<int>("ExternalLiftCapacity") - actor.StatCollection.GetValue<int>("ExternalLiftCapacityUsed");
        }





        public static void MountUnitToAirliftCarrier(this AbstractActor carrier, AbstractActor targetUnit, bool isFriendly)
        {
            if (targetUnit is Mech targetMech)
            {
                foreach (var airliftEffect in ModState.AirliftEffects)
                {
                    if (airliftEffect.FriendlyAirlift && isFriendly)
                    {
                        foreach (var effectData in airliftEffect.effects)
                        {
                            targetMech.Combat.EffectManager.CreateEffect(effectData,
                                airliftEffect.ID,
                                -1, targetMech, targetMech, default(WeaponHitInfo), 1);
                        }
                    }
                    else if (!airliftEffect.FriendlyAirlift && !isFriendly)
                    {
                        foreach (var effectData in airliftEffect.effects)
                        {
                            targetMech.Combat.EffectManager.CreateEffect(effectData,
                                airliftEffect.ID,
                                -1, targetMech, targetMech, default(WeaponHitInfo), 1);
                        }
                    }
                }
                var availableInternalCapacity = carrier.getAvailableInternalLiftCapacity();
                var availableExternalCapacity = carrier.getAvailableExternalLiftCapacity();
                var unitTonnage = Mathf.RoundToInt(targetMech.tonnage);
                if (isFriendly)
                {
                    if (ModInit.modSettings.AirliftCapacityByTonnage)
                    {
                        if (availableInternalCapacity >= unitTonnage)
                        {
                            ModInit.modLog.LogMessage(
                                $"[MountUnitToAirliftCarrier] - target unit {carrier.DisplayName} has available internal lift capacity of {availableInternalCapacity}; mounting {targetMech.DisplayName} internally.");
                            carrier.modifyUsedInternalLiftCapacity(unitTonnage);
                            ModState.AirliftTrackers.Add(targetMech.GUID, new Classes.AirliftTracker(carrier.GUID, true, true, 0f));
                            //shrinkadink
                            targetMech.GameRep.transform.localScale = new Vector3(.01f, .01f, .01f);
                            targetMech.GameRep.ToggleHeadlights(false);
                            return;
                        }
                        if (availableExternalCapacity >= unitTonnage)
                        {
                            ModInit.modLog.LogMessage(
                                $"[MountUnitToAirliftCarrier] - target unit {carrier.DisplayName} has available external lift capacity of {availableExternalCapacity}; mounting {targetMech.DisplayName} externally.");
                            carrier.modifyUsedExternalLiftCapacity(unitTonnage);
                            ModState.AirliftTrackers.Add(targetMech.GUID, new Classes.AirliftTracker(carrier.GUID, false, true, 5f));
                        }
                    }
                    else
                    {
                        if (availableInternalCapacity > 0)
                        {
                            carrier.modifyUsedInternalLiftCapacity(1);
                            ModState.AirliftTrackers.Add(targetMech.GUID, new Classes.AirliftTracker(carrier.GUID, false, true, 0f));
                            //shrinkadink
                            targetMech.GameRep.transform.localScale = new Vector3(.01f, .01f, .01f);
                            targetMech.GameRep.ToggleHeadlights(false);
                            return;
                        }
                        if (availableExternalCapacity > 0)
                        {
                            ModInit.modLog.LogMessage(
                                $"[MountUnitToAirliftCarrier] - target unit {carrier.DisplayName} has available external lift capacity of {availableExternalCapacity}; mounting {targetMech.DisplayName} externally.");
                            carrier.modifyUsedExternalLiftCapacity(1);
                            ModState.AirliftTrackers.Add(targetMech.GUID, new Classes.AirliftTracker(carrier.GUID, false, true, 5f));
                        }
                    }
                }
                else
                {
                    if (ModInit.modSettings.AirliftCapacityByTonnage)
                    {
                        if (availableExternalCapacity >= unitTonnage)
                        {
                            ModInit.modLog.LogMessage(
                                $"[MountUnitToAirliftCarrier] - target unit {carrier.DisplayName} has available external lift capacity of {availableExternalCapacity}; mounting {targetMech.DisplayName} externally.");
                            carrier.modifyUsedExternalLiftCapacity(unitTonnage);
                            ModState.AirliftTrackers.Add(targetMech.GUID, new Classes.AirliftTracker(carrier.GUID, false, true, 5f));
                        }
                    }
                    else
                    {
                        if (availableExternalCapacity > 0)
                        {
                            ModInit.modLog.LogMessage(
                                $"[MountUnitToAirliftCarrier] - target unit {carrier.DisplayName} has available external lift capacity of {availableExternalCapacity}; mounting {targetMech.DisplayName} externally.");
                            carrier.modifyUsedExternalLiftCapacity(1);
                            ModState.AirliftTrackers.Add(targetMech.GUID, new Classes.AirliftTracker(carrier.GUID, false, true, 5f));
                        }
                    }
                }
            }
        }

        public static void DropAirliftedUnit(this AbstractActor carrier, AbstractActor actor, Vector3 locationOverride, bool calledFromDeswarm = false,
            bool calledFromHandleDeath = false, bool unShrinkRep = true)
        {

            var em = actor.Combat.EffectManager;
            foreach (var airliftEffect in ModState.AirliftEffects)
            {
                var effects = em.GetAllEffectsTargetingWithUniqueID(actor, airliftEffect.ID);
                for (int i = effects.Count - 1; i >= 0; i--)
                {
                    ModInit.modLog.LogMessage(
                        $"[DropAirliftedUnit] Cancelling effect on {actor.DisplayName}: {effects[i].EffectData.Description.Name}.");
                    em.CancelEffect(effects[i]);
                }
            }

            var hud = Traverse.Create(CameraControl.Instance).Property("HUD").GetValue<CombatHUD>();
            //actor.GameRep.IsTargetable = true;

            ModState.AirliftTrackers.Remove(actor.GUID);
            //ModState.PositionLockSwarm.Remove(actor.GUID);
            ModState.CachedUnitCoordinates.Remove(carrier.GUID);

            if (unShrinkRep)
            {
                actor.GameRep.transform.localScale = new Vector3(1f, 1f, 1f);
                //actor.GameRep.transform.localScale = ModState.SavedBAScale[actor.GUID];
                //ModState.SavedBAScale.Remove(actor.GUID);
                //squad.GameRep.ToggleHeadlights(true);
            }
            var point = carrier.CurrentPosition;
            if (locationOverride != Vector3.zero)
            {
                point = locationOverride;
                ModInit.modLog.LogMessage($"[DropAirliftedUnit] Using location override {locationOverride}.");
            }
            point.y = actor.Combat.MapMetaData.GetLerpedHeightAt(point, false);
            actor.TeleportActor(point);

            if (!calledFromHandleDeath && !calledFromDeswarm && false) // dont think i need this, since the unit being dropped won't need to reset state ever?
            {
                ModInit.modLog.LogMessage(
                    $"[DropAirliftedUnit] Not called from HandleDeath or Deswarm, resetting buttons and pathing.");
                hud.MechWarriorTray.JumpButton.ResetButtonIfNotActive(actor);
                hud.MechWarriorTray.SprintButton.ResetButtonIfNotActive(actor);
                hud.MechWarriorTray.MoveButton.ResetButtonIfNotActive(actor);
                hud.SelectionHandler.AddJumpState(actor);
                hud.SelectionHandler.AddSprintState(actor);
                hud.SelectionHandler.AddMoveState(actor);
                actor.ResetPathing(false);
                actor.Pathing.UpdateCurrentPath(false);
            }
            if (false) //(actor.HasBegunActivation)
            {
                ModInit.modLog.LogMessage(
                    $"[DropAirliftedUnit] Called from handledeath? {calledFromHandleDeath} or Deswarm? {calledFromDeswarm}, forcing end of activation."); // was i trying to end carrier activation maybe?

                var sequence = actor.DoneWithActor();
                actor.Combat.MessageCenter.PublishMessage(new AddSequenceToStackMessage(sequence));
                //actor.OnActivationEnd(actor.GUID, -1);
            }

            actor.VisibilityCache.UpdateCacheReciprocal(actor.Combat.GetAllLivingCombatants());

            ModInit.modLog.LogMessage(
                $"[DropAirliftedUnit] Removing PositionLock with rider  {actor.DisplayName} {actor.GUID} and carrier {carrier.DisplayName} {carrier.GUID} and rebuilding visibility cache.");
        }

        public class DetachFromAirliftCarrierDelegate
        {
            public AbstractActor Actor{ get; set; }
            public CustomMech Carrier{ get; set; }
            public float CarrierDownSpeed = -20f;
            public float CarrierUpSpeed = 5f;

            public DetachFromAirliftCarrierDelegate(AbstractActor actor, CustomMech carrier)
            {
                this.Actor = actor;
                this.Carrier = carrier;
            }

            public void OnRestoreHeightControl()
            {
                Carrier.custGameRep.HeightController.UpSpeed = CarrierUpSpeed;
                Carrier.custGameRep.HeightController.DownSpeed = CarrierDownSpeed;
            }
            public void OnLandDetach()
            {
                Actor.GameRep.transform.localScale = new Vector3(1f, 1f, 1f);
                //Actor.GameRep.ToggleHeadlights(true); // maybe toggle headlights if internal?
            }
        }

        internal class AttachToAirliftCarrierDelegate
        {
            public AbstractActor Actor { get; set; }
            public CustomMech Carrier { get; set; }
            public float CarrierDownSpeed = -20f;
            public float CarrierUpSpeed = 5f;

            public AttachToAirliftCarrierDelegate(AbstractActor actor, CustomMech carrier)
            {
                this.Actor = actor;
                this.Carrier = carrier;
            }

            public void OnLandAttach()
            {
                //HIDE SQUAD REPRESENTATION
                //attachTarget.MountBattleArmorToChassis(squad, true);
                Carrier.MountUnitToAirliftCarrier(Actor, true);
                //attachTarget.HideBattleArmorOnChassis(squad);
            }

            public void OnRestoreHeightControl()
            {
                Carrier.custGameRep.HeightController.UpSpeed = CarrierUpSpeed;
                Carrier.custGameRep.HeightController.DownSpeed = CarrierDownSpeed;
                var pos = Carrier.CurrentPosition +
                          Vector3.up * Carrier.custGameRep.HeightController.CurrentHeight;
                Actor.TeleportActor(pos);
            }
        }

        public static void AttachToAirliftCarrier(this AbstractActor actor, AbstractActor carrier, bool isFriendly)
        {
            if (isFriendly)
            {
                ModInit.modLog.LogTrace($"AttachToAirliftCarrier processing on friendly.");
                if (carrier is CustomMech custMech && custMech.FlyingHeight() > 1.5f)
                {
                    //Check if actually flying unit
                    //CALL ATTACH CODE BUT WITHOUT SQUAD REPRESENTATION HIDING
                    //custMech.MountBattleArmorToChassis(squad, false);

                    custMech.custGameRep.HeightController.UpSpeed = 50f;
                    custMech.custGameRep.HeightController.DownSpeed = -50f;

                    var attachDel = new AttachToAirliftCarrierDelegate(actor, custMech);
                    custMech.DropOffAnimation(attachDel.OnLandAttach, attachDel.OnRestoreHeightControl);
                }
                else
                {
                    ModInit.modLog.LogTrace($"AttachToCarrier call mount.");
                    //CALL DEFAULT ATTACH CODE
                    carrier.MountUnitToAirliftCarrier(actor, true);
                }
            }
            else
            {
                ModInit.modLog.LogTrace($"AttachToCarrier call mount.");
                //CALL DEFAULT ATTACH CODE
                carrier.MountUnitToAirliftCarrier(actor, false);
            }
        }

        public static void DetachFromAirliftCarrier(this AbstractActor actor, AbstractActor carrier, bool isFriendly)
        {
            if (isFriendly)
            {
                if (carrier is CustomMech custMech && custMech.FlyingHeight() > 1.5f)
                {
                    //Check if actually flying unit
                    //CALL ATTACH CODE BUT WITHOUT SQUAD REPRESENTATION HIDING
                    //custMech.DismountBA(squad, false, false, false);
                    custMech.custGameRep.HeightController.UpSpeed = 50f;
                    custMech.custGameRep.HeightController.DownSpeed = -50f;

                    var detachDel = new DetachFromAirliftCarrierDelegate(actor, custMech);
                    custMech.DropOffAnimation(detachDel.OnLandDetach, detachDel.OnRestoreHeightControl);
                }
                else
                {
                    ModInit.modLog.LogTrace($"DetachFromAirliftCarrier call DropAirliftedUnit.");
                    //CALL DEFAULT ATTACH CODE
                    carrier.DropAirliftedUnit(actor, Vector3.zero, false, false, true);
                }
            }
            else
            {
                ModInit.modLog.LogTrace($"DetachFromCarrier call DropAirliftedUnit.");
                //CALL DEFAULT ATTACH CODE
                carrier.DropAirliftedUnit(actor, Vector3.zero, false, false, true);
                //squad.DismountBA(carrier, Vector3.zero, false, false, true);
            }
        }
    }
}
