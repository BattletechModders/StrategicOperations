using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using BattleTech;
using BattleTech.Framework;
using HBS.Math;
using HBS.Util;
using UnityEngine;

namespace StrategicOperations.Framework
{
    public class TB_StrafeSequence : MultiSequence
    {
        private Team PlayerTeam;
        private List<AbstractActor> AllTargets { get; set; }
        private AbstractActor Attacker { get; set; }
        private Vector3 EndPos { get; set; }
        private float HeightOffset { get; set; }
        public override bool IsCancelable => false;
        public override bool IsComplete => this.state == TB_StrafeSequence.SequenceState.Finished;
        public override bool IsParallelInterruptable => false;
        public bool IsValidMultisequenceChild => false;
        private float MaxWeaponRange { get; set; }
        private float Radius { get; set; }
        private Vector3 StartPos { get; set; }
        private float StrafeLength { get; set; }
        private List<Weapon> StrafeWeapons { get; set; }
        private Vector3 Velocity { get; set; }
        private const float horizMultiplier = 4f;
//        private float speed = 150f;
        private TB_StrafeSequence.SequenceState state;
        private const float timeBetweenAttacks = 0.35f;
        private const float timeIncoming = 6f;
        private float timeInCurrentState;
        private float timeSinceLastAttack;
        private Vector3 zeroEndPos;
        private Vector3 zeroStartPos;

        public enum SequenceState
        {
            None,
            Incoming,
            Strafing,
            Finished
        }

        public TB_StrafeSequence(AbstractActor attacker, Vector3 positionA, Vector3 positionB,
            float radius, Team team) : base(attacker.Combat)
        {
            this.Attacker = attacker;
            this.StartPos = positionA;
            this.EndPos = positionB;
            this.StrafeLength = Mathf.Max(1f, Vector3.Distance(positionA, positionB)); 
            this.Radius = radius;
            this.PlayerTeam = team;
            this.state = TB_StrafeSequence.SequenceState.None;
        }

        private void AttackNextTarget()
        {
            this.timeSinceLastAttack += Time.deltaTime;
            if (this.timeSinceLastAttack > ModInit.modSettings.timeBetweenAttacks && !base.Combat.AttackDirector.IsAnyAttackSequenceActive)
            {
                if (this.AllTargets.Count < 1)
                {
                    ModInit.modLog.LogMessage($"We have {this.AllTargets.Count} targets remaining, probably shouldn't be calling AttackNextTarget anymore.");
                    return;
                }
                for (var i = this.AllTargets.Count - 1; i >= 0; i--)
                {
                    var targetDist = Vector3.Distance(this.Attacker.CurrentPosition, this.AllTargets[i].CurrentPosition);
                    var firingAngle = Vector3.Angle(AllTargets[i].CurrentPosition, this.Attacker.CurrentPosition);
                    ModInit.modLog.LogMessage(
                        $"Strafing unit {Attacker.DisplayName} is {targetDist}m from  {AllTargets[i].DisplayName} and firingAngle is {firingAngle}");
                    if (targetDist <= this.MaxWeaponRange)
                    {
                        var filteredWeapons =
                            new List<Weapon>();
                        foreach (var weapon in this.StrafeWeapons)
                        {
                            if (this.Attacker.HasLOFToTargetUnit(AllTargets[i], weapon) && weapon.MaxRange > targetDist)
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
                        AttackDirector attackDirector = base.Combat.AttackDirector;
                        AttackDirector.AttackSequence attackSequence = attackDirector.CreateAttackSequence(
                            base.SequenceGUID, this.Attacker, this.AllTargets[i], this.Attacker.CurrentPosition,
                            this.Attacker.CurrentRotation, this.AllTargets.Count, filteredWeapons,
                            MeleeAttackType.NotSet, 0, false);
                        attackDirector.PerformAttack(attackSequence);
                        attackSequence.ResetWeapons();
                        this.AllTargets.RemoveAt(i);
                        this.timeSinceLastAttack = 0f;
                        return;
                    }
                    ModInit.modLog.LogMessage(
                        $"Attacker {Attacker.DisplayName} range to target {AllTargets[0].DisplayName} {targetDist} >= maxWeaponRange {this.MaxWeaponRange}");
//                    this.AllTargets.RemoveAt(0);
                }
                ModInit.modLog.LogMessage($"We have {this.AllTargets.Count} targets remaining, none that we can attack.");
            }
//            ModInit.modLog.LogMessage($"timeSinceAttack was {this.timeSinceLastAttack} (needs to be > {timeBetweenAttacks}) and IsAnyAttackSequenceActive?: {base.Combat.AttackDirector.IsAnyAttackSequenceActive} should be false");
        }
        private Vector3 CalcStartPos()
        {

            Vector3 result = this.StartPos - this.Velocity * ModInit.modSettings.strafePreDistanceMult;
            this.MaxWeaponRange = this.StrafeWeapons[0].MaxRange;
            this.HeightOffset = Mathf.Clamp(this.MaxWeaponRange, ModInit.modSettings.strafeAltitudeMin,
                ModInit.modSettings.strafeAltitudeMax);
            result.y = base.Combat.MapMetaData.GetLerpedHeightAt(result, false) + this.HeightOffset;
            return result;
        }

        private void CalcTargets()
        {
            this.AllTargets = new List<AbstractActor>();
            var allActors = new List<AbstractActor>(base.Combat.AllActors);
            
            if (!ModInit.modSettings.strafeTargetsFriendlies)
            {
                allActors = new List<AbstractActor>(base.Combat.AllActors.Where(x=>x.team.IsEnemy(this.PlayerTeam)));
            }
            allActors.RemoveAll(x => x.GUID == this.Attacker.GUID || x.IsDead || x.WasDespawned || x.WasEjected);
            for (int i = 0; i < allActors.Count; i++)
            {
                if (this.IsTarget(allActors[i]))
                {
                    this.AllTargets.Add(allActors[i]);
                    ModInit.modLog.LogMessage($"Added target {allActors[i].DisplayName}: {allActors[i].GUID} to final target list.");
                }
            }
            Vector3 preStartPos = this.EndPos - this.StartPos * 2f;
            this.AllTargets.Sort((AbstractActor x, AbstractActor y) => Vector3.Distance(x.CurrentPosition, preStartPos).CompareTo(Vector3.Distance(y.CurrentPosition, preStartPos)));
        }
        private void GetWeaponsForStrafe()
        {
            this.StrafeWeapons = new List<Weapon>(this.Attacker.Weapons);
            if (this.StrafeWeapons.Count == 0)
            {
                ModInit.modLog.LogMessage($"ERROR!! No weapons found for strafing run.");
                return;
            }
            this.StrafeWeapons.Sort((Weapon x, Weapon y) => y.MaxRange.CompareTo(x.MaxRange));
            ModInit.modLog.LogMessage($"First strafe weapon will be {StrafeWeapons[0].Name} with range {StrafeWeapons[0].MaxRange}");
        }

        //maybe need to refresh weapons on attacker instead of in sequence?
        private bool IsTarget(AbstractActor actor)
        {
            Vector3 currentPosition = actor.CurrentPosition;
            Vector3 vector = NvMath.NearestPointStrict(this.StartPos, this.EndPos, currentPosition);
            vector.y = base.Combat.MapMetaData.GetLerpedHeightAt(vector, false);
            var dist = Vector3.Distance(vector, currentPosition);
            if (dist < this.Radius)
            {
                ModInit.modLog.LogMessage($"Target {actor.DisplayName} is within weapons range. Distance: {dist}, Range: {this.Radius}");
                return true;
            }
            ModInit.modLog.LogMessage($"Target {actor.DisplayName} not within weapons range. Distance: {dist}, Range: {this.Radius}");
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
            var msg = new DespawnActorMessage(EncounterLayerData.MapLogicGuid, this.Attacker.GUID,
                (DeathMethod) DespawnFloatieMessage.Escaped);

            Utils._despawnActorMethod.Invoke(this.Attacker,
                new object[] {msg});
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
            if (this.state == newState)
            {
                return;
            }
            this.state = newState;
            this.timeInCurrentState = 0f;
            switch (newState)
            {
                case TB_StrafeSequence.SequenceState.Incoming:
                {
                    this.zeroStartPos = this.StartPos;
                    this.zeroStartPos.y = 0f;
                    this.zeroEndPos = this.EndPos;
                    this.zeroEndPos.y = 0f;
                    this.CalcTargets();
                    this.GetWeaponsForStrafe();
                    Vector3 vector = this.zeroEndPos - this.zeroStartPos;
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
                    base.SetCamera(CameraControl.Instance.ShowActorCam(this.Attacker, rotation2, 300f), base.MessageIndex);
                    return;
                }
                case TB_StrafeSequence.SequenceState.Strafing:
                    base.ClearCamera();
                    if (this.Attacker.team.LocalPlayerControlsTeam)
                    {
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
            this.timeInCurrentState += Time.deltaTime;
            switch (this.state)
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
                    if (fromEnd < ModInit.modSettings.strafeMinDistanceToEnd)
                    {
                        ModInit.modLog.LogMessage($"Setting Strafe SequenceState to Finished!");
                        this.SetState(TB_StrafeSequence.SequenceState.Finished);
                    }
                    break;
            }
            switch (this.state)
            {
                    
                case TB_StrafeSequence.SequenceState.Incoming:
                    var pos2 = this.Attacker.CurrentPosition + this.Velocity * Time.deltaTime;
                    pos2.y = this.Combat.MapMetaData.GetLerpedHeightAt(pos2, false) + this.HeightOffset;
                    this.SetPosition(pos2, this.Attacker.CurrentRotation);
                    return;
                case TB_StrafeSequence.SequenceState.Strafing:
                    var pos3 = this.Attacker.CurrentPosition + this.Velocity * Time.deltaTime;
                    pos3.y = this.Combat.MapMetaData.GetLerpedHeightAt(pos3, false) + this.HeightOffset;
                    this.SetPosition(pos3, this.Attacker.CurrentRotation);
                    this.AttackNextTarget();
                    break;
                case TB_StrafeSequence.SequenceState.Finished:
                    break;
                default:
                    return;
            }
        }

    }
    
}
