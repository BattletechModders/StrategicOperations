using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
        public static bool HasAirliftedUnits(this AbstractActor actor)
        {
            return ModState.AirliftTrackers.Any(x => x.Value.CarrierGUID == actor.GUID);
        }
        public static bool IsAirlifted(this AbstractActor actor)
        {
            return ModState.AirliftTrackers.ContainsKey(actor.GUID);
        }
        public static bool IsAirliftedFriendly(this AbstractActor actor)
        {
            return ModState.AirliftTrackers.ContainsKey(actor.GUID) && ModState.AirliftTrackers[actor.GUID].IsFriendly;
        }

        public static bool IsAirliftingTargetUnit(this AbstractActor actor, AbstractActor targetActor)
        {
            return ModState.AirliftTrackers.ContainsKey(targetActor.GUID) && ModState.AirliftTrackers[targetActor.GUID].CarrierGUID == actor.GUID;
        }

        public static bool IsAirliftedByTarget(AbstractActor actor, AbstractActor targetActor)
        {
            return ModState.AirliftTrackers.ContainsKey(actor.GUID) &&
                   ModState.AirliftTrackers[actor.GUID].CarrierGUID == targetActor.GUID;
        }

        public static bool HasAirliftedFriendly(this AbstractActor actor)
        {
            return ModState.AirliftTrackers.Any(x => x.Value.IsFriendly && x.Value.CarrierGUID == actor.GUID);
        }

        public static bool IsAirliftedEnemy(this AbstractActor actor)
        {
            return ModState.AirliftTrackers.ContainsKey(actor.GUID) && !ModState.AirliftTrackers[actor.GUID].IsFriendly;
        }

        public static bool HasAirliftedEnemy(this AbstractActor actor)
        {
            return ModState.AirliftTrackers.Any(x => !x.Value.IsFriendly && x.Value.CarrierGUID == actor.GUID);
        }
        
        public static bool getCanAirliftHostiles(this AbstractActor actor)
        {
            return actor.StatCollection.GetValue<bool>("CanAirliftHostiles");
        }

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

        public static bool getHasAvailableInternalLiftCapacityForTarget(this AbstractActor actor, AbstractActor targetActor)
        {
            if (ModInit.modSettings.AirliftCapacityByTonnage)
            {
                if (targetActor is Mech targetMech)
                {
                   return actor.StatCollection.GetValue<int>("InternalLiftCapacity") -
                            actor.StatCollection.GetValue<int>("InternalLiftCapacityUsed") >= targetMech.tonnage;
                }
                else if (targetActor is Vehicle vehicle)
                {
                    
                    return actor.StatCollection.GetValue<int>("InternalLiftCapacity") -
                            actor.StatCollection.GetValue<int>("InternalLiftCapacityUsed") >= vehicle.tonnage;
                }
            }
            return actor.StatCollection.GetValue<int>("InternalLiftCapacity") -
                actor.StatCollection.GetValue<int>("InternalLiftCapacityUsed") >= 1;
        }

        public static bool getHasAvailableExternalLiftCapacityForTarget(this AbstractActor actor, AbstractActor targetActor)
        {
            if (ModInit.modSettings.AirliftCapacityByTonnage)
            {
                if (targetActor is Mech targetMech)
                {
                    return actor.StatCollection.GetValue<int>("ExternalLiftCapacity") -
                        actor.StatCollection.GetValue<int>("ExternalLiftCapacityUsed") >= Mathf.RoundToInt(targetMech.tonnage);
                }
                else if (targetActor is Vehicle vehicle)
                {

                    return actor.StatCollection.GetValue<int>("ExternalLiftCapacity") -
                        actor.StatCollection.GetValue<int>("ExternalLiftCapacityUsed") >= Mathf.RoundToInt(vehicle.tonnage);
                }
            }
            return actor.StatCollection.GetValue<int>("ExternalLiftCapacity") -
                actor.StatCollection.GetValue<int>("ExternalLiftCapacityUsed") >= 1;
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

        public static float GetVerticalOffsetForExternalMount(this AbstractActor targetUnit)
        {

            return targetUnit.HighestLOSPosition.y * .7f; //TODO see if i can dynamagically calculate offset using unity intersects?
        }

        public static List<AbstractActor> GetAirliftedUnits(this AbstractActor carrier)
        {
            var results = new List<AbstractActor>();
            var trackers = ModState.AirliftTrackers.Where(x => x.Value.CarrierGUID == carrier.GUID);
            foreach (var tracker in trackers)
            {
                results.Add(carrier.Combat.FindActorByGUID(tracker.Key));
            }
            return results;
        }
        public static void HandleTurretFallingDamage(this Turret turret)
        {
            var dmg = turret.TurretDef.Chassis.Tonnage;

            var fallingWeapon = new Weapon(turret, turret.Combat, turret.TurretDef.Inventory.First(), "TurretFallingFakeWeapon");
            // Initialize a game representation to prevent warnings in CAC logs
            fallingWeapon.Init();
            fallingWeapon.InitStats();
            fallingWeapon.InitGameRep(fallingWeapon.baseComponentRef.prefabName, turret.GameRep.TurretAttach, turret.LogDisplayName);

            var hitinfo = new WeaponHitInfo(-1, -1, 0, 0, turret.GUID,
                turret.GUID, 1, new float[1], new float[1], new float[1],
                new bool[1], new int[1], new int[1], new AttackImpactQuality[1],
                new AttackDirection[1], new Vector3[1], new string[1], new int[1]);

            turret.TakeWeaponDamage(hitinfo, 1,
                fallingWeapon, dmg,
                0, 0, DamageType.DFASelf);
        }

        public static void MountUnitToAirliftCarrier(this AbstractActor carrier, AbstractActor targetUnit, bool isFriendly)
        {
            if (targetUnit is Mech targetMech) //handle turret also below this
            {
                foreach (var airliftEffect in ModState.AirliftEffects)
                {
                    if (airliftEffect.FriendlyAirlift && isFriendly)
                    {
                        foreach (var effectData in airliftEffect.effects)
                        {
                            targetMech.Combat.EffectManager.CreateEffect(effectData,
                                effectData.Description.Id,
                                -1, targetUnit, targetUnit, default(WeaponHitInfo), 1);
                        }
                    }
                    else if (!airliftEffect.FriendlyAirlift && !isFriendly)
                    {
                        foreach (var effectData in airliftEffect.effects)
                        {
                            targetMech.Combat.EffectManager.CreateEffect(effectData,
                                effectData.Description.Id,
                                -1, targetUnit, targetUnit, default(WeaponHitInfo), 1);
                        }
                    }
                }
                var availableInternalCapacity = carrier.getAvailableInternalLiftCapacity();
                var availableExternalCapacity = carrier.getAvailableExternalLiftCapacity();
                var unitTonnage = Mathf.RoundToInt(targetMech.tonnage);
                var offset = targetUnit.GetVerticalOffsetForExternalMount();
                if (isFriendly)
                {
                    if (ModInit.modSettings.AirliftCapacityByTonnage)
                    {
                        if (availableInternalCapacity >= unitTonnage)
                        {
                            ModInit.modLog?.Info?.Write(
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
                            ModInit.modLog?.Info?.Write(
                                $"[MountUnitToAirliftCarrier] - target unit {carrier.DisplayName} has available external lift capacity of {availableExternalCapacity}; mounting {targetMech.DisplayName} externally. Offset calculated at {offset}");
                            carrier.modifyUsedExternalLiftCapacity(unitTonnage);
                            ModState.AirliftTrackers.Add(targetMech.GUID, new Classes.AirliftTracker(carrier.GUID, false, true, offset));
                        }
                    }
                    else
                    {
                        if (availableInternalCapacity > 0)
                        {
                            ModInit.modLog?.Info?.Write(
                                $"[MountUnitToAirliftCarrier] - target unit {carrier.DisplayName} has available internal lift capacity of {availableInternalCapacity}; mounting {targetMech.DisplayName} internally.");
                            carrier.modifyUsedInternalLiftCapacity(1);
                            ModState.AirliftTrackers.Add(targetMech.GUID, new Classes.AirliftTracker(carrier.GUID, false, true, 0f));
                            //shrinkadink
                            targetMech.GameRep.transform.localScale = new Vector3(.01f, .01f, .01f);
                            targetMech.GameRep.ToggleHeadlights(false);
                            return;
                        }
                        if (availableExternalCapacity > 0)
                        {
                            ModInit.modLog?.Info?.Write(
                                $"[MountUnitToAirliftCarrier] - target unit {carrier.DisplayName} has available external lift capacity of {availableExternalCapacity}; mounting {targetMech.DisplayName} externally. Offset calculated at {offset}");
                            carrier.modifyUsedExternalLiftCapacity(1);
                            ModState.AirliftTrackers.Add(targetMech.GUID, new Classes.AirliftTracker(carrier.GUID, false, true, offset));
                        }
                    }
                }
                else
                {
                    if (ModInit.modSettings.AirliftCapacityByTonnage)
                    {
                        if (availableExternalCapacity >= unitTonnage)
                        {
                            ModInit.modLog?.Info?.Write(
                                $"[MountUnitToAirliftCarrier] - target unit {carrier.DisplayName} has available external lift capacity of {availableExternalCapacity}; mounting {targetMech.DisplayName} externally. Offset calculated at {offset}");
                            carrier.modifyUsedExternalLiftCapacity(unitTonnage);
                            ModState.AirliftTrackers.Add(targetMech.GUID, new Classes.AirliftTracker(carrier.GUID, false, true, offset));
                        }
                    }
                    else
                    {
                        if (availableExternalCapacity > 0)
                        {
                            ModInit.modLog?.Info?.Write(
                                $"[MountUnitToAirliftCarrier] - target unit {carrier.DisplayName} has available external lift capacity of {availableExternalCapacity}; mounting {targetMech.DisplayName} externally. Offset calculated at {offset}");
                            carrier.modifyUsedExternalLiftCapacity(1);
                            ModState.AirliftTrackers.Add(targetMech.GUID, new Classes.AirliftTracker(carrier.GUID, false, true, offset));
                        }
                    }
                }
            }
            else if (targetUnit is Turret targetTurret) //handle turret also below this
            {
                foreach (var airliftEffect in ModState.AirliftEffects)
                {
                    if (airliftEffect.FriendlyAirlift && isFriendly)
                    {
                        foreach (var effectData in airliftEffect.effects)
                        {
                            targetTurret.Combat.EffectManager.CreateEffect(effectData,
                                effectData.Description.Id,
                                -1, targetUnit, targetUnit, default(WeaponHitInfo), 1);
                        }
                    }
                    else if (!airliftEffect.FriendlyAirlift && !isFriendly)
                    {
                        foreach (var effectData in airliftEffect.effects)
                        {
                            targetTurret.Combat.EffectManager.CreateEffect(effectData,
                                effectData.Description.Id,
                                -1, targetUnit, targetUnit, default(WeaponHitInfo), 1);
                        }
                    }
                }
                var availableInternalCapacity = carrier.getAvailableInternalLiftCapacity();
                var availableExternalCapacity = carrier.getAvailableExternalLiftCapacity();
                var unitTonnage = Mathf.RoundToInt(targetTurret.TurretDef.Chassis.Tonnage);
                var offset = targetUnit.GetVerticalOffsetForExternalMount();
                if (isFriendly)
                {
                    if (ModInit.modSettings.AirliftCapacityByTonnage)
                    {
                        if (availableInternalCapacity >= unitTonnage)
                        {
                            ModInit.modLog?.Info?.Write(
                                $"[MountUnitToAirliftCarrier] - target unit {carrier.DisplayName} has available internal lift capacity of {availableInternalCapacity}; mounting {targetTurret.DisplayName} internally.");
                            carrier.modifyUsedInternalLiftCapacity(unitTonnage);
                            ModState.AirliftTrackers.Add(targetTurret.GUID, new Classes.AirliftTracker(carrier.GUID, true, true, 0f));
                            //shrinkadink
                            targetTurret.GameRep.transform.localScale = new Vector3(.01f, .01f, .01f);
                            return;
                        }
                        if (availableExternalCapacity >= unitTonnage)
                        {
                            ModInit.modLog?.Info?.Write(
                                $"[MountUnitToAirliftCarrier] - target unit {carrier.DisplayName} has available external lift capacity of {availableExternalCapacity}; mounting {targetTurret.DisplayName} externally. Offset calculated at {offset}");
                            carrier.modifyUsedExternalLiftCapacity(unitTonnage);
                            ModState.AirliftTrackers.Add(targetTurret.GUID, new Classes.AirliftTracker(carrier.GUID, false, true, offset));
                        }
                    }
                    else
                    {
                        if (availableInternalCapacity > 0)
                        {
                            ModInit.modLog?.Info?.Write(
                                $"[MountUnitToAirliftCarrier] - target unit {carrier.DisplayName} has available internal lift capacity of {availableInternalCapacity}; mounting {targetTurret.DisplayName} internally.");
                            carrier.modifyUsedInternalLiftCapacity(1);
                            ModState.AirliftTrackers.Add(targetTurret.GUID, new Classes.AirliftTracker(carrier.GUID, false, true, 0f));
                            //shrinkadink
                            targetTurret.GameRep.transform.localScale = new Vector3(.01f, .01f, .01f);
                            return;
                        }
                        if (availableExternalCapacity > 0)
                        {
                            ModInit.modLog?.Info?.Write(
                                $"[MountUnitToAirliftCarrier] - target unit {carrier.DisplayName} has available external lift capacity of {availableExternalCapacity}; mounting {targetTurret.DisplayName} externally. Offset calculated at {offset}");
                            carrier.modifyUsedExternalLiftCapacity(1);
                            ModState.AirliftTrackers.Add(targetTurret.GUID, new Classes.AirliftTracker(carrier.GUID, false, true, offset));
                        }
                    }
                }
                else
                {
                    if (ModInit.modSettings.AirliftCapacityByTonnage)
                    {
                        if (availableExternalCapacity >= unitTonnage)
                        {
                            ModInit.modLog?.Info?.Write(
                                $"[MountUnitToAirliftCarrier] - target unit {carrier.DisplayName} has available external lift capacity of {availableExternalCapacity}; mounting {targetTurret.DisplayName} externally. Offset calculated at {offset}");
                            carrier.modifyUsedExternalLiftCapacity(unitTonnage);
                            ModState.AirliftTrackers.Add(targetTurret.GUID, new Classes.AirliftTracker(carrier.GUID, false, true, offset));
                        }
                    }
                    else
                    {
                        if (availableExternalCapacity > 0)
                        {
                            ModInit.modLog?.Info?.Write(
                                $"[MountUnitToAirliftCarrier] - target unit {carrier.DisplayName} has available external lift capacity of {availableExternalCapacity}; mounting {targetTurret.DisplayName} externally. Offset calculated at {offset}");
                            carrier.modifyUsedExternalLiftCapacity(1);
                            ModState.AirliftTrackers.Add(targetTurret.GUID, new Classes.AirliftTracker(carrier.GUID, false, true, offset));
                        }
                    }
                }
            }
        }

//        public static void ProcessWiggleWiggle(this AbstractActor creator, AbstractActor carrier)
//        {
//            var settings = ModInit.modSettings.AirliftWiggleConfiguration;
//            var roll = ModInit.Random.NextDouble();
//            var successChance = Mathf.Min(settings.BaseSuccessChance +
//                                          (settings.PilotingSuccessFactor * creator.GetPilot().Piloting), settings.MaxSuccessChance);
//            ModInit.modLog?.Info?.Write($"[ProcessWiggleWiggle] {creator.DisplayName} is wiggling with success chance {successChance} vs roll {roll}.");
//            if (roll <= successChance)
//            {
//                creator.DetachFromAirliftCarrier(carrier, false);
//            }
//        }

        public static void DropAirliftedUnit(this AbstractActor carrier, AbstractActor actor, Vector3 locationOverride, bool isHostileDrop, bool calledFromDeswarm = false,
            bool calledFromHandleDeath = false, bool unShrinkRep = true)
        {
            var em = actor.Combat.EffectManager;
            foreach (var airliftEffect in ModState.AirliftEffects)
            {
                foreach (var effectProper in airliftEffect.effects)
                {
                    var effects = em.GetAllEffectsTargetingWithBaseID(actor, effectProper.Description.Id);
                    for (int i = effects.Count - 1; i >= 0; i--)
                    {
                        ModInit.modLog?.Info?.Write(
                            $"[DropAirliftedUnit] Cancelling effect on {actor.DisplayName}: {effects[i].EffectData.Description.Name}.");
                        em.CancelEffect(effects[i]);
                    }
                }
            }

            var hud = Traverse.Create(CameraControl.Instance).Property("HUD").GetValue<CombatHUD>();
            //actor.GameRep.IsTargetable = true;

            if (ModState.AirliftTrackers[actor.GUID].IsCarriedInternal)
            {
                if (ModInit.modSettings.AirliftCapacityByTonnage)
                {
                    var tonnage = 0;
                    if (actor is Mech mech) tonnage = Mathf.RoundToInt(mech.tonnage);
                    else if (actor is Vehicle vehicle) tonnage = Mathf.RoundToInt(vehicle.tonnage);
                    ModInit.modLog?.Info?.Write($"[DropAirliftedUnit] Decrementing {carrier.DisplayName} used internal capacity by tonnage {tonnage}.");
                    carrier.modifyUsedInternalLiftCapacity(-tonnage);
                }
                else
                {
                    ModInit.modLog?.Info?.Write($"[DropAirliftedUnit] Decrementing {carrier.DisplayName} used internal capacity by 1.");
                    carrier.modifyUsedInternalLiftCapacity(-1);
                }
            }
            else
            {
                if (ModInit.modSettings.AirliftCapacityByTonnage)
                {
                    var tonnage = 0;
                    if (actor is Mech mech) tonnage = Mathf.RoundToInt(mech.tonnage);
                    else if (actor is Vehicle vehicle) tonnage = Mathf.RoundToInt(vehicle.tonnage);
                    ModInit.modLog?.Info?.Write($"[DropAirliftedUnit] Decrementing {carrier.DisplayName} used external capacity by tonnage {tonnage}.");
                    carrier.modifyUsedExternalLiftCapacity(-tonnage);
                }
                else
                {
                    ModInit.modLog?.Info?.Write($"[DropAirliftedUnit] Decrementing {carrier.DisplayName} used external capacity by 1.");
                    carrier.modifyUsedExternalLiftCapacity(-1);
                }
            }

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

            if (!isHostileDrop && !calledFromHandleDeath)
            {
                var point = carrier.CurrentPosition;
                if (locationOverride != Vector3.zero)
                {
                    point = locationOverride;
                    ModInit.modLog?.Info?.Write($"[DropAirliftedUnit] Using location override {locationOverride}.");
                }

                point.y = actor.Combat.MapMetaData.GetLerpedHeightAt(point, false);
                actor.TeleportActor(point);
            }
            else
            {
                Vector3 vector = actor.CurrentPosition;
                vector.y = actor.Combat.MapMetaData.GetLerpedHeightAt(actor.CurrentPosition, false);
                if (actor is TrooperSquad squad)
                {
                    squad.HandleBattleArmorFallingDamage();
                }
                else if (actor is Mech mech)
                {
                    MechDisplacementSequence sequence = new MechDisplacementSequence(mech, vector, mech.GameRep.thisTransform.rotation, carrier.GUID);
                    mech.Combat.MessageCenter.PublishMessage(new AddParallelSequenceToStackMessage(sequence));
                    //return; // handle turrets here too to give them damage. maybe also double check that BA will receive proper DFA self damage.
                    //also airlifting turrets doesnt seem to work, so figiure that out
                }
                else if (actor is Turret turret)
                {
                    turret.HandleTurretFallingDamage();
                }
                if (actor.GameRep != null)
                {
                    actor.GameRep.transform.position = vector;
                }
                actor.OnPositionUpdate(vector, actor.CurrentRotation, -1, true, null, false);
            }

            if (!calledFromHandleDeath && !calledFromDeswarm && false) // dont think i need this, since the unit being dropped won't need to reset state ever?
            {
                ModInit.modLog?.Info?.Write(
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
                ModInit.modLog?.Info?.Write(
                    $"[DropAirliftedUnit] Called from handledeath? {calledFromHandleDeath} or Deswarm? {calledFromDeswarm}, forcing end of activation."); // was i trying to end carrier activation maybe?

                var sequence = actor.DoneWithActor();
                actor.Combat.MessageCenter.PublishMessage(new AddSequenceToStackMessage(sequence));
                //actor.OnActivationEnd(actor.GUID, -1);
            }

            //actor.VisibilityCache.UpdateCacheReciprocal(actor.Combat.GetAllLivingCombatants());

            ModInit.modLog?.Info?.Write(
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
                Carrier.DropAirliftedUnit(Actor, Vector3.zero, false, false, false, true);
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
                var offset = Vector3.zero;
                if (Actor.IsAirlifted())
                {
                    offset = Vector3.down * ModState.AirliftTrackers[Actor.GUID].Offset;
                }
                Carrier.custGameRep.HeightController.UpSpeed = CarrierUpSpeed;
                Carrier.custGameRep.HeightController.DownSpeed = CarrierDownSpeed;
                var pos = Carrier.CurrentPosition + offset +
                          Vector3.up * Carrier.custGameRep.HeightController.CurrentHeight;
                Actor.TeleportActor(pos);

                //Actor.GameRep.thisTransform.rotation = Quaternion.identity;
                //Actor.CurrentRotation = Quaternion.identity;
                if (Actor is CustomMech customMech)
                {
                    customMech.custGameRep.j_Root.localRotation = Quaternion.identity;
                }
                Actor.GameRep.thisTransform.rotation = Carrier.GameRep.thisTransform.rotation;
                Actor.CurrentRotation = Carrier.CurrentRotation;
                //var rotate = Quaternion.LookRotation(Carrier.CurrentRotation.eulerAngles);
                //Actor.GameRep.thisTransform.LookAt(rotate.eulerAngles, Vector3.up);
            }
        }

        public static void AttachToAirliftCarrier(this AbstractActor actor, AbstractActor carrier, bool isFriendly)
        {
            if (isFriendly)
            {
                ModInit.modLog?.Trace?.Write($"AttachToAirliftCarrier processing on friendly.");
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
                    ModInit.modLog?.Trace?.Write($"AttachToAirliftCarrier call mount.");
                    //CALL DEFAULT ATTACH CODE
                    carrier.MountUnitToAirliftCarrier(actor, true);
                }
            }
            else
            {
                ModInit.modLog?.Trace?.Write($"AttachToCarrier call mount.");
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
                    ModInit.modLog?.Trace?.Write($"DetachFromAirliftCarrier call DropAirliftedUnit.");
                    //CALL DEFAULT ATTACH CODE
                    carrier.DropAirliftedUnit(actor, Vector3.zero, false, false, false, true);
                }
            }
            else
            {
                ModInit.modLog?.Trace?.Write($"DetachFromAirliftCarrier call DropAirliftedUnit.");
                //CALL DEFAULT ATTACH CODE
                carrier.DropAirliftedUnit(actor, Vector3.zero, true, false, false, true);
                //squad.DismountBA(carrier, Vector3.zero, false, false, true);
            }
        }
    }
}
