using System;
using GTA;
using GTA.Native;
using RageCoop.Core;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Threading.Tasks;

namespace RageCoop.Client
{
    internal class EntityPool
    {
        public static object PedsLock = new object();
        private static Dictionary<int, SyncedPed> ID_Peds = new Dictionary<int, SyncedPed>();
        public static int CharactersCount { get { return ID_Peds.Count; } }
        private static Stopwatch PerfCounter=new Stopwatch();
        /// <summary>
        /// Faster access to Character with Handle, but values may not equal to <see cref="ID_Peds"/> since Ped might not have been created.
        /// </summary>
        private static Dictionary<int, SyncedPed> Handle_Peds = new Dictionary<int, SyncedPed>();


        public static object VehiclesLock = new object();
        private static Dictionary<int, SyncedVehicle> ID_Vehicles = new Dictionary<int, SyncedVehicle>();
        private static Dictionary<int, SyncedVehicle> Handle_Vehicles = new Dictionary<int, SyncedVehicle>();

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
        }
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
                Main.Logger.Debug($"My player ID is:{c.ID}");
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
                    Main.Logger.Debug($"Removing ped {c.ID}. Reason:{reason}");
                    p.AttachedBlip?.Delete();
                    p.Kill();
                    p.Delete();
                }
                c.PedBlip?.Delete();
                c.ParachuteProp?.Delete();
                ID_Peds.Remove(id);
            }
        }


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
                    Main.Logger.Debug($"Removing vehicle {v.ID}. Reason:{reason}");
                    veh.AttachedBlip?.Delete();
                    veh.Delete();
                }
                ID_Vehicles.Remove(id);
            }
        }

        public static bool Exists(int id)
        {
            return ID_Peds.ContainsKey(id) || ID_Vehicles.ContainsKey(id);
        }
        public static void DoSync()
        {
            PerfCounter.Restart();
            SyncEvents.CheckProjectiles();
            Debug.TimeStamps[TimeStamp.CheckProjectiles]=PerfCounter.ElapsedTicks;
            var allPeds = World.GetAllPeds();
            var allVehicles=World.GetAllVehicles();
            if (Main.Ticked%100==0) { if (allVehicles.Length>50) { SetBudget(0); } else { SetBudget(1); } }
            Debug.TimeStamps[TimeStamp.GetAllEntities]=PerfCounter.ElapsedTicks;

            lock (EntityPool.PedsLock)
            {
                EntityPool.AddPlayer();

                foreach (Ped p in allPeds)
                {
                    SyncedPed c = EntityPool.GetPedByHandle(p.Handle);
                    if (c==null && (p!=Game.Player.Character))
                    {
                        Main.Logger.Trace($"Creating SyncEntity for ped, handle:{p.Handle}");
                        c=new SyncedPed(p);

                        EntityPool.Add(c);


                    }
                }
                Debug.TimeStamps[TimeStamp.AddPeds]=PerfCounter.ElapsedTicks;


                foreach (SyncedPed c in ID_Peds.Values.ToArray())
                {
                    if ((c.MainPed!=null)&&(!c.MainPed.Exists()))
                    {
                        EntityPool.RemovePed(c.ID, "non-existent");
                        continue;
                    }

                    // Outgoing sync
                    if (c.IsMine)
                    {
                        
                        // event check
                        SyncEvents.Check(c);

                        if (Main.Ticked%20==0)
                        {
                            Networking.SendPedState(c);
                        }
                        else
                        {
                            Networking.SendPed(c);
                        }
                        
                    }
                    else // Incoming sync
                    {
                        

                        c.Update();
                        if (c.IsOutOfSync)
                        {
                            try
                            {
                                EntityPool.RemovePed(c.ID,"OutOfSync");
                            }
                            catch { }
                        }

                    }
                }
                Debug.TimeStamps[TimeStamp.PedTotal]=PerfCounter.ElapsedTicks;

            }
            lock (EntityPool.VehiclesLock)
            {

                foreach (Vehicle veh in allVehicles)
                {
                    SyncedVehicle v = EntityPool.GetVehicleByHandle(veh.Handle);
                    if (v==null)
                    {
                        Main.Logger.Debug($"Creating SyncEntity for vehicle, handle:{veh.Handle}");

                        EntityPool.Add(new SyncedVehicle(veh));


                    }
                }
                Debug.TimeStamps[TimeStamp.AddVehicles]=PerfCounter.ElapsedTicks;

                foreach (SyncedVehicle v in ID_Vehicles.Values.ToArray())
                {
                    if ((v.MainVehicle!=null)&&(!v.MainVehicle.Exists()))
                    {
                        EntityPool.RemoveVehicle(v.ID,"non-existent");
                        continue;
                    }

                    // Outgoing sync
                    if (v.IsMine)
                    {
                        if (Main.Ticked%20==0)
                        {
                            Networking.SendVehicleState(v);
                            
                        }
                        else
                        {
                            Networking.SendVehicle(v);
                        }

                    }
                    else // Incoming sync
                    {


                        v.Update();
                        if (v.IsOutOfSync)
                        {
                            try
                            {
                                EntityPool.RemoveVehicle(v.ID, "OutOfSync");
                            }
                            catch { }
                        }

                    }

                }
                
                Debug.TimeStamps[TimeStamp.VehicleTotal]=PerfCounter.ElapsedTicks;

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
        private static void SetBudget(int b)
        {
            Function.Call(Hash.SET_PED_POPULATION_BUDGET, b); // 0 - 3
            Function.Call(Hash.SET_VEHICLE_POPULATION_BUDGET, b); // 0 - 3
        }
    }
}
