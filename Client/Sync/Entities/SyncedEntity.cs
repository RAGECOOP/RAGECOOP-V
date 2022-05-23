using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GTA;
using GTA.Math;

namespace RageCoop.Client
{
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

        public int ID { get; set; }
        public int OwnerID { get;set; }
        public bool IsOutOfSync
        {
            get
            {
                return Main.Ticked-LastSynced>200;
            }
        }
        public bool IsReady
        {
            get {return !(LastSynced==0||LastStateSynced==0);}
        }
        public bool NeedUpdate
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

        public Vector3 Position { get; set; }
        public Vector3 Rotation { get; set; }
        public Vector3 Velocity { get; set; }
        public abstract void Update();

    }
}
