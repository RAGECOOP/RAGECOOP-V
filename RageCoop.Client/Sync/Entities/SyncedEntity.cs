using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
        public bool IsMine
        {
            get
            {
                return OwnerID==Main.LocalPlayerID;
            }
        }
        /// <summary>
        /// Network ID for this entity
        /// </summary>
        public int ID { get;internal set; }
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
                return Main.Ticked-LastSynced>200;
            }
        }
        internal bool IsReady
        {
            get {return !(LastSynced==0||LastStateSynced==0);}
        }
        internal bool NeedUpdate
        {
            get { return LastSynced>LastUpdated; }
        }
        #region LAST STATE
        /// <summary>
        /// Last time a new sync message arrived.
        /// </summary>
        public ulong LastSynced { get; set; } = 0;
        /// <summary>
        /// Last time a new sync message arrived.
        /// </summary>
        public ulong LastStateSynced { get; internal set; } = 0;
        /// <summary>
        /// Last time the local entity has been updated,
        /// </summary>
        public ulong LastUpdated { get; set; } = 0;
        #endregion

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
