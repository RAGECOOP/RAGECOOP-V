using System.Diagnostics;
using GTA;
using GTA.Math;

namespace RageCoop.Client
{
    /// <summary>
    /// </summary>
    public abstract class SyncedEntity
    {
        private float _accumulatedOff;

        /// <summary>
        /// </summary>
        protected internal bool _lastFrozen = false;


        private int _ownerID;
        public Stopwatch LastSyncedStopWatch = new Stopwatch();

        /// <summary>
        ///     Indicates whether the current player is responsible for syncing this entity.
        /// </summary>
        public bool IsLocal => OwnerID == Main.LocalPlayerID;

        /// <summary>
        ///     Network ID for this entity
        /// </summary>
        public int ID { get; internal set; }

        /// <summary>
        /// </summary>
        public int OwnerID
        {
            get => _ownerID;
            internal set
            {
                if (value == _ownerID && Owner != null) return;
                _ownerID = value;
                Owner = PlayerList.GetPlayer(value);
                if (this is SyncedPed && Owner != null) Owner.Character = (SyncedPed)this;
            }
        }

        internal virtual Player Owner { get; private set; }

        /// <summary>
        /// </summary>
        public bool IsOutOfSync => Main.Ticked - LastSynced > 200 && ID != 0;

        internal bool IsReady => LastSynced > 0 || LastFullSynced == 0;

        internal bool IsInvincible { get; set; } = false;

        internal bool NeedUpdate => LastSynced >= LastUpdated;

        public bool SendNextFrame { get; set; } = false;
        public bool SendFullNextFrame { get; set; } = false;
        internal Model Model { get; set; }
        internal Vector3 Position { get; set; }
        internal Vector3 Rotation { get; set; }
        internal Quaternion Quaternion { get; set; }
        internal Vector3 Velocity { get; set; }
        internal abstract void Update();

        internal void PauseUpdate(ulong frames)
        {
            LastUpdated = Main.Ticked + frames;
        }

        protected Vector3 Predict(Vector3 input)
        {
            return (Owner.PacketTravelTime + 0.001f * LastSyncedStopWatch.ElapsedMilliseconds) * Velocity + input;
        }

        protected bool IsOff(float thisOff, float tolerance = 3, float limit = 30)
        {
            _accumulatedOff += thisOff - tolerance;
            if (_accumulatedOff < 0)
            {
                _accumulatedOff = 0;
            }
            else if (_accumulatedOff >= limit)
            {
                _accumulatedOff = 0;
                return true;
            }

            return false;
        }

        #region LAST STATE

        /// <summary>
        ///     Last time a new sync message arrived.
        /// </summary>
        public ulong LastSynced { get; set; } = 0;

        /// <summary>
        ///     Last time a new sync message arrived.
        /// </summary>
        public ulong LastFullSynced { get; internal set; } = 0;

        /// <summary>
        ///     Last time the local entity has been updated,
        /// </summary>
        public ulong LastUpdated { get; set; }


        internal Stopwatch LastSentStopWatch { get; set; } = Stopwatch.StartNew();

        #endregion
    }
}