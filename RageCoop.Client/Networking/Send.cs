using GTA;
using GTA.Math;
using GTA.Native;
using Lidgren.Network;
using RageCoop.Core;
using System;
using System.Collections.Generic;

namespace RageCoop.Client
{
    internal static partial class Networking
    {
        /// <summary>
        /// Reduce GC pressure by reusing frequently used packets
        /// </summary>
        private static class SendPackets
        {
            public static Packets.PedSync PedPacket = new Packets.PedSync();
            public static Packets.VehicleSync VehicelPacket = new Packets.VehicleSync();
            public static Packets.ProjectileSync ProjectilePacket = new Packets.ProjectileSync();
        }
        public static int SyncInterval = 30;
        public static List<NetConnection> Targets = new List<NetConnection>();
        public static void SendSync(Packet p, ConnectionChannel channel = ConnectionChannel.Default, NetDeliveryMethod method = NetDeliveryMethod.UnreliableSequenced)
        {
            Peer.SendTo(p, Targets, channel, method);
        }

        public static void SendPed(SyncedPed sp, bool full)
        {
            if (sp.LastSentStopWatch.ElapsedMilliseconds < SyncInterval)
            {
                return;
            }
            Ped ped = sp.MainPed;
            var p = SendPackets.PedPacket;
            p.ID = sp.ID;
            p.OwnerID = sp.OwnerID;
            p.Health = ped.Health;
            p.Rotation = ped.ReadRotation();
            p.Velocity = ped.ReadVelocity();
            p.Speed = ped.GetPedSpeed();
            p.Flags = ped.GetPedFlags();
            p.Heading = ped.Heading;
            if (p.Flags.HasPedFlag(PedDataFlags.IsAiming))
            {
                p.AimCoords = ped.GetAimCoord();
            }
            if (p.Flags.HasPedFlag(PedDataFlags.IsRagdoll))
            {
                p.HeadPosition = ped.Bones[Bone.SkelHead].Position;
                p.RightFootPosition = ped.Bones[Bone.SkelRightFoot].Position;
                p.LeftFootPosition = ped.Bones[Bone.SkelLeftFoot].Position;
            }
            else
            {
                // Seat sync
                if (p.Speed >= 4)
                {
                    var veh = ped.CurrentVehicle?.GetSyncEntity() ?? ped.VehicleTryingToEnter?.GetSyncEntity() ?? ped.LastVehicle?.GetSyncEntity();
                    p.VehicleID = veh?.ID ?? 0;
                    if (p.VehicleID == 0) { Main.Logger.Error("Invalid vehicle"); }
                    if (p.Speed == 5)
                    {
                        p.Seat = ped.GetSeatTryingToEnter();
                    }
                    else
                    {
                        p.Seat = ped.SeatIndex;
                    }
                    if (!veh.IsLocal && p.Speed == 4 && p.Seat == VehicleSeat.Driver)
                    {
                        veh.OwnerID = Main.LocalPlayerID;
                        SyncEvents.TriggerChangeOwner(veh.ID, Main.LocalPlayerID);
                    }
                }
                p.Position = ped.ReadPosition();
            }
            sp.LastSentStopWatch.Restart();
            if (full)
            {
                var w = ped.VehicleWeapon;
                p.CurrentWeaponHash = (w != VehicleWeaponHash.Invalid) ? (uint)w : (uint)ped.Weapons.Current.Hash;
                p.Flags |= PedDataFlags.IsFullSync;
                p.Clothes = ped.GetPedClothes();
                p.ModelHash = ped.Model.Hash;
                p.WeaponComponents = ped.Weapons.Current.GetWeaponComponents();
                p.WeaponTint = (byte)Function.Call<int>(Hash.GET_PED_WEAPON_TINT_INDEX, ped, ped.Weapons.Current.Hash);

                Blip b;
                if (sp.IsPlayer)
                {
                    p.BlipColor = Scripting.API.Config.BlipColor;
                    p.BlipSprite = Scripting.API.Config.BlipSprite;
                    p.BlipScale = Scripting.API.Config.BlipScale;
                }
                else if ((b = ped.AttachedBlip) != null)
                {
                    p.BlipColor = b.Color;
                    p.BlipSprite = b.Sprite;

                    if (p.BlipSprite == BlipSprite.PoliceOfficer || p.BlipSprite == BlipSprite.PoliceOfficer2)
                    {
                        p.BlipScale = 0.5f;
                    }
                }
                else
                {
                    p.BlipColor = (BlipColor)255;
                }
            }
            SendSync(p, ConnectionChannel.PedSync);
        }
        public static void SendVehicle(SyncedVehicle v, bool full)
        {
            if (v.LastSentStopWatch.ElapsedMilliseconds < SyncInterval)
            {
                return;
            }
            Vehicle veh = v.MainVehicle;
            var packet = SendPackets.VehicelPacket;
            packet.ID = v.ID;
            packet.OwnerID = v.OwnerID;
            packet.Flags = v.GetVehicleFlags();
            packet.SteeringAngle = veh.SteeringAngle;
            packet.Position = veh.ReadPosition();
            packet.Velocity = veh.Velocity;
            packet.Quaternion = veh.ReadQuaternion();
            packet.RotationVelocity = veh.RotationVelocity;
            packet.ThrottlePower = veh.ThrottlePower;
            packet.BrakePower = veh.BrakePower;
            v.LastSentStopWatch.Restart();
            if (packet.Flags.HasVehFlag(VehicleDataFlags.IsDeluxoHovering)) { packet.DeluxoWingRatio = v.MainVehicle.GetDeluxoWingRatio(); }
            if (full)
            {
                byte primaryColor = 0;
                byte secondaryColor = 0;
                unsafe
                {
                    Function.Call<byte>(Hash.GET_VEHICLE_COLOURS, veh, &primaryColor, &secondaryColor);
                }
                packet.Flags |= VehicleDataFlags.IsFullSync;
                packet.Colors = new byte[] { primaryColor, secondaryColor };
                packet.DamageModel = veh.GetVehicleDamageModel();
                packet.LandingGear = veh.IsAircraft ? (byte)veh.LandingGearState : (byte)0;
                packet.RoofState = (byte)veh.RoofState;
                packet.Mods = veh.Mods.GetVehicleMods();
                packet.ModelHash = veh.Model.Hash;
                packet.EngineHealth = veh.EngineHealth;
                packet.LockStatus = veh.LockStatus;
                packet.LicensePlate = Function.Call<string>(Hash.GET_VEHICLE_NUMBER_PLATE_TEXT, veh);
                packet.Livery = Function.Call<int>(Hash.GET_VEHICLE_LIVERY, veh);
                if (v.MainVehicle == Game.Player.LastVehicle)
                {
                    packet.RadioStation = Util.GetPlayerRadioIndex();
                }
                if (packet.EngineHealth > v.LastEngineHealth)
                {
                    packet.Flags |= VehicleDataFlags.Repaired;
                }
                v.LastEngineHealth = packet.EngineHealth;
            }
            SendSync(packet, ConnectionChannel.VehicleSync);
        }
        public static void SendProjectile(SyncedProjectile sp)
        {
            sp.ExtractData(ref SendPackets.ProjectilePacket);
            if (sp.MainProjectile.IsDead) { EntityPool.RemoveProjectile(sp.ID, "Dead"); }
            SendSync(SendPackets.ProjectilePacket, ConnectionChannel.ProjectileSync);
        }


        #region SYNC EVENTS
        public static void SendBullet(Vector3 start, Vector3 end, uint weapon, int ownerID)
        {
            SendSync(new Packets.BulletShot()
            {
                StartPosition = start,
                EndPosition = end,
                OwnerID = ownerID,
                WeaponHash = weapon,
            }, ConnectionChannel.SyncEvents);
        }
        public static void SendVehicleBullet(uint hash, SyncedPed owner, EntityBone b)
        {
            SendSync(new Packets.VehicleBulletShot
            {
                StartPosition = b.Position,
                EndPosition = b.Position + b.ForwardVector,
                OwnerID = owner.ID,
                Bone = (ushort)b.Index,
                WeaponHash = hash
            });
        }
        #endregion
        public static void SendChatMessage(string message)
        {
            Peer.SendTo(new Packets.ChatMessage(new Func<string, byte[]>((s) => Security.Encrypt(s.GetBytes())))
            { Username = Main.Settings.Username, Message = message }, ServerConnection, ConnectionChannel.Chat, NetDeliveryMethod.ReliableOrdered);
            Peer.FlushSendQueue();
        }
        public static void SendVoiceMessage(byte[] buffer, int recorded)
        {
            SendSync(new Packets.Voice() { ID = Main.LocalPlayerID, Buffer = buffer, Recorded = recorded }, ConnectionChannel.Voice, NetDeliveryMethod.ReliableOrdered);
        }
    }
}
