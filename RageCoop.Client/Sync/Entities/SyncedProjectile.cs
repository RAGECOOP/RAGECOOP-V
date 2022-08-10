using GTA;
using GTA.Math;
using RageCoop.Core;

namespace RageCoop.Client
{
    internal class SyncedProjectile : SyncedEntity
    {
        public SyncedProjectile(Projectile p)
        {
            ID=EntityPool.RequestNewID();
            MainProjectile = p;
            Origin=p.Position;
            var shooter = EntityPool.GetPedByHandle((p.Owner?.Handle).GetValueOrDefault());
            if (shooter==null)
            {
                // Owner will be the vehicle if projectile is shot with a vehicle
                var shooterVeh = EntityPool.GetVehicleByHandle((p.Owner?.Handle).GetValueOrDefault());
                if (shooterVeh!=null && shooterVeh.MainVehicle.Driver!=null)
                {
                    shooter=shooterVeh.MainVehicle.Driver?.GetSyncEntity();
                }
                else
                {
                    Main.Logger.Warning($"Could not find owner for projectile:{Hash}");
                }
            }
            if (shooter != null)
            {
                if (shooter.MainPed!=null && (p.AttachedEntity==shooter.MainPed.Weapons.CurrentWeaponObject ||  p.AttachedEntity== shooter.MainPed))
                {
                    // Reloading
                    IsValid=false;
                }
                ShooterID=shooter.ID;
                IsLocal=shooter.IsLocal;
            }

        }
        public SyncedProjectile(int id)
        {
            ID= id;
            IsLocal=false;
        }
        public bool IsValid { get; private set; } = true;
        public new bool IsLocal { get; private set; } = false;
        public bool Exploded { get; set; } = false;
        public Projectile MainProjectile { get; set; }
        public int ShooterID { get; set; }
        private SyncedPed Shooter { get; set; }
        public Vector3 Origin { get; set; }

        /// <summary>
        /// Invalid property for projectile.
        /// </summary>
        private new int OwnerID { set { } }
        public WeaponHash Hash { get; set; }
        private WeaponAsset Asset { get; set; }
        internal override void Update()
        {

            // Skip update if no new sync message has arrived.
            if (!NeedUpdate) { return; }

            if (MainProjectile == null || !MainProjectile.Exists())
            {
                CreateProjectile();
                return;
            }
            MainProjectile.Velocity=Velocity+(Position+Shooter.Owner.PacketTravelTime*Velocity-MainProjectile.Position);
            MainProjectile.Rotation=Rotation;
            LastUpdated=Main.Ticked;
        }

        private void CreateProjectile()
        {
            Asset=new WeaponAsset(Hash);
            if (!Asset.IsLoaded) { Asset.Request(); }
            World.ShootBullet(Position, Position+Velocity, (Shooter=EntityPool.GetPedByID(ShooterID))?.MainPed, Asset, 0);
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
