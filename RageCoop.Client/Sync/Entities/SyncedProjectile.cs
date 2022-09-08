using GTA;
using GTA.Math;
using GTA.Native;
using RageCoop.Core;

namespace RageCoop.Client
{
    internal class SyncedProjectile : SyncedEntity
    {
        public ProjectileDataFlags Flags { private get; set; } = ProjectileDataFlags.None;

        public readonly Vector3 Origin;
        private bool _firstSend = false;


        public bool IsValid { get; private set; } = true;
        public new bool IsLocal { get; private set; } = false;
        public Projectile MainProjectile { get; set; }
        public SyncedEntity Shooter { get; set; }
        public bool Exploded => Flags.HasProjDataFlag(ProjectileDataFlags.Exploded);

        internal override Player Owner => Shooter.Owner;
        /// <summary>
        /// Invalid property for projectile.
        /// </summary>
        private new int OwnerID { set { } }
        public WeaponHash WeaponHash { get; set; }
        private WeaponAsset Asset { get; set; }
        public void ExtractData(ref Packets.ProjectileSync p)
        {
            p.Position = MainProjectile.Position;
            p.Velocity = MainProjectile.Velocity;
            p.Rotation = MainProjectile.Rotation;
            p.ID = ID;
            p.ShooterID = Shooter.ID;
            p.WeaponHash = (uint)MainProjectile.WeaponHash;
            p.Flags = ProjectileDataFlags.None;
            if (MainProjectile.IsDead)
            {
                p.Flags |= ProjectileDataFlags.Exploded;
            }
            if (MainProjectile.AttachedEntity != null)
            {
                p.Flags |= ProjectileDataFlags.IsAttached;
            }
            if (Shooter is SyncedVehicle)
            {
                p.Flags |= ProjectileDataFlags.IsShotByVehicle;
            }
            if (_firstSend)
            {
                p.Flags |= ProjectileDataFlags.IsAttached;
                _firstSend = false;
            }

        }
        public SyncedProjectile(Projectile p)
        {
            var owner = p.OwnerEntity;
            if (owner == null) { IsValid = false; return; }
            ID = EntityPool.RequestNewID();
            MainProjectile = p;
            Origin = p.Position;
            if (EntityPool.PedsByHandle.TryGetValue(owner.Handle, out var shooter))
            {
                if (shooter.MainPed != null
                    && (p.AttachedEntity == shooter.MainPed.Weapons.CurrentWeaponObject
                    || p.AttachedEntity == shooter.MainPed))
                {
                    // Reloading
                    IsValid = false;
                    return;
                }
                Shooter = shooter;
                IsLocal = shooter.IsLocal;
            }
            else if (EntityPool.VehiclesByHandle.TryGetValue(owner.Handle, out var shooterVeh))
            {
                Shooter = shooterVeh;
                IsLocal = shooterVeh.IsLocal;
            }
            else
            {
                IsValid = false;
            }
        }
        public SyncedProjectile(int id)
        {
            ID = id;
            IsLocal = false;
        }
        internal override void Update()
        {

            // Skip update if no new sync message has arrived.
            if (!NeedUpdate) { return; }

            if (MainProjectile == null || !MainProjectile.Exists())
            {
                CreateProjectile();
                return;
            }
            MainProjectile.Velocity = Velocity + 10 * (Predict(Position) - MainProjectile.Position);
            MainProjectile.Rotation = Rotation;
            LastUpdated = Main.Ticked;
        }

        private void CreateProjectile()
        {
            Asset = new WeaponAsset(WeaponHash);
            if (!Asset.IsLoaded) { Asset.Request(); return; }
            if (Shooter == null) { return; }
            Entity owner;
            owner = (Shooter as SyncedPed)?.MainPed ?? (Entity)(Shooter as SyncedVehicle)?.MainVehicle;
            Position = (Owner.PacketTravelTime + 0.001f * LastSyncedStopWatch.ElapsedMilliseconds) * Shooter.Velocity + Position;
            var end = Position + Velocity;
            Function.Call(Hash.SHOOT_SINGLE_BULLET_BETWEEN_COORDS_IGNORE_ENTITY, Position.X, Position.Y, Position.Z, end.X, end.Y, end.Z, 0, 1, WeaponHash, owner?.Handle ?? 0, 1, 0, -1);
            var ps = World.GetAllProjectiles();
            MainProjectile = ps[ps.Length - 1];
            MainProjectile.Position = Position;
            MainProjectile.Rotation = Rotation;
            MainProjectile.Velocity = Velocity;
            EntityPool.Add(this);
        }
    }
}
