using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GTA.Native;
using GTA;

namespace RageCoop.Client
{
    /// <summary>
    /// Synchronized prop, mostly owned by server
    /// </summary>
    public class SyncedProp : SyncedEntity
    {
        internal SyncedProp(int id)
        {
            ID= id;
        }
        /// <summary>
        /// The real entity
        /// </summary>
        public Prop MainProp { get; set; }
        internal new int OwnerID { get
            {
                // alwayse owned by server
                return 0;
            } }
        internal override void Update()
        {

            if (!NeedUpdate) { return; }
            if (MainProp== null || !MainProp.Exists())
            {
                MainProp=World.CreateProp(ModelHash,Position,Rotation,false,false);
                MainProp.IsInvincible=true;
            }
            MainProp.Position=Position;
            MainProp.Rotation=Rotation;
            MainProp.SetFrozen(true);
            LastUpdated=Main.Ticked;
        }
    }
}
