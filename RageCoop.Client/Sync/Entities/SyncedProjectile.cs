using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GTA;
using GTA.Math;

namespace RageCoop.Client
{
    internal class SyncedProjectile : SyncedEntity
    {
        public SyncedProjectile(Projectile p)
        {
            ID=EntityPool.RequestNewID();
            IsMine=true;
            MainProjectile = p;
            Origin=p.Position;
            var shooter = EntityPool.GetPedByHandle((p.Owner?.Handle).GetValueOrDefault());
            if(shooter != null)
            {
                ShooterID=shooter.ID;
            }
            else
            {
                // Owner will be the vehicle if projectile is shot with a vehicle
                var shooterVeh = EntityPool.GetVehicleByHandle((p.Owner?.Handle).GetValueOrDefault());
                if (shooterVeh!=null && shooterVeh.MainVehicle.Driver!=null)
                {
                    ShooterID=shooterVeh.MainVehicle.Driver.GetSyncEntity().ID;
                }
                else
                {
                    Main.Logger.Warning($"Could not find owner for projectile:{Hash}");
                }
            }

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
        private SyncedPed Shooter { get;set; }
        public Vector3 Origin { get; set; }

        /// <summary>
        /// Invalid property for projectile.
        /// </summary>
        private new int OwnerID{ set { } }
        public WeaponHash Hash { get; set; }
        private WeaponAsset Asset { get; set; }
        internal override void Update()
        {

            // Skip update if no new sync message has arrived.
            if (!NeedUpdate){  return; }

            if (MainProjectile == null || !MainProjectile.Exists())
            {
                CreateProjectile();
                return;
            }
            MainProjectile.Position=Position;
            MainProjectile.Velocity=Velocity;
            MainProjectile.Rotation=Rotation;
            LastUpdated=Main.Ticked;
        }

        private void CreateProjectile()
        {
            Asset=new WeaponAsset(Hash);
            if (!Asset.IsLoaded) { Asset.Request(); }
            World.ShootBullet(Position,Position+Velocity,(Shooter=EntityPool.GetPedByID(ShooterID))?.MainPed,Asset,0);
            var ps = World.GetAllProjectiles();
            MainProjectile=ps[ps.Length-1];
            if (Hash==(WeaponHash)VehicleWeaponHash.Tank)
            {
                var v = Shooter?.MainPed?.CurrentVehicle;
                if (v!=null)
                {
                    World.CreateParticleEffectNonLooped(SyncEvents.CorePFXAsset, "muz_tank", v.GetMuzzleInfo().Position, v.Bones[35].ForwardVector.ToEulerRotation(v.Bones[35].UpVector), 1);
                }
            }
            EntityPool.Add(this);
        }
    }
}
