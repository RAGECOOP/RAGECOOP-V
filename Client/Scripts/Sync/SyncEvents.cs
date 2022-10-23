using System;
using GTA;
using GTA.Math;
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

        public static void TriggerBulletShot(uint hash, SyncedPed owner, Vector3 impactPosition)
        {
            // Main.Logger.Trace($"bullet shot:{(WeaponHash)hash}");

            var start = owner.MainPed.GetMuzzlePosition();
            if (start.DistanceTo(impactPosition) > 10)
                // Reduce latency
                start = impactPosition - (impactPosition - start).Normalized * 10;
            Networking.SendBullet(start, impactPosition, hash, owner.ID);
        }

        public static void TriggerVehBulletShot(uint hash, Vehicle veh, SyncedPed owner)
        {
            int i;
            if ((i = veh.GetMuzzleIndex(owner.MainPed.VehicleWeapon)) != -1)
                Networking.SendVehicleBullet(hash, owner, veh.Bones[i]);
            else
                Main.Logger.Warning($"Failed to get muzzle info for vehicle:{veh.DisplayName}");
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
            v.LastSynced = Main.Ticked;
            v.Position = v.MainVehicle.Position;
            v.Quaternion = v.MainVehicle.Quaternion;
        }

        private static void HandleNozzleTransform(Packets.NozzleTransform p)
        {
            EntityPool.GetVehicleByID(p.VehicleID)?.MainVehicle?.SetNozzleAngel(p.Hover ? 1 : 0);
        }

        private static void HandleBulletShot(Vector3 start, Vector3 end, uint weaponHash, int ownerID)
        {
            var p = EntityPool.GetPedByID(ownerID)?.MainPed;
            if (p == null)
            {
                p = Game.Player.Character;
                Main.Logger.Warning("Failed to find owner for bullet");
            }

            var damage = (int)p.GetWeaponDamage(weaponHash);
            weaponHash = WeaponUtil.GetWeaponFix(weaponHash);

            if (!CorePFXAsset.IsLoaded) CorePFXAsset.Request();
            var asset = new WeaponAsset(weaponHash);
            if (!asset.IsLoaded) asset.Request();
            World.ShootBullet(start, end, p, asset, damage);
            Prop w;
            var turret = false;
            if (((w = p.Weapons.CurrentWeaponObject) != null && p.VehicleWeapon == VehicleWeaponHash.Invalid) ||
                (turret = p.IsOnTurretSeat()))
                World.CreateParticleEffectNonLooped(CorePFXAsset,
                    p.Weapons.Current.Components.GetSuppressorComponent().Active
                        ? "muz_pistol_silencer"
                        : ((WeaponHash)weaponHash).GetFlashFX(turret), p.GetMuzzlePosition(),
                    turret ? p.CurrentVehicle.GetMuzzleBone(p.VehicleWeapon).GetRotation() : w.Rotation);
        }

        public static void HandleVehicleBulletShot(Packets.VehicleBulletShot p)
        {
            HandleBulletShot(p.StartPosition, p.EndPosition, p.WeaponHash, p.OwnerID);
            var v = EntityPool.GetPedByID(p.OwnerID)?.MainPed.CurrentVehicle;
            if (v == null) return;
            var b = v.Bones[p.Bone];
            World.CreateParticleEffectNonLooped(CorePFXAsset,
                ((WeaponHash)p.WeaponHash).GetFlashFX(true),
                b.Position, b.GetRotation());
        }

        public static void HandleEvent(PacketType type, NetIncomingMessage msg)
        {
            switch (type)
            {
                case PacketType.BulletShot:
                {
                    var p = msg.GetPacket<Packets.BulletShot>();
                    HandleBulletShot(p.StartPosition, p.EndPosition, p.WeaponHash, p.OwnerID);
                    break;
                }
                case PacketType.VehicleBulletShot:
                {
                    HandleVehicleBulletShot(msg.GetPacket<Packets.VehicleBulletShot>());
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
            if (subject.IsShooting)
            {
                if (!subject.IsUsingProjectileWeapon())
                {
                    var i = 0;
                    Func<bool> getBulletImpact = () =>
                    {
                        var endPos = subject.LastWeaponImpactPosition;
                        if (endPos == default)
                        {
                            if (++i <= 5) return false;

                            endPos = subject.GetAimCoord();
                            if (subject.IsInVehicle() && subject.VehicleWeapon != VehicleWeaponHash.Invalid)
                            {
                                if (subject.IsOnTurretSeat())
                                    TriggerBulletShot((uint)subject.VehicleWeapon, c, endPos);
                                else
                                    TriggerVehBulletShot((uint)subject.VehicleWeapon, subject.CurrentVehicle, c);
                            }
                            else
                            {
                                TriggerBulletShot((uint)subject.Weapons.Current.Hash, c, endPos);
                            }

                            return true;
                        }

                        if (subject.IsInVehicle() && subject.VehicleWeapon != VehicleWeaponHash.Invalid)
                        {
                            if (subject.IsOnTurretSeat())
                                TriggerBulletShot((uint)subject.VehicleWeapon, c, endPos);
                            else
                                TriggerVehBulletShot((uint)subject.VehicleWeapon, subject.CurrentVehicle, c);
                        }
                        else
                        {
                            TriggerBulletShot((uint)subject.Weapons.Current.Hash, c, endPos);
                        }

                        return true;
                    };

                    if (!getBulletImpact()) API.QueueAction(getBulletImpact);
                }
                else if (subject.VehicleWeapon == VehicleWeaponHash.Tank && subject.LastWeaponImpactPosition != default)
                {
                    TriggerBulletShot((uint)VehicleWeaponHash.Tank, c, subject.LastWeaponImpactPosition);
                }
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