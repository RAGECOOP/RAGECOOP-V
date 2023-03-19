using System;
using GTA;
using GTA.Math;
using GTA.Native;
using Lidgren.Network;
using RageCoop.Client.Scripting;
using RageCoop.Core;

namespace RageCoop.Client
{
    internal static class SyncEvents
    {
        #region TRIGGER

        public static void TriggerPedKilled(SyncedPed victim)
        {
            Networking.SendSync(new Packets.PedKilled { VictimID = victim.ID }, ConnectionChannel.SyncEvents);
        }

        public static void TriggerChangeOwner(int vehicleID, int newOwnerID)
        {
            Networking.SendSync(new Packets.OwnerChanged
            {
                ID = vehicleID,
                NewOwnerID = newOwnerID
            }, ConnectionChannel.SyncEvents, NetDeliveryMethod.ReliableOrdered);
        }

        public static void TriggerNozzleTransform(int vehID, bool hover)
        {
            Networking.SendSync(new Packets.NozzleTransform { VehicleID = vehID, Hover = hover },
                ConnectionChannel.SyncEvents);
        }

        #endregion

        #region HANDLE

        public static ParticleEffectAsset CorePFXAsset = new ParticleEffectAsset("core");

        private static void HandlePedKilled(Packets.PedKilled p)
        {
            EntityPool.GetPedByID(p.VictimID)?.MainPed?.Kill();
        }

        private static void HandleOwnerChanged(Packets.OwnerChanged p)
        {
            var v = EntityPool.GetVehicleByID(p.ID);
            if (v == null) return;
            v.OwnerID = p.NewOwnerID;
            v.SetLastSynced(true);
            v.Position = v.MainVehicle.Position;
            v.Quaternion = v.MainVehicle.Quaternion;
        }

        private static void HandleNozzleTransform(Packets.NozzleTransform p)
        {
            EntityPool.GetVehicleByID(p.VehicleID)?.MainVehicle?.SetNozzleAngel(p.Hover ? 1 : 0);
        }

        private static void HandleBulletShot(int ownerID, uint weaponHash, Vector3 end)
        {
            var c = EntityPool.GetPedByID(ownerID);
            var p = c?.MainPed;
            if (p == null)
            {
                return;
                // p = Game.Player.Character;
                // Log.Warning("Failed to find owner for bullet");
            }

            var damage = (int)p.GetWeaponDamage(weaponHash);

            // Some weapon hash has some firing issue, so we need to replace it with known good ones
            weaponHash = WeaponUtil.GetWeaponFix(weaponHash);

            // Request asset for muzzle flash
            if (!CorePFXAsset.IsLoaded) CorePFXAsset.Request();

            // Request asset for materialising the bullet
            var asset = new WeaponAsset(weaponHash);
            if (!asset.IsLoaded) asset.Request();

            var vehWeap = p.VehicleWeapon;
            bool isVeh = vehWeap != VehicleWeaponHash.Invalid;
            var bone = isVeh ? c.MainPed.CurrentVehicle.GetMuzzleBone(vehWeap) : c.MainPed.GetMuzzleBone();
            if (bone == null)
            {
                Log.Warning($"Failed to find muzzle bone for {(isVeh ? vehWeap : (WeaponHash)weaponHash)}, {(isVeh ? p.CurrentVehicle.DisplayName : "")}");
                return;
            }
            World.ShootBullet(bone.Position, end, p, asset, damage);

            World.CreateParticleEffectNonLooped(CorePFXAsset,
                !isVeh && p.Weapons.Current.Components.GetSuppressorComponent().Active
                    ? "muz_pistol_silencer"
                    : ((WeaponHash)weaponHash).GetFlashFX(isVeh), bone.Position, isVeh ? bone.GetRotation() : bone.Owner.Rotation);
        }

        public static void HandleEvent(PacketType type, NetIncomingMessage msg)
        {
            switch (type)
            {
                case PacketType.BulletShot:
                    {
                        var p = msg.GetPacket<Packets.BulletShot>();
                        HandleBulletShot(p.OwnerID, p.WeaponHash, p.EndPosition);
                        break;
                    }
                case PacketType.OwnerChanged:
                    {
                        HandleOwnerChanged(msg.GetPacket<Packets.OwnerChanged>());
                    }
                    break;
                case PacketType.PedKilled:
                    {
                        HandlePedKilled(msg.GetPacket<Packets.PedKilled>());
                    }
                    break;
                case PacketType.NozzleTransform:
                    {
                        HandleNozzleTransform(msg.GetPacket<Packets.NozzleTransform>());
                        break;
                    }
            }

            Networking.Peer.Recycle(msg);
        }

        #endregion

        #region CHECK EVENTS

        public static void Check(SyncedPed c)
        {
            var subject = c.MainPed;

            // Check bullets
            if (subject.IsShooting && !subject.IsUsingProjectileWeapon())
            {

                var i = 0;

                // Some weapon is not instant hit, so we may need to wait a few ticks to get the impact position
                bool getBulletImpact()
                {
                    var endPos = subject.LastWeaponImpactPosition;
                    var vehWeap = subject.VehicleWeapon;
                    if (vehWeap == VehicleWeaponHash.Invalid)
                    {
                        // Ped weapon sync
                        var pedWeap = subject.Weapons.Current.Hash;
                        if (endPos != default)
                        {
                            Networking.SendBullet(c.ID, (uint)pedWeap, endPos);
                            return true;
                        }

                        // Get impact in next tick
                        if (++i <= 5) return false;

                        // Exceeded maximum wait of 5 ticks, return (inaccurate) aim coordinate
                        endPos = subject.GetAimCoord();
                        Networking.SendBullet(c.ID, (uint)pedWeap, endPos);
                        return true;
                    }
                    else
                    {
                        // Veh weapon sync
                        if (endPos == default)
                        {
                            var veh = c.MainPed.CurrentVehicle;
                            var b = veh.GetMuzzleBone(vehWeap);
                            if (b == null)
                            {
                                Log.Warning($"Failed to find muzzle bone for {vehWeap}, {veh.DisplayName}");
                                return true;
                            }
                            endPos = b.Position + b.ForwardVector * 200;
                        }
                        Networking.SendBullet(c.ID, (uint)vehWeap, endPos);
                        return true;
                    }

                }

                if (!getBulletImpact()) API.QueueAction(getBulletImpact);
            }
        }

        public static void Check(SyncedVehicle v)
        {
            if (v.MainVehicle == null || !v.MainVehicle.HasNozzle()) return;

            if (v.LastNozzleAngle == 1 && v.MainVehicle.GetNozzleAngel() != 1)
                TriggerNozzleTransform(v.ID, false);
            else if (v.LastNozzleAngle == 0 && v.MainVehicle.GetNozzleAngel() != 0) TriggerNozzleTransform(v.ID, true);
            v.LastNozzleAngle = v.MainVehicle.GetNozzleAngel();
        }

        #endregion
    }
}