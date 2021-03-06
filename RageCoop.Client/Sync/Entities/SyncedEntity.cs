using GTA;
using GTA.Math;

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
            get
            {
                return OwnerID==Main.LocalPlayerID;
            }
        }

        /// <summary>
        /// Network ID for this entity
        /// </summary>
        public int ID { get; internal set; }
        /// <summary>
        /// 
        /// </summary>
        public int OwnerID { get; internal set; }
        /// <summary>
        /// 
        /// </summary>
        public bool IsOutOfSync
        {
            get
            {
                return Main.Ticked-LastSynced>200 && ID!=0;
            }
        }
        internal bool IsReady
        {
            get { return (LastSynced>0||LastFullSynced==0); }
        }
        internal bool IsInvincible { get; set; } = false;
        internal bool NeedUpdate
        {
            get { return LastSynced>=LastUpdated; }
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
        #endregion

        public bool SendNextFrame { get; set; } = false;
        public bool SendFullNextFrame { get; set; } = false;

        /// <summary>
        /// 
        /// </summary>
        internal protected bool _lastFrozen = false;
        internal Model Model { get; set; }
        internal Vector3 Position { get; set; }
        internal Vector3 Rotation { get; set; }
        internal Quaternion Quaternion { get; set; }
        internal Vector3 Velocity { get; set; }
        internal abstract void Update();
        internal void PauseUpdate(ulong frames)
        {
            LastUpdated=Main.Ticked+frames;
        }

    }
}
