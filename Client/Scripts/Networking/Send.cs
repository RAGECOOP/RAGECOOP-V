using System.Collections.Generic;
using GTA;
using GTA.Math;
using GTA.Native;
using Lidgren.Network;
using RageCoop.Client.Scripting;
using RageCoop.Core;

namespace RageCoop.Client
{
    internal static partial class Networking
    {
        public static int SyncInterval = 30;
        public static List<NetConnection> Targets = new List<NetConnection>();

        public static void SendSync(Packet p, ConnectionChannel channel = ConnectionChannel.Default,
            NetDeliveryMethod method = NetDeliveryMethod.UnreliableSequenced)
        {
            Peer.SendTo(p, Targets, channel, method);
        }

        public static void SendPed(SyncedPed sp, bool full)
        {
            if (sp.LastSentStopWatch.ElapsedMilliseconds < SyncInterval) return;
            var ped = sp.MainPed;
            var p = SendPackets.PedPacket;
            p.ID = sp.ID;
            p.OwnerID = sp.OwnerID;
            p.Health = ped.Health;
            p.Rotation = ped.ReadRotation();
            p.Velocity = ped.ReadVelocity();
            p.Speed = ped.GetPedSpeed();
            p.Flags = ped.GetPedFlags();
            p.Heading = ped.Heading;
            if (p.Flags.HasPedFlag(PedDataFlags.IsAiming)) p.AimCoords = ped.GetAimCoord();
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
                    var veh = ped.CurrentVehicle?.GetSyncEntity() ??
                              ped.VehicleTryingToEnter?.GetSyncEntity() ?? ped.LastVehicle?.GetSyncEntity();
                    p.VehicleID = veh?.ID ?? 0;
                    if (p.VehicleID == 0) Log.Error("Invalid vehicle");
                    if (p.Speed == 5)
                        p.Seat = ped.GetSeatTryingToEnter();
                    else
                        p.Seat = ped.SeatIndex;
                    if (!veh.IsLocal && p.Speed == 4 && p.Seat == VehicleSeat.Driver)
                    {
                        veh.OwnerID = LocalPlayerID;
                        SyncEvents.TriggerChangeOwner(veh.ID, LocalPlayerID);
                    }
                }

                p.Position = ped.ReadPosition();
            }

            sp.LastSentStopWatch.Restart();
            if (full)
            {
                if (p.Speed == 4)
                    p.VehicleWeapon = ped.VehicleWeapon;
                p.CurrentWeapon = ped.Weapons.Current.Hash;
                p.Flags |= PedDataFlags.IsFullSync;
                p.Clothes = ped.GetPedClothes();
                p.ModelHash = ped.Model.Hash;
                p.WeaponComponents = ped.Weapons.Current.GetWeaponComponents();
                p.WeaponTint = (byte)Call<int>(GET_PED_WEAPON_TINT_INDEX, ped, ped.Weapons.Current.Hash);

                Blip b;
                if (sp.IsPlayer)
                {
                    p.BlipColor = API.Config.BlipColor;
                    p.BlipSprite = API.Config.BlipSprite;
                    p.BlipScale = API.Config.BlipScale;
                }
                else if ((b = ped.AttachedBlip) != null)
                {
                    p.BlipColor = b.Color;
                    p.BlipSprite = b.Sprite;

                    if (p.BlipSprite == BlipSprite.PoliceOfficer || p.BlipSprite == BlipSprite.PoliceOfficer2)
                        p.BlipScale = 0.5f;
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
            if (v.LastSentStopWatch.ElapsedMilliseconds < SyncInterval) return;
            var veh = v.MainVehicle;
            var packet = SendPackets.VehicelPacket;
            packet.ID = v.ID;
            packet.OwnerID = v.OwnerID;
            packet.Flags = v.GetVehicleFlags();
            packet.SteeringAngle = veh.SteeringAngle;
            packet.Position = veh.ReadPosition();
            packet.Velocity = veh.Velocity;
            packet.Quaternion = veh.ReadQuaternion();
            packet.RotationVelocity = veh.WorldRotationVelocity;
            packet.ThrottlePower = veh.ThrottlePower;
            packet.BrakePower = veh.BrakePower;
            v.LastSentStopWatch.Restart();
            if (packet.Flags.HasVehFlag(VehicleDataFlags.IsDeluxoHovering))
                packet.DeluxoWingRatio = v.MainVehicle.GetDeluxoWingRatio();
            if (full)
            {
                byte primaryColor = 0;
                byte secondaryColor = 0;
                unsafe
                {
                    Call<byte>(GET_VEHICLE_COLOURS, veh, &primaryColor, &secondaryColor);
                }

                packet.Flags |= VehicleDataFlags.IsFullSync;
                packet.Colors = new[] { primaryColor, secondaryColor };
                packet.DamageModel = veh.GetVehicleDamageModel();
                packet.LandingGear = veh.IsAircraft ? (byte)veh.LandingGearState : (byte)0;
                packet.RoofState = (byte)veh.RoofState;
                packet.Mods = veh.Mods.GetVehicleMods();
                packet.ModelHash = veh.Model.Hash;
                packet.EngineHealth = veh.EngineHealth;
                packet.LockStatus = veh.LockStatus;
                packet.LicensePlate = Call<string>(GET_VEHICLE_NUMBER_PLATE_TEXT, veh);
                packet.Livery = Call<int>(GET_VEHICLE_LIVERY, veh);
                packet.RadioStation = v.MainVehicle == LastV
                    ? Util.GetPlayerRadioIndex() : byte.MaxValue;
                if (packet.EngineHealth > v.LastEngineHealth) packet.Flags |= VehicleDataFlags.Repaired;
                v.LastEngineHealth = packet.EngineHealth;
            }

            SendSync(packet, ConnectionChannel.VehicleSync);
        }

        public static void SendProjectile(SyncedProjectile sp)
        {
            sp.ExtractData(ref SendPackets.ProjectilePacket);
            if (sp.MainProjectile.IsDead) EntityPool.RemoveProjectile(sp.ID, "Dead");
            SendSync(SendPackets.ProjectilePacket, ConnectionChannel.ProjectileSync);
        }

        public static void SendChatMessage(string message)
        {
            Peer.SendTo(new Packets.ChatMessage(s => Security.Encrypt(s.GetBytes()))
            { Username = Settings.Username, Message = message }, ServerConnection, ConnectionChannel.Chat,
                NetDeliveryMethod.ReliableOrdered);
            Peer.FlushSendQueue();
        }

        public static void SendVoiceMessage(byte[] buffer, int recorded)
        {
            SendSync(new Packets.Voice { ID = LocalPlayerID, Buffer = buffer, Recorded = recorded },
                ConnectionChannel.Voice, NetDeliveryMethod.ReliableOrdered);
        }

        /// <summary>
        ///     Reduce GC pressure by reusing frequently used packets
        /// </summary>
        private static class SendPackets
        {
            public static readonly Packets.PedSync PedPacket = new Packets.PedSync();
            public static readonly Packets.VehicleSync VehicelPacket = new Packets.VehicleSync();
            public static Packets.ProjectileSync ProjectilePacket = new Packets.ProjectileSync();
        }


        #region SYNC EVENTS

        public static void SendBullet(int ownerID, uint weapon, Vector3 end)
        {
            SendSync(new Packets.BulletShot
            {
                EndPosition = end,
                OwnerID = ownerID,
                WeaponHash = weapon
            }, ConnectionChannel.SyncEvents);
        }

        #endregion
    }
}