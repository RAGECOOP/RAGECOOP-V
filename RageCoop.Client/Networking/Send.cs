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

        public static void SendPed(SyncedPed c,bool full)
        {
            Ped p = c.MainPed;
            var packet=new Packets.PedSync()
            {
                ID =c.ID,
                OwnerID=c.OwnerID,
                Health = p.Health,
                Position = p.Position,
                Rotation = p.Rotation,
                Velocity = p.Velocity,
                Speed = p.GetPedSpeed(),
                CurrentWeaponHash = (uint)p.Weapons.Current.Hash,
                Flag = p.GetPedFlags(),
                Heading=p.Heading,
            };
            if (packet.Flag.HasPedFlag(PedDataFlags.IsAiming))
            {
                packet.AimCoords = p.GetAimCoord();
            }
            if (full)
            {
                packet.Flag |= PedDataFlags.IsFullSync;
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
        public static void SendVehicle(SyncedVehicle v,bool full)
        {
            Vehicle veh = v.MainVehicle;
            var packet = new Packets.VehicleSync()
            {
                ID =v.ID,
                OwnerID=v.OwnerID,
                Flag = veh.GetVehicleFlags(),
                SteeringAngle = veh.SteeringAngle,
                Position = veh.PredictPosition(),
                Quaternion=veh.Quaternion,
                Velocity = veh.Velocity,
                RotationVelocity=veh.RotationVelocity,
                ThrottlePower = veh.ThrottlePower,
                BrakePower = veh.BrakePower,
            };
            if (packet.Flag.HasVehFlag(VehicleDataFlags.IsDeluxoHovering)) { packet.DeluxoWingRatio=v.MainVehicle.GetDeluxoWingRatio(); }
            if (full)
            {
                byte primaryColor = 0;
                byte secondaryColor = 0;
                unsafe
                {
                    Function.Call<byte>(Hash.GET_VEHICLE_COLOURS, veh, &primaryColor, &secondaryColor);
                }
                packet.Flag |= VehicleDataFlags.IsFullSync;
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
            }
            Send(packet,ConnectionChannel.VehicleSync);
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
        #endregion
    }
}
