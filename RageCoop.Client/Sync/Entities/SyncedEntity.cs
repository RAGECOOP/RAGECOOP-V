using GTA;
using GTA.Math;
using System.Diagnostics;

namespace RageCoop.Client
{
    /// <summary>
    /// 
    /// </summary>
    public abstract class SyncedEntity
    {

        /// <summary>
        /// Indicates whether the current player is responsible for syncing this entity.
        /// </summary>
        public bool IsLocal
        {
            get => OwnerID == Main.LocalPlayerID;
        }

        /// <summary>
        /// Network ID for this entity
        /// </summary>
        public int ID { get; internal set; }


        private int _ownerID;
        /// <summary>
        /// 
        /// </summary>
        public int OwnerID
        {
            get => _ownerID;
            internal set
            {
                if (value == _ownerID && Owner != null) { return; }
                _ownerID = value;
                Owner = PlayerList.GetPlayer(value);
                if (this is SyncedPed && Owner != null)
                {
                    Owner.Character = ((SyncedPed)this);
                }
            }
        }

        internal virtual Player Owner { get; private set; }
        /// <summary>
        /// 
        /// </summary>
        public bool IsOutOfSync
        {
            get => Main.Ticked - LastSynced > 200 && ID != 0;
        }
        internal bool IsReady
        {
            get => LastSynced > 0 || LastFullSynced == 0;
        }
        internal bool IsInvincible { get; set; } = false;
        internal bool NeedUpdate
        {
            get => LastSynced >= LastUpdated;
        }
        #region LAST STATE
        /// <summary>
        /// Last time a new sync message arrived.
        /// </summary>
        public ulong LastSynced { get; set; } = 0;
        /// <summary>
        /// Last time a new sync message arrived.
        /// </summary>
        public ulong LastFullSynced { get; internal set; } = 0;
        /// <summary>
        /// Last time the local entity has been updated,
        /// </summary>
        public ulong LastUpdated { get; set; } = 0;


        internal Stopwatch LastSentStopWatch { get; set; } = Stopwatch.StartNew();
        #endregion

        public bool SendNextFrame { get; set; } = false;
        public bool SendFullNextFrame { get; set; } = false;

        /// <summary>
        /// 
        /// </summary>
        protected internal bool _lastFrozen = false;
        internal Model Model { get; set; }
        internal Vector3 Position { get; set; }
        internal Vector3 Rotation { get; set; }
        internal Quaternion Quaternion { get; set; }
        internal Vector3 Velocity { get; set; }
        public Stopwatch LastSyncedStopWatch = new Stopwatch();
        internal abstract void Update();
        internal void PauseUpdate(ulong frames)
        {
            LastUpdated = Main.Ticked + frames;
        }
        protected Vector3 Predict(Vector3 input)
        {
            return (Owner.PacketTravelTime + 0.001f * LastSyncedStopWatch.ElapsedMilliseconds) * Velocity + input;
        }
        private float _accumulatedOff = 0;
        protected bool IsOff(float thisOff, float tolerance = 3, float limit = 30)
        {
            _accumulatedOff += thisOff - tolerance;
            if (_accumulatedOff < 0) { _accumulatedOff = 0; }
            else if (_accumulatedOff >= limit)
            {
                _accumulatedOff = 0;
                return true;
            }
            return false;
        }
    }
}
