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
        public static void modifyAvailableInternalLiftCapacity(this AbstractActor actor, int value)
        {
            actor.StatCollection.ModifyStat("modifyAvailableInternalLiftCapacity", -1, "InternalLiftCapacityUsed", StatCollection.StatOperation.Int_Add, value);
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
        public static void modifyAvailableExternalLiftCapacity(this AbstractActor actor, int value)
        {
            actor.StatCollection.ModifyStat("modifyAvailableExternalLiftCapacity", -1, "ExternalLiftCapacityUsed", StatCollection.StatOperation.Int_Add, value);
        }
        public static int getAvailableExternalLiftCapacity(this AbstractActor actor)
        {
            return actor.StatCollection.GetValue<int>("ExternalLiftCapacity") - actor.StatCollection.GetValue<int>("ExternalLiftCapacityUsed");
        }

        public static void MountUnitToAirliftCarrier(this AbstractActor carrier, AbstractActor targetUnit,
            bool shrinkRepForInternal)
        {
            if (targetUnit is Mech targetMech)
            {
                ModState.PositionLockAirlift.Add(targetMech.GUID, carrier.GUID);
                
                if (shrinkRepForInternal)
                {
                    //var baseScale = battleArmor.GameRep.transform.localScale;
                    //ModState.SavedBAScale.Add(battleArmor.GUID, baseScale);
                    targetMech.GameRep.transform.localScale = new Vector3(.01f, .01f, .01f);
                    targetMech.GameRep.ToggleHeadlights(false);
                }

                if (carrier.team.IsFriendly(targetMech.team))
                {
                    var availableCapacity = carrier.getAvailableInternalLiftCapacity();

                    if (ModInit.modSettings.AirliftCapacityByTonnage)
                    {
                        var unitTonnage = Mathf.RoundToInt(targetMech.tonnage);

                        if (availableCapacity >= unitTonnage)
                        {
                            ModInit.modLog.LogMessage(
                                $"[MountUnitToAirliftCarrier] - target unit {carrier.DisplayName} has available internal lift capacity of {availableCapacity}; mounting {targetMech.DisplayName} internally.");
                            carrier.modifyAvailableInternalLiftCapacity(unitTonnage);
                            tracker.IsSquadInternal = true;
                            // try and set firing arc to 360?
                            battleArmor.FiringArc(360f);
                            return;
                        }
                    }
                    else
                    {
                        if (availableCapacity > 0)
                        {
                            ModInit.modLog.LogMessage(
                                $"[MountUnitToAirliftCarrier] - target unit {carrier.DisplayName} has internal BA capacity of {internalCap}. Currently used: {currentInternalUsed}, mounting squad internally.");
                            carrier.modifyInternalBASquads(1);
                            tracker.IsSquadInternal = true;
                            // try and set firing arc to 360?
                            battleArmor.FiringArc(360f);
                            return;
                        }
                    }

                    carrier.setHasExternalMountedBattleArmor(true);
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

            ModState.PositionLockAirlift.Remove(actor.GUID);
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

        public class DetachFromCarrierDelegate
        {
            public AbstractActor Actor{ get; set; }
            public CustomMech Carrier{ get; set; }
            public float CarrierDownSpeed = -20f;
            public float CarrierUpSpeed = 5f;

            public DetachFromCarrierDelegate(AbstractActor actor, CustomMech carrier)
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

        internal class AttachToCarrierDelegate
        {
            public AbstractActor Actor { get; set; }
            public CustomMech Carrier { get; set; }
            public float CarrierDownSpeed = -20f;
            public float CarrierUpSpeed = 5f;

            public AttachToCarrierDelegate(AbstractActor actor, CustomMech carrier)
            {
                this.Actor = actor;
                this.Carrier = carrier;
            }

            public void OnLandAttach()
            {
                //HIDE SQUAD REPRESENTATION
                //attachTarget.MountBattleArmorToChassis(squad, true);
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

        public static void AttachToAirliftCarrier(this AbstractActor actor, AbstractActor carrier)
        {
            if (carrier is CustomMech custMech)
            {
                if (custMech.FlyingHeight() > 1.5f)
                {
                    //Check if actually flying unit
                    //CALL ATTACH CODE BUT WITHOUT SQUAD REPRESENTATION HIDING
                    //custMech.MountBattleArmorToChassis(squad, false);

                    custMech.custGameRep.HeightController.UpSpeed = 50f;
                    custMech.custGameRep.HeightController.DownSpeed = -50f;

                    var attachDel = new AttachToCarrierDelegate(actor, custMech);
                    custMech.DropOffAnimation(attachDel.OnLandAttach, attachDel.OnRestoreHeightControl);
                }
            }
            else
            {
                ModInit.modLog.LogTrace($"AttachToCarrier call mount.");
                //CALL DEFAULT ATTACH CODE
                //custMech.MountBattleArmorToChassis(squad, true);
            }
        }

        public static void DetachFromAirliftCarrier(this AbstractActor squad, AbstractActor carrier)
        {
            if (carrier is CustomMech custMech)
            {
                if (custMech.FlyingHeight() > 1.5f)
                {
                    //Check if actually flying unit
                    //CALL ATTACH CODE BUT WITHOUT SQUAD REPRESENTATION HIDING
                    //custMech.DismountBA(squad, false, false, false);
                    custMech.custGameRep.HeightController.UpSpeed = 50f;
                    custMech.custGameRep.HeightController.DownSpeed = -50f;

                    var detachDel = new DetachFromCarrierDelegate(squad, custMech);
                    custMech.DropOffAnimation(detachDel.OnLandDetach, detachDel.OnRestoreHeightControl);
                }
            }
            else
            {
                ModInit.modLog.LogTrace($"DetachFromCarrier call dismount.");
                //CALL DEFAULT ATTACH CODE
                
                //squad.DismountBA(carrier, Vector3.zero, false, false, true);
            }
        }
    }
}
