using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GTA;
using GTA.Math;
using RageCoop.Core;
using GTA.Native;
using System.Threading;

namespace RageCoop.Client { 
    internal static class SyncEvents
    {
        #region TRIGGER
        /// <summary>
        /// Informs other players that this ped has been killed.
        /// </summary>
        /// <param name="c"></param>
        public static void TriggerPedKilled(SyncedPed victim)
        {
            Main.MainNetworking.Send(new Packets.PedKilled() { VictimID=victim.ID},ConnectionChannel.SyncEvents);
        }

        public static void TriggerEnteringVehicle(SyncedPed c,SyncedVehicle veh, VehicleSeat seat)
        {
            Main.MainNetworking.SendEnteringVehicle(c.ID,veh.ID,(short)seat);
        }

        public static void TriggerEnteredVehicle(SyncedPed c, SyncedVehicle veh, VehicleSeat seat)
        {
            Main.MainNetworking.Send(new Packets.EnteredVehicle()
            {
                VehicleSeat=(short)seat,
                PedID=c.ID,
                VehicleID=veh.ID
            },ConnectionChannel.SyncEvents);
            if (seat==VehicleSeat.Driver)
            {
                TriggerChangeOwner(veh, c.ID);
                veh.OwnerID=Main.MyPlayerID;
                veh.LastSynced=Main.Ticked;
            }
        }

        public static void TriggerChangeOwner(SyncedVehicle c, int newOwnerID)
        {
            Main.MainNetworking.SendOwnerChanged(c.ID,newOwnerID);
        }

        public static void TriggerBulletShot(uint hash,SyncedPed owner,Vector3 impactPosition)
        {
            // Minigun, not working for some reason
            if (hash==(uint)WeaponHash.Minigun)
            {
                hash=(uint)WeaponHash.HeavyRifle;
            }
            // Valkyire, not working for some reason
            if (hash==2756787765) { hash=(uint)WeaponHash.HeavyRifle; }

            var start = owner.MainPed.GetMuzzlePosition();
            if (owner.MainPed.IsOnTurretSeat()) { start=owner.MainPed.Bones[Bone.SkelHead].Position; }
            if (start.DistanceTo(impactPosition)>10)
            {
                // Reduce latency
                start=impactPosition-(impactPosition-start).Normalized*10;
            }
            Main.MainNetworking.SendBulletShot(start, impactPosition, hash, owner.ID);
        }
        public static void TriggerLeaveVehicle(int id)
        {
            Main.MainNetworking.SendLeaveVehicle(id);
        }

        public static void TriggerVehBulletShot(uint hash, Vehicle veh)
        {

        }

        public static void TriggerProjectileShot(Projectile p)
        {
            var pp = p.Owner;
            if (pp.IsShooting)
            {
                
                var start = p.Position;
                var end = start+p.Velocity;
                Main.MainNetworking.SendBulletShot(start, end, (uint)p.WeaponHash, pp.GetSyncEntity().ID);
            }
        }
        #endregion

        #region HANDLE

        public static void HandleLeaveVehicle(Packets.LeaveVehicle p)
        {
            var ped = EntityPool.GetPedByID(p.ID)?.MainPed;
            var flag = LeaveVehicleFlags.None;
            // Bail out
            if (ped?.CurrentVehicle==null) { return; }
            if (ped.CurrentVehicle.Speed>5) { flag|=LeaveVehicleFlags.BailOut;}
            ped.Task.LeaveVehicle(flag) ;
        }
        public static void HandlePedKilled(Packets.PedKilled p)
        {
            EntityPool.GetPedByID(p.VictimID)?.MainPed?.Kill();
        }
        public static void HandleEnteringVehicle(SyncedPed c, SyncedVehicle veh, VehicleSeat seat)
        {
            c.MainPed?.Task.EnterVehicle(veh.MainVehicle, seat,-1,2,EnterVehicleFlags.WarpToDoor|EnterVehicleFlags.AllowJacking);
        }
        public static void HandleEnteredVehicle(int pedId, int vehId, VehicleSeat seat)
        {
            var v = EntityPool.GetVehicleByID(vehId);
            if (v==null) { return; }
            EntityPool.GetPedByID(pedId)?.MainPed?.SetIntoVehicle(v.MainVehicle, seat);
        }
        public static void HandleOwnerChanged(Packets.OwnerChanged p)
        {
            var v = EntityPool.GetVehicleByID(p.ID);
            if (v==null) { return; }
            v.OwnerID=p.NewOwnerID;
        }
        private static ParticleEffectAsset CorePFXAsset = default;

        static WeaponAsset _weaponAsset = default;
        static uint _lastWeaponHash;
        public static void HandleBulletShot(Vector3 start, Vector3 end, uint weaponHash, int ownerID)
        {
            if (CorePFXAsset==default) { 
                CorePFXAsset= new ParticleEffectAsset("core");
            }
            if (!CorePFXAsset.IsLoaded) { CorePFXAsset.Request(); }
            var p = EntityPool.GetPedByID(ownerID)?.MainPed;
            if (p == null) { p=Game.Player.Character; Main.Logger.Warning("Failed to find owner for bullet"); }
            if (_lastWeaponHash!=weaponHash)
            {
                _weaponAsset=new WeaponAsset(weaponHash);
                _lastWeaponHash=weaponHash;
            }
            if (!_weaponAsset.IsLoaded) { _weaponAsset.Request(); }
            World.ShootBullet(start, end, p, _weaponAsset, p.GetWeaponDamage());
            var w = p.Weapons.CurrentWeaponObject;
            if(w != null)
            {
                if (p.Weapons.Current.Components.GetSuppressorComponent().Active)
                {
                    World.CreateParticleEffectNonLooped(CorePFXAsset, "muz_pistol_silencer", p.GetMuzzlePosition(), w.Rotation, 1);
                }
                else
                {
                    World.CreateParticleEffectNonLooped(CorePFXAsset, "muz_assault_rifle", p.GetMuzzlePosition(), w.Rotation, 1);
                }

            }
        }
        
        public static void HandleEvent(PacketTypes type,byte[] data)
        {
            switch (type)
            {
                case PacketTypes.BulletShot:
                    {
                        Packets.BulletShot p = new Packets.BulletShot();
                        p.Unpack(data);
                        HandleBulletShot(p.StartPosition.ToVector(), p.EndPosition.ToVector(), p.WeaponHash, p.OwnerID);
                        break;
                    }
                case PacketTypes.EnteringVehicle:
                    {
                        Packets.EnteringVehicle p = new Packets.EnteringVehicle();
                        p.Unpack(data);
                        HandleEnteringVehicle(EntityPool.GetPedByID(p.PedID), EntityPool.GetVehicleByID(p.VehicleID), (VehicleSeat)p.VehicleSeat);


                    }
                    break;
                case PacketTypes.LeaveVehicle:
                    {
                        Packets.LeaveVehicle packet = new Packets.LeaveVehicle();
                        packet.Unpack(data);
                        HandleLeaveVehicle(packet);
                    }
                    break;
                case PacketTypes.OwnerChanged:
                    {
                        Packets.OwnerChanged packet = new Packets.OwnerChanged();
                        packet.Unpack(data);
                        HandleOwnerChanged(packet);
                    }
                    break;
                case PacketTypes.PedKilled:
                    {
                        var packet = new Packets.PedKilled();
                        packet.Unpack(data);
                        HandlePedKilled(packet);
                    }
                    break;
                case PacketTypes.EnteredVehicle:
                    {
                        var packet = new Packets.EnteredVehicle();
                        packet.Unpack(data);
                        HandleEnteredVehicle(packet.PedID,packet.VehicleID,(VehicleSeat)packet.VehicleSeat);
                        break;
                    }
            }
        }

        public static int GetWeaponDamage(this Ped p)
        {
            if (p.VehicleWeapon!=VehicleWeaponHash.Invalid)
            {
                return 30;
            }
            switch (p.Weapons.Current.Group)
            {
                case WeaponGroup.Pistol: return 30;
                case WeaponGroup.AssaultRifle: return 30;
                case WeaponGroup.SMG: return 20;
                case WeaponGroup.MG: return 40;
                case WeaponGroup.Shotgun: return 30;
                case WeaponGroup.Sniper: return 200;
                case WeaponGroup.Heavy: return 30;
            }
            return 0;
        }


        #endregion
        /*
        public static List<WeaponHash> ProjectileWeapons = new List<WeaponHash>
        {
            WeaponHash.RPG,
            WeaponHash.Grenade,
            WeaponHash.GrenadeLauncher,
            WeaponHash.Ball,
            WeaponHash.Bottle,
            WeaponHash.BZGas,
            WeaponHash.Firework,

        };
        */
        #region CHECK EVENTS

        static bool _projectileShot = false;
        static Projectile[] lastProjectiles=new Projectile[0];
        public static void CheckProjectiles()
        {
            Projectile[] projectiles = World.GetAllProjectiles();

            _projectileShot=false;
            foreach (Projectile p in projectiles)
            {
                var owner = p.Owner.GetSyncEntity();
                if ((!lastProjectiles.Contains(p)) && (owner!=null) && owner.IsMine)
                {
                    TriggerProjectileShot(p);
                    _projectileShot=true;
                }
            }

            lastProjectiles=projectiles;
        }

        public static void Check(SyncedPed c)
        {
            Ped subject = c.MainPed;
            if (subject.IsShooting && !_projectileShot)
            {
                int i = 0;
                Func<bool> getBulletImpact = (() =>
                {
                    Vector3 endPos = subject.LastWeaponImpactPosition;
                    if (endPos==default ) 
                    {
                        if (i>5)
                        {
                            endPos=subject.GetAimCoord();
                            if (subject.IsInVehicle() && subject.VehicleWeapon!=VehicleWeaponHash.Invalid)
                            {
                                if (subject.IsOnTurretSeat())
                                {
                                    TriggerBulletShot((uint)subject.VehicleWeapon,subject.GetSyncEntity(), endPos);
                                }
                                else
                                {
                                    TriggerVehBulletShot((uint)subject.VehicleWeapon, subject.CurrentVehicle);
                                }
                            }
                            else
                            {
                                TriggerBulletShot((uint)subject.Weapons.Current.Hash, subject.GetSyncEntity(), endPos);
                            }
                            return true;
                        }
                        i++;
                        return false;
                    }
                    else 
                    {
                        if (subject.IsInVehicle() && subject.VehicleWeapon!=VehicleWeaponHash.Invalid)
                        {
                            if (subject.IsOnTurretSeat())
                            {
                                TriggerBulletShot((uint)subject.VehicleWeapon, subject.GetSyncEntity(), endPos);
                            }
                            else
                            {
                                TriggerVehBulletShot((uint)subject.VehicleWeapon, subject.CurrentVehicle);
                            }
                        }
                        else
                        {
                            TriggerBulletShot((uint)subject.Weapons.Current.Hash, subject.GetSyncEntity(), endPos);
                        }
                        return true; 
                    }
                    
                    
                });
                if (!getBulletImpact())
                {
                    Main.QueueAction(getBulletImpact);
                }
                
            }

            // Vehicles
            var g = subject.IsGettingIntoVehicle;
            if ( g && (!c._lastEnteringVehicle))
            { 
                var v = subject.VehicleTryingToEnter.GetSyncEntity();
                TriggerEnteringVehicle(c, v, subject.GetSeatTryingToEnter());
            }
            var currentSitting= subject.IsSittingInVehicle();
            if (c._lastSittingInVehicle)
            {
                if (!currentSitting)
                {
                    var veh = subject.CurrentVehicle;
                    if (veh!=null)
                    {
                        var v = veh.GetSyncEntity();
                        TriggerLeaveVehicle(c.ID);
                    }
                }
            }
            else if (currentSitting)
            {
                TriggerEnteredVehicle(c, subject.CurrentVehicle.GetSyncEntity(), subject.SeatIndex);
            }
            c._lastSittingInVehicle=currentSitting;
            c._lastEnteringVehicle=g;
        }
        
        
        #endregion
    }
}
