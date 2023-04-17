using BattleTech;
using HBS.Util;
using UnityEngine;

namespace StrategicOperations.Framework
{
    public class TB_FlyAwaySequence : MultiSequence
    {
		public AbstractActor actor { get; set; }
        public Vector3 startDirection { get; set; }
        public float speed { get; set; }
        private float heightOffset { get; set; }
        private float minWeaponRange { get; set; }

        private bool IsOffMap => (this.actor.CurrentPosition.x < this.minMapCoord || this.actor.CurrentPosition.x > this.maxMapCoord) && (this.actor.CurrentPosition.z < this.minMapCoord || this.actor.CurrentPosition.z > this.maxMapCoord);
        public override bool IsCancelable => false;

        public override bool IsComplete => this.state == SequenceState.Finished;
        public override bool IsParallelInterruptable => false;
        public override bool IsValidMultiSequenceChild => false;

        private enum SequenceState
        {
            None,
            FlyingAway,
            Finished
        }

        private TB_FlyAwaySequence.SequenceState state;
        private float timeInCurrentState;
        private float minMapCoord = -1200f;
        private float maxMapCoord = 1200f;
        private float lift = 5f;
        private float thrust = 50f;
        private Vector3 velocity;

        public TB_FlyAwaySequence(AbstractActor actor, Vector3 directionStart, float speed) : base(actor.Combat)
        {
            this.actor = actor;
            this.startDirection = directionStart;
            this.speed = speed;
            this.state = TB_FlyAwaySequence.SequenceState.None;
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
            this.SetState(TB_FlyAwaySequence.SequenceState.FlyingAway);
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
            this.actor.GameRep.thisTransform.position = position;
            this.actor.GameRep.thisTransform.rotation = rotation;
            this.actor.OnPositionUpdate(position, rotation, base.SequenceGUID, false, null, true);
        }
        private void SetState(TB_FlyAwaySequence.SequenceState newState)
        {
            if (this.state == newState)
            {
                return;
            }
            this.state = newState;
            this.timeInCurrentState = 0f;
            if (newState == TB_FlyAwaySequence.SequenceState.FlyingAway)
            {
                Vector3 startDirection = this.startDirection;
                startDirection.Normalize();
                this.velocity = startDirection * this.speed;
                return;
            }
            if (newState != TB_FlyAwaySequence.SequenceState.Finished)
            {
                return;
            }
            this.actor.PlaceFarAwayFromMap();
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
            TB_FlyAwaySequence.SequenceState sequenceState = this.state;
            if (sequenceState != TB_FlyAwaySequence.SequenceState.FlyingAway)
            {
                if (sequenceState != TB_FlyAwaySequence.SequenceState.Finished)
                {
                }
            }
            else if (this.IsOffMap || this.Combat.TurnDirector.IsMissionOver)
            {
                this.SetState(TB_FlyAwaySequence.SequenceState.Finished);
            }
            sequenceState = this.state;
            if (sequenceState != TB_FlyAwaySequence.SequenceState.FlyingAway)
            {
                return;
            }
            Vector3 vector = this.velocity;
            vector.y = 0f;
            this.velocity += vector.normalized * this.thrust * Time.deltaTime;
            this.velocity.y = this.velocity.y + Time.deltaTime * this.lift;
            this.SetPosition(this.actor.CurrentPosition + this.velocity * Time.deltaTime, this.actor.CurrentRotation);
        }
    }
}
