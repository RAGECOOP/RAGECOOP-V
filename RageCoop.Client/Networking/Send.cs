using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Lidgren.Network;
using RageCoop.Core;
using GTA;
using GTA.Native;
using GTA.Math;

namespace RageCoop.Client
{
    internal static partial class Networking
    {


        #region -- SEND --
        /// <summary>
        /// Pack the packet then send to server.
        /// </summary>
        /// <param name="p"></param>
        /// <param name="channel"></param>
        /// <param name="method"></param>
        public static void Send(Packet p, ConnectionChannel channel = ConnectionChannel.Default, NetDeliveryMethod method = NetDeliveryMethod.UnreliableSequenced)
        {
            NetOutgoingMessage outgoingMessage = Client.CreateMessage();
            p.Pack(outgoingMessage);
            Client.SendMessage(outgoingMessage, method, (int)channel);
        }

        public static void SendPed(SyncedPed c)
        {
            Ped p = c.MainPed;
            var packet=new Packets.PedSync()
            {
                ID =c.ID,
                Health = p.Health,
                Position = p.Position,
                Rotation = p.Rotation,
                Velocity = p.Velocity,
                Speed = p.GetPedSpeed(),
                CurrentWeaponHash = (uint)p.Weapons.Current.Hash,
                Flag = p.GetPedFlags(),
                Heading=p.Heading,
            };
            if (packet.Flag.HasFlag(PedDataFlags.IsAiming))
            {
                packet.AimCoords = p.GetAimCoord();
            }
            if (packet.Flag.HasFlag(PedDataFlags.IsRagdoll))
            {
                packet.RotationVelocity=p.RotationVelocity;
            }
            Send(packet, ConnectionChannel.PedSync);
        }
        public static void SendPedState(SyncedPed c)
        {
            Ped p = c.MainPed;

            var packet=new Packets.PedStateSync()
            {
                ID = c.ID,
                OwnerID=c.OwnerID,
                Clothes=p.GetPedClothes(),
                ModelHash=p.Model.Hash,
                WeaponComponents=p.Weapons.Current.GetWeaponComponents(),
                WeaponTint=(byte)Function.Call<int>(Hash.GET_PED_WEAPON_TINT_INDEX, p, p.Weapons.Current.Hash)
            };

            Send(packet, ConnectionChannel.PedSync);
        }
        public static void SendVehicle(SyncedVehicle v)
        {
            Vehicle veh = v.MainVehicle;
            var packet = new Packets.VehicleSync()
            {
                ID =v.ID,
                SteeringAngle = veh.SteeringAngle,
                Position = veh.PredictPosition(),
                Quaternion=veh.Quaternion,
                // Rotation = veh.Rotation,
                Velocity = veh.Velocity,
                RotationVelocity=veh.RotationVelocity,
                ThrottlePower = veh.ThrottlePower,
                BrakePower = veh.BrakePower,
            };
            if (v.MainVehicle.Model.Hash==1483171323) { packet.DeluxoWingRatio=v.MainVehicle.GetDeluxoWingRatio(); }
            Send(packet,ConnectionChannel.VehicleSync);
        }
        public static void SendVehicleState(SyncedVehicle v)
        {
            Vehicle veh = v.MainVehicle;
            byte primaryColor = 0;
            byte secondaryColor = 0;
            unsafe
            {
                Function.Call<byte>(Hash.GET_VEHICLE_COLOURS, veh, &primaryColor, &secondaryColor);
            }
            var packet=new Packets.VehicleStateSync()
            {
                ID =v.ID,
                OwnerID = v.OwnerID,
                Flag = veh.GetVehicleFlags(),
                Colors=new byte[] { primaryColor, secondaryColor },
                DamageModel=veh.GetVehicleDamageModel(),
                LandingGear = veh.IsAircraft ? (byte)veh.LandingGearState : (byte)0,
                Mods = veh.Mods.GetVehicleMods(),
                ModelHash=veh.Model.Hash,
                EngineHealth=veh.EngineHealth,
                Passengers=veh.GetPassengers(),
                LockStatus=veh.LockStatus,
            };
            if (v.MainVehicle==Game.Player.LastVehicle)
            {
                packet.RadioStation=Util.GetPlayerRadioIndex();
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
                Position=p.Position,
                Rotation=p.Rotation,
                Velocity=p.Velocity,
                WeaponHash=(uint)p.WeaponHash,
                Exploded=p.IsDead
            };
            if (p.IsDead) { EntityPool.RemoveProjectile(sp.ID,"Dead"); }
            Send(packet, ConnectionChannel.ProjectileSync);
        }


        #region SYNC EVENTS
        public static void SendBulletShot(Vector3 start,Vector3 end,uint weapon,int ownerID)
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
            NetOutgoingMessage outgoingMessage = Client.CreateMessage();

            new Packets.ChatMessage() { Username = Main.Settings.Username, Message = message }.Pack(outgoingMessage);

            Client.SendMessage(outgoingMessage, NetDeliveryMethod.ReliableOrdered, (byte)ConnectionChannel.Chat);
            Client.FlushSendQueue();

#if DEBUG
#endif
        }
        public static void SendDownloadFinish(int id)
        {
            NetOutgoingMessage outgoingMessage = Client.CreateMessage();

            new Packets.FileTransferComplete() { ID = id }.Pack(outgoingMessage);

            Client.SendMessage(outgoingMessage, NetDeliveryMethod.ReliableOrdered, (byte)ConnectionChannel.File);
            Client.FlushSendQueue();

#if DEBUG
#endif
        }
        #endregion
    }
}
