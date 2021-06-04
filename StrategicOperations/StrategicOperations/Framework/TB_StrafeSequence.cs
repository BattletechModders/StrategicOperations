using System;
using System.Collections.Generic;
using System.Linq;
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
        public List<AbstractActor> AllTargets { get; set; }
        public AbstractActor Attacker { get; set; }
        public Vector3 EndPos { get; set; }
        public float HeightOffset { get; set; }
        public override bool IsCancelable => false;
        public override bool IsComplete => this.state == TB_StrafeSequence.SequenceState.Finished;
        public override bool IsParallelInterruptable => false;
        public bool IsValidMultisequenceChild => false;
        public float MaxWeaponRange { get; set; }
        public float Radius { get; set; }
        public Vector3 StartPos { get; set; }
        public List<Weapon> StrafeWeapons { get; set; }
        public Vector3 Velocity { get; set; }
        private const float horizMultiplier = 4f;
        private const float speed = 150f;
        public TB_StrafeSequence.SequenceState state;
        private const float timeBetweenAttacks = 0.35f;
        private const float timeIncoming = 6f;
        public float timeInCurrentState;
        public float timeSinceLastAttack;
        public Vector3 zeroEndPos;
        public Vector3 zeroStartPos;

        public enum SequenceState
        {
            None,
            Incoming,
            Strafing,
            Finished
        }

        public TB_StrafeSequence(AbstractActor attacker, Vector3 positionA, Vector3 positionB,
            float radius) : base(attacker.Combat)
        {
            this.Attacker = attacker;
            this.StartPos = positionA;
            this.EndPos = positionB;
            this.Radius = radius;
            this.state = TB_StrafeSequence.SequenceState.None;
        }

        private void AttackNextTarget()
        {
            this.timeSinceLastAttack += Time.deltaTime;
            if (this.timeSinceLastAttack > 0.35f && !base.Combat.AttackDirector.IsAnyAttackSequenceActive)
            {
                while (this.AllTargets.Count > 0 && Vector3.Distance(this.Attacker.CurrentPosition, this.AllTargets[0].CurrentPosition) <= this.MaxWeaponRange * 0.95f)
                {
                    if (this.Attacker.HasLOFToTargetUnit(this.AllTargets[0], this.StrafeWeapons[0]))
                    {
                        ModInit.modLog.LogMessage($"Strafing unit {Attacker.DisplayName} attacking target {AllTargets[0].DisplayName}");
                        AttackDirector attackDirector = base.Combat.AttackDirector;
                        AttackDirector.AttackSequence attackSequence = attackDirector.CreateAttackSequence(base.SequenceGUID, this.Attacker, this.AllTargets[0], this.Attacker.CurrentPosition, this.Attacker.CurrentRotation, this.AllTargets.Count, this.StrafeWeapons, MeleeAttackType.NotSet, 0, false);
                        attackSequence.ResetWeapons();
                        attackDirector.PerformAttack(attackSequence);
                        this.AllTargets.RemoveAt(0);
                        this.timeSinceLastAttack = 0f;
                        return;
                    }
                    this.AllTargets.RemoveAt(0);
                }
            }
        }
        private Vector3 CalcStartPos()
        {
            Vector3 result = this.StartPos - this.Velocity * 6f;
            this.MaxWeaponRange = this.StrafeWeapons[0].MaxRange;
            this.HeightOffset = this.MaxWeaponRange / 4f;
            result.y += this.HeightOffset;
            return result;
        }

        private void CalcTargets()
        {
            this.AllTargets = new List<AbstractActor>();
            List<AbstractActor> allActors = base.Combat.AllActors;
            for (int i = 0; i < allActors.Count; i++)
            {
                if (this.IsTarget(allActors[i]))
                {
                    this.AllTargets.Add(allActors[i]);
                }
            }
            Vector3 preStartPos = this.EndPos - this.StartPos * 2f;
            this.AllTargets.Sort((AbstractActor x, AbstractActor y) => Vector3.Distance(y.CurrentPosition, preStartPos).CompareTo(Vector3.Distance(x.CurrentPosition, preStartPos)));
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
        }
        private bool IsTarget(AbstractActor actor)
        {
            Vector3 currentPosition = actor.CurrentPosition;
            Vector3 vector = NvMath.NearestPointStrict(this.StartPos, this.EndPos, currentPosition);
            vector.y = base.Combat.MapMetaData.GetLerpedHeightAt(vector, false);
            return Vector3.Distance(vector, currentPosition) < this.Radius;
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
                    this.Velocity = vector * 150f;
                    Vector3 position = this.CalcStartPos();
                    Quaternion rotation = Quaternion.LookRotation(vector);
                    Quaternion rotation2 = Quaternion.LookRotation(Vector3.forward * 5f + Vector3.down * 1f);
                    this.SetPosition(position, rotation);
                    base.SetCamera(CameraControl.Instance.ShowActorCam(this.Attacker, rotation2, 30f), base.MessageIndex);
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
                        this.SetState(TB_StrafeSequence.SequenceState.Strafing);
                    }
                    break;
                case TB_StrafeSequence.SequenceState.Strafing:
                    if (Vector3.Distance(this.Attacker.CurrentPosition, this.EndPos) < this.MaxWeaponRange)
                    {
                        this.SetState(TB_StrafeSequence.SequenceState.Finished);
                    }
                    break;
            }
            switch (this.state)
            {
                case TB_StrafeSequence.SequenceState.Incoming:
                    this.SetPosition(this.Attacker.CurrentPosition + this.Velocity * Time.deltaTime, this.Attacker.CurrentRotation);
                    return;
                case TB_StrafeSequence.SequenceState.Strafing:
                    this.SetPosition(this.Attacker.CurrentPosition + this.Velocity * Time.deltaTime, this.Attacker.CurrentRotation);
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
