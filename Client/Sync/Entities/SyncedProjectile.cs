using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GTA;

namespace RageCoop.Client
{
    internal class SyncedProjectile:SyncedEntity
    {
        public bool Exploded { get; set; } = false;
        public Projectile MainProjectile { get; set; }
        public int ShooterID { get; set; }
        public WeaponHash Hash { get; set; }
        private WeaponAsset Asset { get; set; }
        private bool _creatingProjectile{  get;set; }=false;
        private ulong _projectileShotTime { get;set; }
        public override void Update()
        {
            // Check if all data avalible
            if (!IsReady) { return; }

            // Skip update if no new sync message has arrived.
            if (!NeedUpdate)
            {  return; }

            if (_creatingProjectile) { return; }

            if (MainProjectile == null || !MainProjectile.Exists())
            {
                CreateProjectile();
                return;
            }
            MainProjectile.Position=Position+Velocity*Networking.Latency;
            MainProjectile.Velocity=Velocity;
            MainProjectile.Rotation=Rotation;
            if (Exploded)
            {
                if (Exploded)
                {
                    if(MainProjectile != null && MainProjectile.Exists())
                    {
                        MainProjectile.Explode();
                        return;
                    }
                }
            }
        }

        private void CreateProjectile()
        {
            Asset=new WeaponAsset(Hash);
            if (!Asset.IsLoaded) { Asset.Request(); }
            World.ShootBullet(Position,Position+Velocity,EntityPool.GetPedByID(ShooterID)?.MainPed,Asset,0,Velocity.Length());
            _projectileShotTime=Main.Ticked;
            _creatingProjectile=true;

            EventHandler<ProjectileShotEventArgs> checker = null;
            checker= (sender, e) =>
            {
                if (Main.Ticked<=_projectileShotTime+1)
                {
                    if (e.Projectile.WeaponHash==Hash)
                    {
                        MainProjectile=e.Projectile;
                        _creatingProjectile=false;
                        SyncEvents.OnProjectileShot-=checker;
                    }
                }
                else
                {
                    _creatingProjectile=false;
                    SyncEvents.OnProjectileShot-=checker;
                }
            };
            SyncEvents.OnProjectileShot+=checker;
        }
    }
}
