using System;
using System.Collections.Generic;
using System.Linq;
using BattleTech;
using BattleTech.UI;
using CustAmmoCategories;
using CustAmmoCategoriesPatches;
using CustomAmmoCategoriesPatches;
using Harmony;
using HBS.Math;
using HBS.Util;
using IRBTModUtils;
using UnityEngine;

namespace StrategicOperations.Framework
{
    public class TB_StrafeSequence : MultiSequence
    {
        public string ParentSequenceID;
        public string TurnEventID;
        private Team StrafingTeam;
        private List<ICombatant> CurrentTargets { get; set; }
        private List<string> AllTargetGUIDs { get; set; }
        private AbstractActor Attacker { get; set; }
        private Vector3 EndPos { get; set; }
        private float HeightOffset { get; set; }
        public override bool IsCancelable => false;
        public override bool IsComplete => this._state == TB_StrafeSequence.SequenceState.Finished;
        public override bool IsParallelInterruptable => false;
        public bool IsValidMultisequenceChild => false;
        private float MaxWeaponRange { get; set; }
        private float Radius { get; set; }
        private Vector3 StartPos { get; set; }
        private float StrafeLength { get; set; }
//        private List<Weapon> StrafeWeapons { get; set; }
        private Vector3 Velocity { get; set; }
        private const float HorizMultiplier = 4f;
//        private float speed = 150f;
        private TB_StrafeSequence.SequenceState _state;
        private const float TimeBetweenAttacks = 0.35f;
        private const float TimeIncoming = 6f;
        private float _timeInCurrentState;
        private float _timeSinceLastAttack;
        private Vector3 _zeroEndPos;
        private Vector3 _zeroStartPos;
        public List<int> attackSequences = new List<int>();
        public bool IsStrafeAOE;
        public int AOECount;
        public List<Vector3> AOEPositions = new List<Vector3>();
        public CombatHUD HUD;

        public enum SequenceState
        {
            None,
            Incoming,
            Strafing,
            Finished
        }

        public TB_StrafeSequence(string parentSequenceID, string turnEventID, AbstractActor attacker, Vector3 positionA, Vector3 positionB,
            float radius, Team team, bool isStrafeAOE, int strafeAOECount = 0) : base(attacker.Combat)
        {
            this.ParentSequenceID = parentSequenceID;
            this.TurnEventID = turnEventID;
            this.Attacker = attacker;
            this.StartPos = positionA;
            this.EndPos = positionB;
            this.StrafeLength = Mathf.Max(1f, Vector3.Distance(positionA, positionB)); 
            this.Radius = radius;
            this.StrafingTeam = team;
            this._state = TB_StrafeSequence.SequenceState.None;
            this.IsStrafeAOE = isStrafeAOE; //do thing
            this.AllTargetGUIDs = new List<string>();
            this.AOECount = strafeAOECount;
        }

        private void AttackNextTargets()
        {
            this._timeSinceLastAttack += Time.deltaTime;
            if (this._timeSinceLastAttack > ModInit.modSettings.timeBetweenAttacks)
            {
                if (!base.Combat.AttackDirector.IsAnyAttackSequenceActive)
                {
                    if (IsStrafeAOE && this.HUD != null)
                    {
                        ModInit.modLog?.Info?.Write($"Incoming AOE Attack");
                        if (AOEPositions.Count > 0)
                        {
                            ModInit.modLog?.Info?.Write($"{AOEPositions.Count} attack points for AOE remain, creating delegate and performing terrain attack at point {this.AOEPositions[0]}");
                            Vector3 collisionWorldPos;
                            var LOFLevel = this.Attacker.Combat.LOFCache.GetLineOfFire(this.Attacker, this.Attacker.CurrentPosition, this.Attacker, this.AOEPositions[0], this.Attacker.CurrentRotation, out collisionWorldPos);
                            Attacker.addTerrainHitPosition(this.AOEPositions[0], LOFLevel < LineOfFireLevel.LOFObstructed);
                            
                            AttackInvocation invocation = new AttackInvocation(this.Attacker, this.Attacker, this.Attacker.Weapons, MeleeAttackType.NotSet, 0);

                            ReceiveMessageCenterMessage subscriber = delegate (MessageCenterMessage message)
                            {
                                //base.Orders = (message as AddSequenceToStackMessage).sequence;
                            };
                            base.Combat.MessageCenter.AddSubscriber(MessageCenterMessageType.AddSequenceToStackMessage, subscriber);

                            Combat.AttackDirector.isSequenceIsTerrainAttack(true);
                            base.Combat.MessageCenter.PublishMessage(invocation);
                            base.Combat.MessageCenter.RemoveSubscriber(MessageCenterMessageType.AddSequenceToStackMessage, subscriber);

                            //var aoeDeligate = new TerrainAttackDeligate(Attacker, this.HUD, LineOfFireLevel.LOFClear, Attacker, this.AOEPositions[0], this.StrafeWeapons);
                            //aoeDeligate.PerformAttackStrafe(this);
                            this.AOEPositions.RemoveAt(0); //sorta working? shitloads of NREs from bulletimpacteffect for some reason. wtf.
                        }
                        return;
                    }
                    if (this.CurrentTargets.Count < 1)
                    {
                        ModInit.modLog?.Trace?.Write(
                            $"We have {this.CurrentTargets.Count} 0 targets remaining, probably shouldn't be calling AttackNextTarget anymore.");
                        this.SetState(TB_StrafeSequence.SequenceState.Finished);
                        return;
                    }

                    for (var i = this.CurrentTargets.Count - 1; i >= 0; i--)
                    {
                        var target = this.CurrentTargets[i];
                        if (!target.IsOperational)
                        {
                            CurrentTargets.RemoveAt(i);
                            continue;
                        }
                        var targetDist = Vector3.Distance(this.Attacker.CurrentPosition,
                            this.CurrentTargets[i].CurrentPosition);
                        ModInit.modLog?.Info?.Write(
                            $"Strafing unit {Attacker.DisplayName} is {targetDist}m from {target.DisplayName}, at loc {this.Attacker.CurrentPosition}. {base.Combat.MapMetaData.GetLerpedHeightAt(this.Attacker.CurrentPosition, false)} above map");
                        if (targetDist <= this.MaxWeaponRange)
                        {
                            var filteredWeapons =
                                new List<Weapon>();
                            foreach (var weapon in this.Attacker.Weapons)
                            {
                                if (this.Attacker.HasLOFToTargetUnit(target, weapon) &&
                                    weapon.MaxRange > targetDist)
                                {
                                    weapon.EnableWeapon();
                                    weapon.ResetWeapon();
                                    filteredWeapons.Add(weapon);
                                    ModInit.modLog?.Info?.Write(
                                        $"weapon {weapon.Name} has LOF and range");
                                }
                            }

                            if (filteredWeapons.Count == 0)
                            {
                                ModInit.modLog?.Info?.Write(
                                    $"No weapons had LOF and range.");
                                continue;
                            }

                            ModInit.modLog?.Info?.Write(
                                $"Strafing unit {Attacker.DisplayName} attacking target {target.DisplayName} from range {targetDist}");

                            //var attackStackSequence = new AttackStackSequence(Attacker, target, Attacker.CurrentPosition,
                            //Attacker.CurrentRotation, filteredWeapons, MeleeAttackType.NotSet, 0, -1);
                            //Attacker.Combat.MessageCenter.PublishMessage(new AddSequenceToStackMessage(attackStackSequence));

                            if (false) //disable maybe broken one? why does it not prefire and complete...
                            {

                                AttackDirector attackDirector = base.Combat.AttackDirector;
                                AttackDirector.AttackSequence attackSequence = attackDirector.CreateAttackSequence(
                                    base.SequenceGUID, this.Attacker, target, this.Attacker.CurrentPosition,
                                    this.Attacker.CurrentRotation, 0, filteredWeapons,
                                    MeleeAttackType.NotSet, 0, false);
                                this.attackSequences.Add(attackSequence.id);
                                attackDirector.PerformAttack(attackSequence);
                                //attackSequence.ResetWeapons();
                            }

                            if (true) //this processes correctly, but pauses animations.
                            {
                                var invocation = new AttackInvocation(this.Attacker, target, filteredWeapons, MeleeAttackType.NotSet, 0);
                                base.Combat.MessageCenter.PublishMessage(invocation);
                            }

                            this.AllTargetGUIDs.Add(target.GUID);
                            this.CurrentTargets.RemoveAt(i);
                            this._timeSinceLastAttack = 0f;
                            continue;
                        }

                        ModInit.modLog?.Info?.Write(
                            $"Attacker {Attacker.DisplayName} range to target {CurrentTargets[0].DisplayName} {targetDist} >= maxWeaponRange {this.MaxWeaponRange}");
//                    this.AllTargets.RemoveAt(0);
                    }

                    ModInit.modLog?.Debug?.Write(
                        $"We have {this.CurrentTargets.Count} targets remaining, none that we can attack.");
                }
                ModInit.modLog?.Debug?.Write($"There is already an attack sequence active, so we're not doing anything?");
            }
//            ModInit.modLog?.Info?.Write($"timeSinceAttack was {this.timeSinceLastAttack} (needs to be > {timeBetweenAttacks}) and IsAnyAttackSequenceActive?: {base.Combat.AttackDirector.IsAnyAttackSequenceActive} should be false");
        }
        private Vector3 CalcStartPos()
        {
            this.MaxWeaponRange = 400;
            if (this.Attacker.Weapons.Count != 0)
            {
                this.MaxWeaponRange = this.Attacker.Weapons.First().MaxRange;
            }
            Vector3 result = this.StartPos - this.Velocity * ModInit.modSettings.strafePreDistanceMult;
            this.HeightOffset = Mathf.Clamp(this.MaxWeaponRange/4, ModInit.modSettings.strafeAltitudeMin,
                ModInit.modSettings.strafeAltitudeMax);
            result.y = base.Combat.MapMetaData.GetLerpedHeightAt(result, false) + this.HeightOffset;
            return result;
        }

        private void CalcTargets()
        {
            if (IsStrafeAOE)
            {
                ModInit.modLog?.Info?.Write($"Calculating impact points for AOE Strafe.");
                this.HUD = SharedState.CombatHUD;
                //this.HUD = Traverse.Create(CameraControl.Instance).Property("HUD").GetValue<CombatHUD>();
                Vector3 b = (this.EndPos - StartPos) / Math.Max(this.AOECount - 1, 1);
                Vector3 vector = StartPos;
                vector.y = base.Combat.MapMetaData.GetLerpedHeightAt(vector, false);
                ModInit.modLog?.Info?.Write($"Added impact point {vector}");
                AOEPositions.Add(vector);
                //Utils.SpawnDebugFlare(vector, "vfxPrfPrtl_artillerySmokeSignal_loop", 3);
                for (int i = 0; i < this.AOECount-1; i++)
                {
                    vector += b;
                    vector.y = base.Combat.MapMetaData.GetLerpedHeightAt(vector, false);
                    ModInit.modLog?.Info?.Write($"Added impact point {vector}");
                    AOEPositions.Add(vector);
                    //Utils.SpawnDebugFlare(vector,"vfxPrfPrtl_artillerySmokeSignal_loop", 3);
                }

                return;
            }

            this.CurrentTargets = new List<ICombatant>();

            var allCombatants = new List<ICombatant>(base.Combat.GetAllLivingCombatants());

            if (ModInit.modSettings.strafeTargetsFriendliesChance == 0 && ModInit.modSettings.strafeNeutralBuildingsChance == 0)
            {
                allCombatants = new List<ICombatant>(allCombatants.Where(x=>x.team.IsEnemy(this.StrafingTeam)));
            }
            allCombatants.RemoveAll(x => x.GUID == this.Attacker.GUID || !x.IsOperational);
            var cancelChanceFromAA = 0f;
            for (int i = 0; i < allCombatants.Count; i++)
            {
                if (allCombatants[i] is AbstractActor actor)
                {
                    if (actor.WasDespawned || actor.WasEjected)
                    {
                        continue;
                    }
                }

                if (allCombatants[i] is BattleTech.Building building && !building.isDropshipNotLanded())
                {
                    var rollBuilding = ModInit.Random.NextDouble();
                    var isObjective = Traverse.Create(building).Field("isObjectiveTarget").GetValue<bool>();
                    if (isObjective)
                    {
                        var chanceBuilding = ModInit.modSettings.strafeObjectiveBuildingsChance;
                        if (rollBuilding >= chanceBuilding)
                        {
                            ModInit.modLog?.Info?.Write($"Roll {rollBuilding} >= strafeObjectiveBuildingsChance {chanceBuilding}, skipping.");
                            continue;
                        }
                    }
                    else if (building.team.GUID == "421027ec-8480-4cc6-bf01-369f84a22012") //only need to check for "World" since friendly buildings will be covered below, and we're ok targeting enemy buildings
                    {
                        var chanceBuilding = ModInit.modSettings.strafeNeutralBuildingsChance;
                        if (rollBuilding >= chanceBuilding && allCombatants[i].team.IsNeutral(this.StrafingTeam))
                        {
                            ModInit.modLog?.Info?.Write($"Roll {rollBuilding} >= strafeNeutralBuildingsChance {chanceBuilding}, skipping.");
                            continue;
                        }
                    }
                }

                var roll = ModInit.Random.NextDouble();
                var chance = ModInit.modSettings.strafeTargetsFriendliesChance;
                if (roll >= chance && allCombatants[i].team.IsFriendly(this.StrafingTeam))
                {
                    ModInit.modLog?.Info?.Write($"Roll {roll} >= chance {chance}, skipping.");
                    continue;
                }

                if (allCombatants[i].team.IsEnemy(this.StrafingTeam))
                {
                    if (this.StrafingTeam.IsLocalPlayer)
                    {
                        cancelChanceFromAA = ModState.cancelChanceForPlayerStrafe;
                    }
                    else if (cancelChanceFromAA == 0f)
                    {
                        cancelChanceFromAA = allCombatants[i].GetAvoidStrafeChanceForTeam();
                    }

                    if (roll <= cancelChanceFromAA)
                    {
                        ModInit.modLog?.Info?.Write(
                            $"Roll {roll} <= cancelChanceFromAA {cancelChanceFromAA}, skipping.");
                        continue;
                    }
                    ModInit.modLog?.Info?.Write($"Roll {roll} > cancelChanceFromAA {cancelChanceFromAA}, not skipping.");
                }

                if (this.IsTarget(allCombatants[i]))
                {
                    this.CurrentTargets.Add(allCombatants[i]);
                    ModInit.modLog?.Info?.Write($"Added target {allCombatants[i].DisplayName}: {allCombatants[i].GUID} to final target list.");
                }
            }
            Vector3 preStartPos = this.EndPos - this.StartPos * 2f;
            this.CurrentTargets.Sort((ICombatant x, ICombatant y) => Vector3.Distance(x.CurrentPosition, preStartPos).CompareTo(Vector3.Distance(y.CurrentPosition, preStartPos)));
        }
        private void GetWeaponsForStrafe()
        {
            //this.StrafeWeapons = this.Attacker.Weapons; //new List<Weapon>(this.Attacker.Weapons);
            if (this.Attacker.Weapons.Count == 0)
            {
                ModInit.modLog?.Info?.Write($"No weapons found for strafing run.");
                return;
            }
            this.Attacker.Weapons.Sort((Weapon x, Weapon y) => x.MaxRange.CompareTo(y.MaxRange));
            ModInit.modLog?.Info?.Write($"First strafe weapon will be {Attacker.Weapons[0].Name} with range {Attacker.Weapons[0].MaxRange}");
        }

        //maybe need to refresh weapons on attacker instead of in sequence?
        private bool IsTarget(ICombatant combatant)
        {
            Vector3 currentPosition = combatant.CurrentPosition;
            Vector3 vector = NvMath.NearestPointStrict(this.StartPos, this.EndPos, currentPosition);
            vector.y = base.Combat.MapMetaData.GetLerpedHeightAt(vector, false);
            var dist = Vector3.Distance(vector, currentPosition);
            if (dist < this.Radius)
            {
                ModInit.modLog?.Info?.Write($"Target {combatant.DisplayName} is within strafe radius range. Distance: {dist}, Range: {this.Radius}");
                return true;
            }
            ModInit.modLog?.Debug?.Write($"Target {combatant.DisplayName} not within strafe radius range. Distance: {dist}, Range: {this.Radius}");
            return false;
        }
        public override void Load(SerializationStream stream)
        {
        }
        public override void LoadComplete()
        {
        }
        public override void OnAdded()
        {
            base.OnAdded();

            this.SetState(TB_StrafeSequence.SequenceState.Incoming);
        }

        public override void OnComplete()
        {
            base.OnComplete();

            ModState.DeferredDespawnersFromStrafe.Add(Attacker.GUID, Attacker);
            //this.Attacker.PlaceFarAwayFromMap();

            foreach (var idx in this.attackSequences)
            {
                base.Combat.AttackDirector.RemoveAttackSequence(idx);
            }

            foreach (var targetID in this.AllTargetGUIDs)
            {
                var targetCombatant = base.Combat.FindCombatantByGUID(targetID, false);
                if (targetCombatant != null)
                {
                    targetCombatant.HandleDeath(Attacker.GUID);
                    if (!targetCombatant.IsDead)
                    {
                        if (targetCombatant is AbstractActor targetActor)
                        {
                            targetActor.CheckForInstability();
                            targetActor.HandleKnockdown(base.RootSequenceGUID, Attacker.GUID, Vector2.one, null);
                        }
                    }
                }
            }
            Utils.NotifyStrafeSequenceComplete(this.ParentSequenceID, this.TurnEventID);
        }

        public override void OnUpdate()
        {
            base.OnUpdate();
            this.Update();
        }
        public override void Save(SerializationStream stream)
        {
        }
        private void SetPosition(Vector3 position, Quaternion rotation)
        {
            this.Attacker.GameRep.thisTransform.position = position;
            this.Attacker.GameRep.thisTransform.rotation = rotation;
            this.Attacker.OnPositionUpdate(position, rotation, base.SequenceGUID, false, null, true);
        }
        private void SetState(TB_StrafeSequence.SequenceState newState)
        {
            if (this._state == newState)
            {
                return;
            }
            this._state = newState;
            this._timeInCurrentState = 0f;
            switch (newState)
            {
                case TB_StrafeSequence.SequenceState.Incoming:
                {
                    this._zeroStartPos = this.StartPos;
                    this._zeroStartPos.y = 0f;
                    this._zeroEndPos = this.EndPos;
                    this._zeroEndPos.y = 0f;
                    this.CalcTargets();
                    this.GetWeaponsForStrafe();
                    Vector3 vector = this._zeroEndPos - this._zeroStartPos;
                    vector.Normalize();
                    var speed = ModInit.modSettings.strafeVelocityDefault;;
                    if (Attacker.MaxSpeed > 0)
                    {
                        speed = Attacker.MaxSpeed;
                    }
                    this.Velocity = vector * speed;
                    Vector3 position = this.CalcStartPos();
                    Quaternion rotation = Quaternion.LookRotation(vector);
                    Quaternion rotation2 = Quaternion.LookRotation(Vector3.forward * 5f + Vector3.down * 1f);
                    this.SetPosition(position, rotation);
                    base.ClearCamera();
                        if (ModInit.modSettings.showStrafeCamera) base.SetCamera(CameraControl.Instance.ShowActorCam(this.Attacker, rotation2, 300f), base.MessageIndex);
                    return;
                }
                case TB_StrafeSequence.SequenceState.Strafing:
                    base.ClearCamera();
                    if (this.Attacker.team.LocalPlayerControlsTeam)
                    {
//                        Quaternion rotation2 = Quaternion.LookRotation(Vector3.forward * 5f + Vector3.down * 1f);
//                        base.SetCamera(CameraControl.Instance.ShowActorCam(this.Attacker, rotation2, 300f), base.MessageIndex);
                        AudioEventManager.PlayRandomPilotVO(VOEvents.AirstrikeLaunched_Ally, base.Combat.LocalPlayerTeam, base.Combat.LocalPlayerTeam.units);
                        return;
                    }
                    AudioEventManager.PlayRandomPilotVO(VOEvents.AirstrikeLaunched_Enemy, base.Combat.LocalPlayerTeam, base.Combat.LocalPlayerTeam.units);
                    return;
                case TB_StrafeSequence.SequenceState.Finished:
                {
                    TB_FlyAwaySequence sequence = new TB_FlyAwaySequence(this.Attacker, this.Velocity, 150f);
                    base.Combat.MessageCenter.PublishMessage(new AddParallelSequenceToStackMessage(sequence));
                    return;
                }
                default:
                    return;
            }
        }
        public override bool ShouldSave()
        {
            return false;
        }
        public override int Size()
        {
            return 0;
        }
        private void Update()
        {
            this._timeInCurrentState += Time.deltaTime;
            var curPos_2D = this.Attacker.CurrentPosition;
            curPos_2D.y = 0f;
            var endPos_2D = this.EndPos;
            endPos_2D.y = 0f;
            var startPos_2D = this.StartPos;
            startPos_2D.y = 0f;
            var fromEnd = Vector3.Distance(curPos_2D, endPos_2D);
            var fromStart = Vector3.Distance(curPos_2D, startPos_2D);
            switch (this._state)
            {
                case TB_StrafeSequence.SequenceState.Incoming:
                    if (fromStart < this.MaxWeaponRange) //set some kind of safety in case of weapon range fuckup.
                    {
                        ModInit.modLog?.Info?.Write($"Setting Strafe SequenceState to Strafing!");
                        this.SetState(TB_StrafeSequence.SequenceState.Strafing);
                    }
                    break;
                case TB_StrafeSequence.SequenceState.Strafing:
                    //var endpoint = this.StartPos + (this.EndPos - this.StartPos).normalized * Vector3.Distance(this.StartPos, this.EndPos);
                    //var angle = Vector3.Angle(this.EndPos, this.Attacker.CurrentPosition);
                    ModInit.modLog?.Debug?.Write($"Strafing unit {fromEnd}m in 2D space from endpoint!");// Angle to endPoint: {angle}");
                    if (fromEnd < ModInit.modSettings.strafeMinDistanceToEnd || (fromEnd <= fromStart && fromEnd > this.StrafeLength))
                    {
                        ModInit.modLog?.Info?.Write($"Setting Strafe SequenceState to Finished!");
                        this.SetState(TB_StrafeSequence.SequenceState.Finished);
                    }
                    break;
            }
            switch (this._state)
            {
                case TB_StrafeSequence.SequenceState.Incoming:
                    var pos2 = this.Attacker.CurrentPosition + this.Velocity * Time.deltaTime;
                    var terrainHeight = this.Combat.MapMetaData.GetLerpedHeightAt(pos2, false);
                    if (pos2.y < terrainHeight)
                    {
                        ModInit.modLog?.Debug?.Write($"We're going under the map, should be fixing.");
                    }
                    if (pos2.y - terrainHeight < this.HeightOffset)
                    {
                        pos2.y = terrainHeight + this.HeightOffset; //add gate here so it doesnt continue up forever?
                    }
                    this.SetPosition(pos2, this.Attacker.CurrentRotation);
                    if (true)// switched this? should give AI visibility too i guess (StrafingTeam.IsLocalPlayer)
                    {
                        foreach (var enemy in StrafingTeam.GetAllEnemies())
                        {
                            Vector3 vector = pos2 - enemy.CurrentPosition;
                            vector.y = 0f;
                            ModInit.modLog?.Debug?.Write(
                                $"{enemy.Description.UIName} is {vector.magnitude} from strafing unit for. Unit has sensor range of {base.Combat.LOS.GetSensorRange(Attacker)}!");
                            if (vector.magnitude < ModInit.modSettings.strafeSensorFactor *
                                base.Combat.LOS.GetSensorRange(Attacker))
                            {
                                ModInit.modLog?.Debug?.Write($"Should be showing enemy!");
                                var rep = enemy.GameRep as PilotableActorRepresentation;
                                if (rep != null && !rep.VisibleToPlayer &&
                                    enemy.VisibilityToTargetUnit(StrafingTeam.units.FirstOrDefault(x => !x.IsDead)) <
                                    VisibilityLevel.Blip0Minimum)
                                {
                                    ModInit.modLog?.Debug?.Write($"Game Rep is not null!");
//                                rep.OnPlayerVisibilityChanged(VisibilityLevel.Blip0Minimum);
                                    rep.SetForcedPlayerVisibilityLevel(VisibilityLevel.Blip0Minimum);
                                }
                            }
                        }
                    }
                    return;
                case TB_StrafeSequence.SequenceState.Strafing:
                    var pos3 = this.Attacker.CurrentPosition + this.Velocity * Time.deltaTime;
                    // maybe try to smooth out altitude changes here. but i dont really care.
                    var terrainHeight2 = this.Combat.MapMetaData.GetLerpedHeightAt(pos3, false);
                    if (pos3.y < terrainHeight2)
                    {
                        ModInit.modLog?.Debug?.Write($"We're going under the map, should be fixing.");
                    }
                    if (pos3.y - terrainHeight2 < this.HeightOffset)
                    {
                        pos3.y = terrainHeight2 + this.HeightOffset;
                    }
                    this.SetPosition(pos3, this.Attacker.CurrentRotation);
                    this.AttackNextTargets();
                    break;
                case TB_StrafeSequence.SequenceState.Finished:
                    break;
                default:
                    return;
            }
        }
    }
}
