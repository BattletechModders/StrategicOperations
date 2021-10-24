using System;
using System.Collections.Generic;
using System.Linq;
using BattleTech;
using BattleTech.UI;
using CustomAmmoCategoriesPatches;
using Harmony;
using HBS.Math;
using HBS.Util;
using UnityEngine;

namespace StrategicOperations.Framework
{
    public class TB_StrafeSequence : MultiSequence
    {
        public string ParentSequenceID;
        public string TurnEventID;
        private Team StrafingTeam;
        private List<ICombatant> AllTargets { get; set; }
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
        private List<Weapon> StrafeWeapons { get; set; }
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
                        ModInit.modLog.LogMessage($"Incoming AOE Attack");
                        if (AOEPositions.Count > 0)
                        {
                            ModInit.modLog.LogMessage($"{AOEPositions.Count} attack points for AOE remain, creating delegate and performing terrain attack at point {this.AOEPositions[0]}");
                            Combat.AttackDirector.isSequenceIsTerrainAttack(true);
                            var aoeDeligate = new TerrainAttackDeligate(Attacker, this.HUD, LineOfFireLevel.LOFClear,
                                Attacker, this.AOEPositions[0], this.StrafeWeapons);
                            aoeDeligate.PerformAttackStrafe(this);
                            this.AOEPositions.RemoveAt(0); //sorta working? shitloads of NREs from bulletimpacteffect for some reason. wtf.
                        }
                        return;
                    }
                    if (this.AllTargets.Count < 1)
                    {
                        ModInit.modLog.LogMessage(
                            $"We have {this.AllTargets.Count} 0 targets remaining, probably shouldn't be calling AttackNextTarget anymore.");
                        this.SetState(TB_StrafeSequence.SequenceState.Finished);
                        return;
                    }

                    for (var i = this.AllTargets.Count - 1; i >= 0; i--)
                    {
                        var target = this.AllTargets[i];
                        if (target.IsDead || target.IsFlaggedForDeath || !target.IsOperational)
                        {
                            AllTargets.RemoveAt(i);
                            continue;
                        }
                        var targetDist = Vector3.Distance(this.Attacker.CurrentPosition,
                            this.AllTargets[i].CurrentPosition);
                        var firingAngle = Vector3.Angle(AllTargets[i].CurrentPosition,
                            this.Attacker.CurrentPosition);
                        ModInit.modLog.LogMessage(
                            $"Strafing unit {Attacker.DisplayName} is {targetDist}m from  {AllTargets[i].DisplayName} and firingAngle is {firingAngle}");
                        if (targetDist <= this.MaxWeaponRange)
                        {
                            var filteredWeapons =
                                new List<Weapon>();
                            foreach (var weapon in this.StrafeWeapons)
                            {
                                if (this.Attacker.HasLOFToTargetUnit(AllTargets[i], weapon) &&
                                    weapon.MaxRange > targetDist)
                                {
                                    weapon.ResetWeapon();
                                    filteredWeapons.Add(weapon);
                                    ModInit.modLog.LogMessage(
                                        $"weapon {weapon.Name} has LOF and range");
                                }
                            }

                            if (filteredWeapons.Count == 0)
                            {
                                ModInit.modLog.LogMessage(
                                    $"No weapons had LOF and range.");
                                continue;
                            }

                            ModInit.modLog.LogMessage(
                                $"Strafing unit {Attacker.DisplayName} attacking target {AllTargets[i].DisplayName} from range {targetDist} and firingAngle {firingAngle}");

                            //var attackStackSequence = new AttackStackSequence(Attacker, this.AllTargets[i], Attacker.CurrentPosition,
                            //Attacker.CurrentRotation, filteredWeapons, MeleeAttackType.NotSet, 0, -1);
                            //Attacker.Combat.MessageCenter.PublishMessage(new AddSequenceToStackMessage(attackStackSequence));

                            AttackDirector attackDirector = base.Combat.AttackDirector;
                            AttackDirector.AttackSequence attackSequence = attackDirector.CreateAttackSequence(
                                base.SequenceGUID, this.Attacker, this.AllTargets[i], this.Attacker.CurrentPosition,
                                this.Attacker.CurrentRotation, 0, filteredWeapons,
                                MeleeAttackType.NotSet, 0, false);
                            this.attackSequences.Add(attackSequence.id);
                            attackDirector.PerformAttack(attackSequence); 
                            attackSequence.ResetWeapons();

                            this.AllTargets.RemoveAt(i);
                            this._timeSinceLastAttack = 0f;
                            continue;
                        }

                        ModInit.modLog.LogMessage(
                            $"Attacker {Attacker.DisplayName} range to target {AllTargets[0].DisplayName} {targetDist} >= maxWeaponRange {this.MaxWeaponRange}");
//                    this.AllTargets.RemoveAt(0);
                    }

                    ModInit.modLog.LogMessage(
                        $"We have {this.AllTargets.Count} targets remaining, none that we can attack.");
                    
                }
                ModInit.modLog.LogMessage($"There is already an attack sequence active, so we're not doing anything?");
            }
//            ModInit.modLog.LogMessage($"timeSinceAttack was {this.timeSinceLastAttack} (needs to be > {timeBetweenAttacks}) and IsAnyAttackSequenceActive?: {base.Combat.AttackDirector.IsAnyAttackSequenceActive} should be false");
        }
        private Vector3 CalcStartPos()
        {
            this.MaxWeaponRange = 400;
            if (this.StrafeWeapons.Count != 0)
            {
                this.MaxWeaponRange = this.StrafeWeapons.FirstOrDefault().MaxRange;
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
                ModInit.modLog.LogMessage($"Calculating impact points for AOE Strafe.");
                this.HUD = Traverse.Create(CameraControl.Instance).Property("HUD").GetValue<CombatHUD>();
                Vector3 b = (this.EndPos - StartPos) / Math.Max(this.AOECount - 1, 1);
                Vector3 vector = StartPos;
                vector.y = base.Combat.MapMetaData.GetLerpedHeightAt(vector, false);
                ModInit.modLog.LogMessage($"Added impact point {vector}");
                AOEPositions.Add(vector);
                //Utils.SpawnDebugFlare(vector, "vfxPrfPrtl_artillerySmokeSignal_loop", 3);
                for (int i = 0; i < this.AOECount-1; i++)
                {
                    vector += b;
                    vector.y = base.Combat.MapMetaData.GetLerpedHeightAt(vector, false);
                    ModInit.modLog.LogMessage($"Added impact point {vector}");
                    AOEPositions.Add(vector);
                    //Utils.SpawnDebugFlare(vector,"vfxPrfPrtl_artillerySmokeSignal_loop", 3);
                }

                return;
            }

            this.AllTargets = new List<ICombatant>();

            var allCombatants = new List<ICombatant>(base.Combat.GetAllCombatants());

            if (ModInit.modSettings.strafeTargetsFriendliesChance == 0 && ModInit.modSettings.strafeNeutralBuildingsChance == 0)
            {
                allCombatants = new List<ICombatant>(allCombatants.Where(x=>x.team.IsEnemy(this.StrafingTeam)));
            }
            allCombatants.RemoveAll(x => x.GUID == this.Attacker.GUID || x.IsDead);
            for (int i = 0; i < allCombatants.Count; i++)
            {
                if (allCombatants[i] is AbstractActor actor)
                {
                    if (actor.WasDespawned || actor.WasEjected)
                    {
                        continue;
                    }
                }

                if (allCombatants[i] is BattleTech.Building building)
                {
                    if (building.team.GUID == "421027ec-8480-4cc6-bf01-369f84a22012") //only need to check for "World" since friendly buildings will be covered below, and we're ok targeting enemy buildings
                    {
                        var rollBuilding = ModInit.Random.NextDouble();
                        var chanceBuilding = ModInit.modSettings.strafeNeutralBuildingsChance;
                        if (rollBuilding >= chanceBuilding && allCombatants[i].team.IsNeutral(this.StrafingTeam))
                        {
                            ModInit.modLog.LogMessage($"Roll {rollBuilding} >= chance {chanceBuilding}, skipping.");
                            continue;
                        }
                    }
                }

                var roll = ModInit.Random.NextDouble();
                var chance = ModInit.modSettings.strafeTargetsFriendliesChance;
                if (roll >= chance && allCombatants[i].team.IsFriendly(this.StrafingTeam))
                {
                    ModInit.modLog.LogMessage($"Roll {roll} >= chance {chance}, skipping.");
                    continue;
                }
                if (this.IsTarget(allCombatants[i]))
                {
                    this.AllTargets.Add(allCombatants[i]);
                    ModInit.modLog.LogMessage($"Added target {allCombatants[i].DisplayName}: {allCombatants[i].GUID} to final target list.");
                }
            }
            Vector3 preStartPos = this.EndPos - this.StartPos * 2f;
            this.AllTargets.Sort((ICombatant x, ICombatant y) => Vector3.Distance(x.CurrentPosition, preStartPos).CompareTo(Vector3.Distance(y.CurrentPosition, preStartPos)));
        }
        private void GetWeaponsForStrafe()
        {
            this.StrafeWeapons = new List<Weapon>(this.Attacker.Weapons);
            if (this.StrafeWeapons.Count == 0)
            {
                ModInit.modLog.LogMessage($"No weapons found for strafing run.");
                return;
            }
            this.StrafeWeapons.Sort((Weapon x, Weapon y) => y.MaxRange.CompareTo(x.MaxRange));
            ModInit.modLog.LogMessage($"First strafe weapon will be {StrafeWeapons[0].Name} with range {StrafeWeapons[0].MaxRange}");
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
                ModInit.modLog.LogMessage($"Target {combatant.DisplayName} is within weapons range. Distance: {dist}, Range: {this.Radius}");
                return true;
            }
            ModInit.modLog.LogMessage($"Target {combatant.DisplayName} not within weapons range. Distance: {dist}, Range: {this.Radius}");
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

            var msg = new DespawnActorMessage(EncounterLayerData.MapLogicGuid, this.Attacker.GUID, (DeathMethod) DespawnFloatieMessage.Escaped);
            Utils._despawnActorMethod.Invoke(this.Attacker, new object[] {msg});
            foreach (var idx in this.attackSequences)
            {
                base.Combat.AttackDirector.RemoveAttackSequence(idx);
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
                    FlyAwaySequence sequence = new FlyAwaySequence(this.Attacker, this.Velocity, 150f);
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
            switch (this._state)
            {
                case TB_StrafeSequence.SequenceState.Incoming:
                    if (Vector3.Distance(this.Attacker.CurrentPosition, this.StartPos) < this.MaxWeaponRange)
                    {
                        ModInit.modLog.LogMessage($"Setting Strafe SequenceState to Strafing!");
                        this.SetState(TB_StrafeSequence.SequenceState.Strafing);
                    }
                    break;
                case TB_StrafeSequence.SequenceState.Strafing:
                    //var endpoint = this.StartPos + (this.EndPos - this.StartPos).normalized * Vector3.Distance(this.StartPos, this.EndPos);
                    var curPos_2D = this.Attacker.CurrentPosition;
                    curPos_2D.y = 0f;
                    var endPos_2D = this.EndPos;
                    endPos_2D.y = 0f;
                    var startPos_2D = this.StartPos;
                    startPos_2D.y = 0f;
                    var fromEnd = Vector3.Distance(curPos_2D, endPos_2D);
                    var fromStart = Vector3.Distance(curPos_2D, startPos_2D);
                    var angle = Vector3.Angle(this.EndPos, this.Attacker.CurrentPosition);
                    ModInit.modLog.LogMessage($"Strafing unit {fromEnd}m in 2D space from endpoint! Angle to endPoint: {angle}");
                    if (fromEnd < ModInit.modSettings.strafeMinDistanceToEnd || (fromEnd <= fromStart && fromEnd > this.StrafeLength))
                    {
                        ModInit.modLog.LogMessage($"Setting Strafe SequenceState to Finished!");
                        this.SetState(TB_StrafeSequence.SequenceState.Finished);
                    }
                    break;
            }
            switch (this._state)
            {
                case TB_StrafeSequence.SequenceState.Incoming:
                    var pos2 = this.Attacker.CurrentPosition + this.Velocity * Time.deltaTime;
                    pos2.y = this.Combat.MapMetaData.GetLerpedHeightAt(pos2, false) + this.HeightOffset;
                    this.SetPosition(pos2, this.Attacker.CurrentRotation);
                    if (StrafingTeam.IsLocalPlayer)
                    {
                        foreach (var enemy in StrafingTeam.GetAllEnemies())
                        {
                            Vector3 vector = pos2 - enemy.CurrentPosition;
                            vector.y = 0f;
                            ModInit.modLog.LogTrace(
                                $"{enemy.Description.UIName} is {vector.magnitude} from strafing unit for. Unit has sensor range of {base.Combat.LOS.GetSensorRange(Attacker)}!");
                            if (vector.magnitude < ModInit.modSettings.strafeSensorFactor *
                                base.Combat.LOS.GetSensorRange(Attacker))
                            {
                                ModInit.modLog.LogTrace($"Should be showing enemy!");
                                var rep = enemy.GameRep as PilotableActorRepresentation;
                                if (rep != null && !rep.VisibleToPlayer &&
                                    enemy.VisibilityToTargetUnit(StrafingTeam.units.FirstOrDefault(x => !x.IsDead)) <
                                    VisibilityLevel.Blip0Minimum)
                                {
                                    ModInit.modLog.LogTrace($"Game Rep is not null!");
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

                    if (this.Combat.MapMetaData.GetLerpedHeightAt(pos3, false) < this.HeightOffset)
                    {
                        pos3.y = this.Combat.MapMetaData.GetLerpedHeightAt(pos3, false) + this.HeightOffset;
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
