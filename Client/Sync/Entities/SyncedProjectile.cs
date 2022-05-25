using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GTA;

namespace RageCoop.Client
{
    internal class SyncedProjectile : SyncedEntity
    {
        public SyncedProjectile(Projectile p)
        {
            ID=EntityPool.RequestNewID();
            IsMine=true;
            MainProjectile = p;
            ShooterID=p.Owner.GetSyncEntity().ID;
        }
        public SyncedProjectile(int id)
        {
            ID= id;
            IsMine=false;
        }
        public new bool IsMine { get; private set; }
        public bool Exploded { get; set; } = false;
        public Projectile MainProjectile { get; set; }
        public int ShooterID { get; set; }

        /// <summary>
        /// Invalid property for projectile.
        /// </summary>
        private new int OwnerID{ set { } }
        public WeaponHash Hash { get; set; }
        private WeaponAsset Asset { get; set; }
        public override void Update()
        {

            // Skip update if no new sync message has arrived.
            if (!NeedUpdate){  return; }

            if (MainProjectile == null || !MainProjectile.Exists())
            {
                CreateProjectile();
                return;
            }
            MainProjectile.PositionNoOffset=Position+Velocity*Networking.Latency;
            MainProjectile.Velocity=Velocity;
            MainProjectile.Rotation=Rotation;
            LastUpdated=Main.Ticked;
        }

        private void CreateProjectile()
        {
            Asset=new WeaponAsset(Hash);
            if (!Asset.IsLoaded) { Asset.Request(); }
            World.ShootBullet(Position,Position+Velocity,EntityPool.GetPedByID(ShooterID)?.MainPed,Asset,0);
            var ps = World.GetAllProjectiles();
            MainProjectile=ps[ps.Length-1];
            EntityPool.Add(this);
        }
    }
}
