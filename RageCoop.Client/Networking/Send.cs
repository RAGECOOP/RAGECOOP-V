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

        public static int SyncInterval = 30;
        public static List<NetConnection> Targets = new List<NetConnection>();
        public static void Send(Packet p, ConnectionChannel channel = ConnectionChannel.Default, NetDeliveryMethod method = NetDeliveryMethod.UnreliableSequenced)
        {
            NetOutgoingMessage outgoingMessage = Peer.CreateMessage();
            p.Pack(outgoingMessage);
            Peer.SendMessage(outgoingMessage,Targets, method, (int)channel);
        }
        public static void SendTo(Packet p,NetConnection connection, ConnectionChannel channel = ConnectionChannel.Default, NetDeliveryMethod method = NetDeliveryMethod.UnreliableSequenced)
        {
            NetOutgoingMessage outgoingMessage = Peer.CreateMessage();
            p.Pack(outgoingMessage);
            Peer.SendMessage(outgoingMessage, connection, method, (int)channel);
        }

        public static void SendPed(SyncedPed c, bool full)
        {
            if (c.LastSentStopWatch.ElapsedMilliseconds<SyncInterval)
            {
                return;
            }
            Ped p = c.MainPed;
            var packet = new Packets.PedSync()
            {
                ID =c.ID,
                OwnerID=c.OwnerID,
                Health = p.Health,
                Rotation = p.ReadRotation(),
                Velocity = p.ReadVelocity(),
                Speed = p.GetPedSpeed(),
                CurrentWeaponHash = (uint)p.Weapons.Current.Hash,
                Flags = p.GetPedFlags(),
                Heading=p.Heading,
            };
            if (packet.Flags.HasPedFlag(PedDataFlags.IsAiming))
            {
                packet.AimCoords = p.GetAimCoord();
            }
            if (packet.Flags.HasPedFlag(PedDataFlags.IsRagdoll))
            {
                packet.HeadPosition=p.Bones[Bone.SkelHead].Position;
                packet.RightFootPosition=p.Bones[Bone.SkelRightFoot].Position;
                packet.LeftFootPosition=p.Bones[Bone.SkelLeftFoot].Position;
            }
            else
            {
                packet.Position = p.ReadPosition();
            }
            c.LastSentStopWatch.Restart();
            if (full)
            {
                packet.Flags |= PedDataFlags.IsFullSync;
                packet.Clothes=p.GetPedClothes();
                packet.ModelHash=p.Model.Hash;
                packet.WeaponComponents=p.Weapons.Current.GetWeaponComponents();
                packet.WeaponTint=(byte)Function.Call<int>(Hash.GET_PED_WEAPON_TINT_INDEX, p, p.Weapons.Current.Hash);

                Blip b;
                if (c.IsPlayer)
                {
                    packet.BlipColor=Scripting.API.Config.BlipColor;
                    packet.BlipSprite=Scripting.API.Config.BlipSprite;
                    packet.BlipScale=Scripting.API.Config.BlipScale;
                }
                else if ((b = p.AttachedBlip) !=null)
                {
                    packet.BlipColor=b.Color;
                    packet.BlipSprite=b.Sprite;

                    if (packet.BlipSprite==BlipSprite.PoliceOfficer || packet.BlipSprite==BlipSprite.PoliceOfficer2)
                    {
                        packet.BlipScale=0.5f;
                    }
                }
            }
            Send(packet, ConnectionChannel.PedSync);
        }
        public static void SendVehicle(SyncedVehicle v, bool full)
        {
            if (v.LastSentStopWatch.ElapsedMilliseconds<SyncInterval)
            {
                return;
            }
            Vehicle veh = v.MainVehicle;
            var packet = new Packets.VehicleSync()
            {
                ID =v.ID,
                OwnerID=v.OwnerID,
                Flags = veh.GetVehicleFlags(),
                SteeringAngle = veh.SteeringAngle,
                Position = veh.PredictPosition(),
                Velocity=veh.Velocity,
                Quaternion=veh.ReadQuaternion(),
                RotationVelocity=veh.RotationVelocity,
                ThrottlePower = veh.ThrottlePower,
                BrakePower = veh.BrakePower,
            };
            if (v.LastVelocity==default) {v.LastVelocity=packet.Velocity; }
            packet.Acceleration = (packet.Velocity-v.LastVelocity)*1000/v.LastSentStopWatch.ElapsedMilliseconds;
            v.LastSentStopWatch.Restart();
            v.LastVelocity= packet.Velocity;
            if (packet.Flags.HasVehFlag(VehicleDataFlags.IsDeluxoHovering)) { packet.DeluxoWingRatio=v.MainVehicle.GetDeluxoWingRatio(); }
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
                packet.DamageModel=veh.GetVehicleDamageModel();
                packet.LandingGear = veh.IsAircraft ? (byte)veh.LandingGearState : (byte)0;
                packet.RoofState=(byte)veh.RoofState;
                packet.Mods = veh.Mods.GetVehicleMods();
                packet.ModelHash=veh.Model.Hash;
                packet.EngineHealth=veh.EngineHealth;
                packet.Passengers=veh.GetPassengers();
                packet.LockStatus=veh.LockStatus;
                packet.LicensePlate=Function.Call<string>(Hash.GET_VEHICLE_NUMBER_PLATE_TEXT, veh);
                packet.Livery=Function.Call<int>(Hash.GET_VEHICLE_LIVERY, veh);
                if (v.MainVehicle==Game.Player.LastVehicle)
                {
                    packet.RadioStation=Util.GetPlayerRadioIndex();
                }
                if (packet.EngineHealth>v.LastEngineHealth)
                {
                    packet.Flags |= VehicleDataFlags.Repaired;
                }
                v.LastEngineHealth=packet.EngineHealth;
            }
            Send(packet, ConnectionChannel.VehicleSync);
        }
        public static void SendProjectile(SyncedProjectile sp)
        {
            var p = sp.MainProjectile;
            var packet = new Packets.ProjectileSync()
            {
                ID =sp.ID,
                ShooterID=sp.ShooterID,
                Rotation=p.Rotation,
                Velocity=p.Velocity,
                WeaponHash=(uint)p.WeaponHash,
                Exploded=p.IsDead
            };
            packet.Position=p.Position+packet.Velocity*Latency;
            if (p.IsDead) { EntityPool.RemoveProjectile(sp.ID, "Dead"); }
            Send(packet, ConnectionChannel.ProjectileSync);
        }


        #region SYNC EVENTS
        public static void SendBulletShot(Vector3 start, Vector3 end, uint weapon, int ownerID)
        {
            Send(new Packets.BulletShot()
            {
                StartPosition = start,
                EndPosition = end,
                OwnerID = ownerID,
                WeaponHash=weapon,
            }, ConnectionChannel.SyncEvents);
        }
        #endregion
        public static void SendChatMessage(string message)
        {
            NetOutgoingMessage outgoingMessage = Peer.CreateMessage();

            new Packets.ChatMessage(new Func<string, byte[]>((s) =>
            {
                return Security.Encrypt(s.GetBytes());
            })) { Username = Main.Settings.Username, Message = message }.Pack(outgoingMessage);

            Peer.SendMessage(outgoingMessage,ServerConnection, NetDeliveryMethod.ReliableOrdered, (byte)ConnectionChannel.Chat);
            Peer.FlushSendQueue();
        }
    }
}
