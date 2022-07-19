
using System;
using GTA;
using GTA.Native;
using RageCoop.Core;
using System.Collections.Generic;
using System.Linq;
using RageCoop.Client.Scripting;
using System.Text;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Security.Cryptography;

namespace RageCoop.Client
{
    internal class EntityPool
    {
        public static object PedsLock = new object();
        private static Dictionary<int, SyncedPed> ID_Peds = new Dictionary<int, SyncedPed>();
        public static int CharactersCount { get { return ID_Peds.Count; } }
#if BENCHMARK
        private static Stopwatch PerfCounter=new Stopwatch();
        private static Stopwatch PerfCounter2=Stopwatch.StartNew();
#endif
        /// <summary>
        /// Faster access to Character with Handle, but values may not equal to <see cref="ID_Peds"/> since Ped might not have been created.
        /// </summary>
        private static Dictionary<int, SyncedPed> Handle_Peds = new Dictionary<int, SyncedPed>();


        public static object VehiclesLock = new object();
        private static Dictionary<int, SyncedVehicle> ID_Vehicles = new Dictionary<int, SyncedVehicle>();
        private static Dictionary<int, SyncedVehicle> Handle_Vehicles = new Dictionary<int, SyncedVehicle>();

        public static object ProjectilesLock = new object();
        private static Dictionary<int, SyncedProjectile> ID_Projectiles = new Dictionary<int, SyncedProjectile>();
        private static Dictionary<int, SyncedProjectile> Handle_Projectiles = new Dictionary<int, SyncedProjectile>();

        public static object PropsLock=new object();
        public static Dictionary<int,SyncedProp> ServerProps=new Dictionary<int,SyncedProp>();

        public static object BlipsLock = new object();
        public static Dictionary<int, Blip> ServerBlips = new Dictionary<int, Blip>();


        public static void Cleanup(bool keepPlayer=true,bool keepMine=true)
        {
            foreach(int id in new List<int>(ID_Peds.Keys))
            {
                if (keepPlayer&&(id==Main.LocalPlayerID)) { continue; }
                if (keepMine&&(ID_Peds[id].OwnerID==Main.LocalPlayerID)) { continue; }
                RemovePed(id);
            }
            ID_Peds.Clear();
            Handle_Peds.Clear();

            foreach (int id in new List<int>(ID_Vehicles.Keys))
            {
                if (keepMine&&(ID_Vehicles[id].OwnerID==Main.LocalPlayerID)) { continue; }
                RemoveVehicle(id);
            }
            ID_Vehicles.Clear();
            Handle_Vehicles.Clear();

            foreach(var p in ID_Projectiles.Values)
            {
                if (p.ShooterID!=Main.LocalPlayerID && p.MainProjectile!=null && p.MainProjectile.Exists())
                {
                    p.MainProjectile.Delete();
                }
            }
            ID_Projectiles.Clear();
            Handle_Projectiles.Clear();

            foreach(var p in ServerProps.Values)
            {
                p?.MainProp?.Delete();
            }
            ServerProps.Clear();

            foreach(var b in ServerBlips.Values)
            {
                if (b.Exists())
                {
                    b.Delete();
                }
            }
            ServerBlips.Clear();
        }

        #region PEDS
        public static SyncedPed GetPedByID(int id)
        {
            return ID_Peds.ContainsKey(id) ? ID_Peds[id] : null;
        }
        public static SyncedPed GetPedByHandle(int handle)
        {
            return Handle_Peds.ContainsKey(handle) ? Handle_Peds[handle] : null;
        }
        public static List<int> GetPedIDs()
        {
            return new List<int>(ID_Peds.Keys);
        }
        public static bool AddPlayer()
        {
            Ped p = Game.Player.Character;
            SyncedPed player = GetPedByID(Main.LocalPlayerID);
            if (player!=null)
            {
                if (player.MainPed!=p)
                {
                    // Player model changed
                    player.MainPed = p;

                    // Remove it from Handle_Characters
                    var pairs=Handle_Peds.Where(x=>x.Value==player);
                    if (pairs.Any())
                    {
                        var pair=pairs.First();

                        // Re-add
                        Handle_Peds.Remove(pair.Key);
                        if (Handle_Peds.ContainsKey(p.Handle))
                        {
                            RemovePed(Handle_Peds[p.Handle].ID);
                        }
                        Handle_Peds.Add(p.Handle, player);
                    }
                }
            }
            else
            {
                Main.Logger.Debug($"Creating SyncEntity for player, handle:{p.Handle}");
                SyncedPed c = new SyncedPed(p);
                Main.LocalPlayerID=c.OwnerID=c.ID;
                Add(c);
                Main.Logger.Debug($"Local player ID is:{c.ID}");
                PlayerList.SetPlayer(c.ID, Main.Settings.Username );
                return true;
            }
            return false;
        }
        public static void Add(SyncedPed c)
        {
            if (ID_Peds.ContainsKey(c.ID))
            {
                ID_Peds[c.ID]=c;
            }
            else
            {
                ID_Peds.Add(c.ID, c);
            }
            if (c.MainPed==null) { return; }
            if (Handle_Peds.ContainsKey(c.MainPed.Handle))
            {
                Handle_Peds[c.MainPed.Handle]=c;
            }
            else
            {
                Handle_Peds.Add(c.MainPed.Handle, c);
            }
            if (c.IsLocal)
            {
                API.Events.InvokePedSpawned(c);
            }
        }
        public static void RemovePed(int id,string reason="Cleanup")
        {
            if (ID_Peds.ContainsKey(id))
            {
                SyncedPed c = ID_Peds[id];
                var p = c.MainPed;
                if (p!=null)
                {
                    if (Handle_Peds.ContainsKey(p.Handle))
                    {
                        Handle_Peds.Remove(p.Handle);
                    }
                    // Main.Logger.Debug($"Removing ped {c.ID}. Reason:{reason}");
                    p.AttachedBlip?.Delete();
                    p.Kill();
                    p.Model.MarkAsNoLongerNeeded();
                    p.MarkAsNoLongerNeeded();
                    p.Delete();
                }
                c.PedBlip?.Delete();
                c.ParachuteProp?.Delete();
                ID_Peds.Remove(id);
                if (c.IsLocal)
                {
                    API.Events.InvokePedDeleted(c);
                }
            }
        }
        #endregion

        #region VEHICLES
        public static SyncedVehicle GetVehicleByID(int id)
        {
            return ID_Vehicles.ContainsKey(id) ? ID_Vehicles[id] : null;
        }
        public static SyncedVehicle GetVehicleByHandle(int handle)
        {
            return Handle_Vehicles.ContainsKey(handle) ? Handle_Vehicles[handle] : null;
        }
        public static List<int> GetVehicleIDs()
        {
            return new List<int>(ID_Vehicles.Keys);
        }
        public static void Add(SyncedVehicle v)
        {
            if (ID_Vehicles.ContainsKey(v.ID))
            {
                ID_Vehicles[v.ID]=v;
            }
            else
            {
                ID_Vehicles.Add(v.ID, v);
            }
            if (v.MainVehicle==null) { return; }
            if (Handle_Vehicles.ContainsKey(v.MainVehicle.Handle))
            {
                Handle_Vehicles[v.MainVehicle.Handle]=v;
            }
            else
            {
                Handle_Vehicles.Add(v.MainVehicle.Handle, v);
            }
            if (v.IsLocal)
            {
                API.Events.InvokeVehicleSpawned(v);
            }
        }
        public static void RemoveVehicle(int id,string reason = "Cleanup")
        {
            if (ID_Vehicles.ContainsKey(id))
            {
                SyncedVehicle v = ID_Vehicles[id];
                var veh = v.MainVehicle;
                if (veh!=null)
                {
                    if (Handle_Vehicles.ContainsKey(veh.Handle))
                    {
                        Handle_Vehicles.Remove(veh.Handle);
                    }
                    // Main.Logger.Debug($"Removing vehicle {v.ID}. Reason:{reason}");
                    veh.AttachedBlip?.Delete();
                    veh.Model.MarkAsNoLongerNeeded();
                    veh.MarkAsNoLongerNeeded();
                    veh.Delete();
                }
                ID_Vehicles.Remove(id);
                if (v.IsLocal) { API.Events.InvokeVehicleDeleted(v); }
            }
        }

        #endregion

        #region PROJECTILES
        public static SyncedProjectile GetProjectileByID(int id)
        {
            return ID_Projectiles.ContainsKey(id) ? ID_Projectiles[id] : null;
        }
        public static void Add(SyncedProjectile p)
        {
            if (!p.IsValid) { return; }
            if (ID_Projectiles.ContainsKey(p.ID))
            {
                ID_Projectiles[p.ID]=p;
            }
            else
            {
                ID_Projectiles.Add(p.ID, p);
            }
            if (p.MainProjectile==null) { return; }
            if (Handle_Projectiles.ContainsKey(p.MainProjectile.Handle))
            {
                Handle_Projectiles[p.MainProjectile.Handle]=p;
            }
            else
            {
                Handle_Projectiles.Add(p.MainProjectile.Handle, p);
            }
        }
        public static void RemoveProjectile(int id, string reason)
        {
            if (ID_Projectiles.ContainsKey(id))
            {
                SyncedProjectile sp = ID_Projectiles[id];
                var p = sp.MainProjectile;
                if (p!=null)
                {
                    if (Handle_Projectiles.ContainsKey(p.Handle))
                    {
                        Handle_Projectiles.Remove(p.Handle);
                    }
                    Main.Logger.Debug($"Removing projectile {sp.ID}. Reason:{reason}");
                    p.Explode();
                }
                ID_Projectiles.Remove(id);
            }
        }

        public static bool PedExists(int id)
        {
            return ID_Peds.ContainsKey(id);
        }
        public static bool VehicleExists(int id)
        {
            return ID_Vehicles.ContainsKey(id);
        }
        public static bool ProjectileExists(int id)
        {
            return ID_Projectiles.ContainsKey(id);
        }
        #endregion
        static int vehStateIndex;
        static int pedStateIndex;
        static int vehStatesPerFrame;
        static int pedStatesPerFrame;
        static int i;
        public static Ped[] allPeds=new Ped[0];
        public static Vehicle[] allVehicles=new Vehicle[0];
        public static Projectile[] allProjectiles=new Projectile[0];

        public static void DoSync()
        {
#if BENCHMARK
            PerfCounter.Restart();
            Debug.TimeStamps[TimeStamp.CheckProjectiles]=PerfCounter.ElapsedTicks;
#endif
            allPeds = World.GetAllPeds();
            allVehicles=World.GetAllVehicles();
            allProjectiles=World.GetAllProjectiles();
            vehStatesPerFrame=allVehicles.Length*2/(int)Game.FPS+1;
            pedStatesPerFrame=allPeds.Length*2/(int)Game.FPS+1;
            /*
            if (Main.Ticked%50==0)
            {
                bool flag1 = allVehicles.Length>Main.Settings.WorldVehicleSoftLimit && Main.Settings.WorldVehicleSoftLimit>-1;
                bool flag2 = allPeds.Length>Main.Settings.WorldPedSoftLimit && Main.Settings.WorldPedSoftLimit>-1;
                if ((flag1||flag2) && _trafficSpawning)
                { SetBudget(0); _trafficSpawning=false; }
                else if(!_trafficSpawning)
                { SetBudget(1); _trafficSpawning=true; }
            }
            */
#if BENCHMARK

            Debug.TimeStamps[TimeStamp.GetAllEntities]=PerfCounter.ElapsedTicks;
#endif

            lock (ProjectilesLock)
            {

                foreach (Projectile p in allProjectiles)
                {
                    if (!Handle_Projectiles.ContainsKey(p.Handle))
                    {
                        Add(new SyncedProjectile(p));
                        
                    }
                }

                foreach (SyncedProjectile p in ID_Projectiles.Values.ToArray())
                {

                    // Outgoing sync
                    if (p.IsLocal)
                    {
                        if (p.MainProjectile.AttachedEntity==null)
                        {

                            // Prevent projectiles from exploding next to vehicle
                            if (WeaponUtil.VehicleProjectileWeapons.Contains((VehicleWeaponHash)p.MainProjectile.WeaponHash))
                            {
                                if (p.MainProjectile.WeaponHash!=(WeaponHash)VehicleWeaponHash.Tank && p.Origin.DistanceTo(p.MainProjectile.Position)<2)
                                {
                                    continue;
                                }
                            }

                            Networking.SendProjectile(p);
                        }
                    }
                    else // Incoming sync
                    {
                        if (p.Exploded || p.IsOutOfSync)
                        {
                            RemoveProjectile(p.ID, "OutOfSync | Exploded");
                        }
                        else
                        {
                            p.Update();
                        }

                    }

                }

            }
            
            i=-1;

            lock (PedsLock)
            {
                EntityPool.AddPlayer();

                foreach (Ped p in allPeds)
                {
                    SyncedPed c = EntityPool.GetPedByHandle(p.Handle);
                    if (c==null && (p!=Game.Player.Character))
                    {
                        if (allPeds.Length>Main.Settings.WorldPedSoftLimit)
                        {
                            if (p.PopulationType!=EntityPopulationType.RandomAmbient)
                            {
                                p.Delete();
                                continue;
                            }
                        }
                        // Main.Logger.Trace($"Creating SyncEntity for ped, handle:{p.Handle}");
                        c=new SyncedPed(p);

                        EntityPool.Add(c);


                    }
                }
#if BENCHMARK

                Debug.TimeStamps[TimeStamp.AddPeds]=PerfCounter.ElapsedTicks;
#endif
                var ps = ID_Peds.Values.ToArray();
                pedStateIndex+=pedStatesPerFrame;
                if (pedStateIndex>=ps.Length)
                {
                    pedStateIndex=0;
                }
                foreach (SyncedPed c in ps)
                {
                    i++;
                    if ((c.MainPed!=null)&&(!c.MainPed.Exists()))
                    {
                        EntityPool.RemovePed(c.ID, "non-existent");
                        continue;
                    }

                    // Outgoing sync
                    if (c.IsLocal)
                    {
#if BENCHMARK
                        var start = PerfCounter2.ElapsedTicks;
#endif
                        // event check
                        SyncEvents.Check(c);

                        Networking.SendPed(c, (i-pedStateIndex)<pedStatesPerFrame);
#if BENCHMARK

                        Debug.TimeStamps[TimeStamp.SendPed]=PerfCounter2.ElapsedTicks-start;
#endif                        
                    }
                    else // Incoming sync
                    {
#if BENCHMARK
                        var start = PerfCounter2.ElapsedTicks;
#endif
                        c.Update();
                        if (c.IsOutOfSync)
                        {
                            RemovePed(c.ID, "OutOfSync");
                        }
#if BENCHMARK

                        Debug.TimeStamps[TimeStamp.UpdatePed]=PerfCounter2.ElapsedTicks-start;
#endif
                    }
                }
#if BENCHMARK

                Debug.TimeStamps[TimeStamp.PedTotal]=PerfCounter.ElapsedTicks;
#endif
            }

            i=-1;

            lock (VehiclesLock)
            {

                foreach (Vehicle veh in allVehicles)
                {
                    if (!Handle_Vehicles.ContainsKey(veh.Handle))
                    {
                        if (allVehicles.Length>Main.Settings.WorldVehicleSoftLimit)
                        {
                            var type = veh.PopulationType;
                            if (type==EntityPopulationType.RandomAmbient || type==EntityPopulationType.RandomParked)
                            {
                                foreach(var p in veh.Occupants)
                                {
                                    p.Delete();
                                    var c = EntityPool.GetPedByHandle(p.Handle);
                                    if (c!=null)
                                    {
                                        EntityPool.RemovePed(c.ID,"ThrottleTraffic");
                                    }
                                }
                                veh.Delete();
                                continue;
                            }
                        }
                        // Main.Logger.Debug($"Creating SyncEntity for vehicle, handle:{veh.Handle}");

                        EntityPool.Add(new SyncedVehicle(veh));


                    }
                }
#if BENCHMARK

                Debug.TimeStamps[TimeStamp.AddVehicles]=PerfCounter.ElapsedTicks;
#endif
                var vs = ID_Vehicles.Values.ToArray();
                vehStateIndex+=vehStatesPerFrame;
                if (vehStateIndex>=vs.Length)
                {
                    vehStateIndex=0;
                }
                foreach (SyncedVehicle v in vs)
                {
                    i++;
                    if ((v.MainVehicle!=null)&&(!v.MainVehicle.Exists()))
                    {
                        EntityPool.RemoveVehicle(v.ID,"non-existent");
                        continue;
                    }

                    // Outgoing sync
                    if (v.IsLocal)
                    {
                        if (!v.MainVehicle.IsVisible) { continue; }
                        SyncEvents.Check(v);

                        Networking.SendVehicle(v, (i-vehStateIndex)<vehStatesPerFrame);

                    }
                    else // Incoming sync
                    {


                        v.Update();
                        if (v.IsOutOfSync)
                        {
                            RemoveVehicle(v.ID, "OutOfSync");
                        }

                    }

                }

#if BENCHMARK
                Debug.TimeStamps[TimeStamp.VehicleTotal]=PerfCounter.ElapsedTicks;
#endif
            }


        }


        public static void RemoveAllFromPlayer(int playerPedId)
        {
            foreach(SyncedPed p in ID_Peds.Values.ToArray())
            {
                if (p.OwnerID==playerPedId)
                {
                    RemovePed(p.ID);
                }
            }
            foreach (SyncedVehicle v in ID_Vehicles.Values.ToArray())
            {
                if (v.OwnerID==playerPedId)
                {
                    RemoveVehicle(v.ID);
                }
            }
        }

        public static int RequestNewID()
        {
            int ID=0;
            while ((ID==0) 
                || ID_Peds.ContainsKey(ID) 
                || ID_Vehicles.ContainsKey(ID) 
                || ID_Projectiles.ContainsKey(ID))
            {
                byte[] rngBytes = new byte[4];

                RandomNumberGenerator.Create().GetBytes(rngBytes);

                // Convert the bytes into an integer
                ID = BitConverter.ToInt32(rngBytes, 0);
            }
            return ID;
        }
        private static void SetBudget(int b)
        {
            Function.Call(Hash.SET_PED_POPULATION_BUDGET, b); // 0 - 3
            Function.Call(Hash.SET_VEHICLE_POPULATION_BUDGET, b); // 0 - 3
        }
        public static string DumpDebug()
        {
            string s= "";
            s+="\nID_Peds: "+ID_Peds.Count;
            s+="\nHandle_Peds: "+Handle_Peds.Count;
            s+="\nID_Vehicles: "+ID_Vehicles.Count;
            s+="\nHandle_Vehicles: "+Handle_Vehicles.Count;
            s+="\nID_Projectiles: "+ID_Projectiles.Count;
            s+="\nHandle_Projectiles: "+Handle_Projectiles.Count;
            s+="\npedStatesPerFrame:"+pedStatesPerFrame;
            s+="\nvehStatesPerFrame:"+vehStatesPerFrame;
            return s;
        }
        public static class ThreadSafe
        {
            public static void Add(SyncedVehicle v)
            {
                lock (EntityPool.VehiclesLock)
                {
                    EntityPool.Add(v);
                }
            }
            public static void Add(SyncedPed p)
            {
                lock (EntityPool.PedsLock)
                {
                    EntityPool.Add(p);
                }
            }
            public static void Add(SyncedProjectile sp)
            {
                lock (EntityPool.ProjectilesLock)
                {
                    EntityPool.Add(sp);
                }
            }
        }
    }
}
