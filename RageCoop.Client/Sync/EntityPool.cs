using GTA;
using GTA.Native;
using Lidgren.Network;
using RageCoop.Client.Scripting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;

namespace RageCoop.Client
{
    internal class EntityPool
    {
        public static object PedsLock = new object();
#if BENCHMARK
        private static Stopwatch PerfCounter=new Stopwatch();
        private static Stopwatch PerfCounter2=Stopwatch.StartNew();
#endif
        #region ACTIVE INSTANCES

        public static Dictionary<int, SyncedPed> PedsByID = new Dictionary<int, SyncedPed>();
        public static Dictionary<int, SyncedPed> PedsByHandle = new Dictionary<int, SyncedPed>();


        public static Dictionary<int, SyncedVehicle> VehiclesByID = new Dictionary<int, SyncedVehicle>();
        public static Dictionary<int, SyncedVehicle> VehiclesByHandle = new Dictionary<int, SyncedVehicle>();

        public static Dictionary<int, SyncedProjectile> ProjectilesByID = new Dictionary<int, SyncedProjectile>();
        public static Dictionary<int, SyncedProjectile> ProjectilesByHandle = new Dictionary<int, SyncedProjectile>();

        public static Dictionary<int, SyncedProp> ServerProps = new Dictionary<int, SyncedProp>();
        public static Dictionary<int, Blip> ServerBlips = new Dictionary<int, Blip>();

        #endregion

        #region LOCKS

        public static object VehiclesLock = new object();
        public static object ProjectilesLock = new object();
        public static object PropsLock = new object();
        public static object BlipsLock = new object();

        #endregion
        public static void Cleanup(bool keepPlayer = true, bool keepMine = true)
        {
            foreach (var ped in PedsByID.Values.ToArray())
            {
                if ((keepPlayer && (ped.ID == Main.LocalPlayerID)) || (keepMine && (ped.OwnerID == Main.LocalPlayerID))) { continue; }
                RemovePed(ped.ID);
            }
            PedsByID.Clear();
            PedsByHandle.Clear();

            foreach (int id in VehiclesByID.Keys.ToArray())
            {
                if (keepMine && (VehiclesByID[id].OwnerID == Main.LocalPlayerID)) { continue; }
                RemoveVehicle(id);
            }
            VehiclesByID.Clear();
            VehiclesByHandle.Clear();

            foreach (var p in ProjectilesByID.Values)
            {
                if (p.Shooter.ID != Main.LocalPlayerID && p.MainProjectile != null && p.MainProjectile.Exists())
                {
                    p.MainProjectile.Delete();
                }
            }
            ProjectilesByID.Clear();
            ProjectilesByHandle.Clear();

            foreach (var p in ServerProps.Values)
            {
                p?.MainProp?.Delete();
            }
            ServerProps.Clear();

            foreach (var b in ServerBlips.Values)
            {
                if (b.Exists())
                {
                    b.Delete();
                }
            }
            ServerBlips.Clear();
        }

        #region PEDS
        public static SyncedPed GetPedByID(int id) => PedsByID.TryGetValue(id, out var p) ? p : null;
        public static SyncedPed GetPedByHandle(int handle) => PedsByHandle.TryGetValue(handle, out var p) ? p : null;
        public static List<int> GetPedIDs() => new List<int>(PedsByID.Keys);
        public static bool AddPlayer()
        {
            Ped p = Game.Player.Character;
            // var clipset=p.Gender==Gender.Male? "MOVE_M@TOUGH_GUY@" : "MOVE_F@TOUGH_GUY@";
            // Function.Call(Hash.SET_PED_MOVEMENT_CLIPSET,p,clipset,1f);
            SyncedPed player = GetPedByID(Main.LocalPlayerID);
            if (player == null)
            {
                Main.Logger.Debug($"Creating SyncEntity for player, handle:{p.Handle}");
                SyncedPed c = new SyncedPed(p);
                Main.LocalPlayerID = c.OwnerID = c.ID;
                Add(c);
                Main.Logger.Debug($"Local player ID is:{c.ID}");
                PlayerList.SetPlayer(c.ID, Main.Settings.Username);
                return true;
            }

            if (player.MainPed != p)
            {
                // Player model changed
                player.MainPed = p;

                // Remove it from Handle_Characters
                var pairs = PedsByHandle.Where(x => x.Value == player);
                if (pairs.Any())
                {
                    var pair = pairs.First();

                    // Re-add
                    PedsByHandle.Remove(pair.Key);
                    if (PedsByHandle.ContainsKey(p.Handle))
                    {
                        RemovePed(PedsByHandle[p.Handle].ID);
                    }
                    PedsByHandle.Add(p.Handle, player);
                }
            }
            return false;
        }
        public static void Add(SyncedPed c)
        {
            if (PedsByID.ContainsKey(c.ID))
            {
                PedsByID[c.ID] = c;
            }
            else
            {
                PedsByID.Add(c.ID, c);
            }
            if (c.MainPed == null) { return; }
            if (PedsByHandle.ContainsKey(c.MainPed.Handle))
            {
                PedsByHandle[c.MainPed.Handle] = c;
            }
            else
            {
                PedsByHandle.Add(c.MainPed.Handle, c);
            }
            if (c.IsLocal)
            {
                API.Events.InvokePedSpawned(c);
            }
        }
        public static void RemovePed(int id, string reason = "Cleanup")
        {
            if (PedsByID.ContainsKey(id))
            {
                SyncedPed c = PedsByID[id];
                var p = c.MainPed;
                if (p != null)
                {
                    if (PedsByHandle.ContainsKey(p.Handle))
                    {
                        PedsByHandle.Remove(p.Handle);
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
                PedsByID.Remove(id);
                if (c.IsLocal)
                {
                    API.Events.InvokePedDeleted(c);
                }
            }
        }
        #endregion

        #region VEHICLES
        public static SyncedVehicle GetVehicleByID(int id) => VehiclesByID.TryGetValue(id, out var v) ? v : null;
        public static SyncedVehicle GetVehicleByHandle(int handle) => VehiclesByHandle.TryGetValue(handle, out var v) ? v : null;
        public static List<int> GetVehicleIDs() => new List<int>(VehiclesByID.Keys);
        public static void Add(SyncedVehicle v)
        {
            if (VehiclesByID.ContainsKey(v.ID))
            {
                VehiclesByID[v.ID] = v;
            }
            else
            {
                VehiclesByID.Add(v.ID, v);
            }
            if (v.MainVehicle == null) { return; }
            if (VehiclesByHandle.ContainsKey(v.MainVehicle.Handle))
            {
                VehiclesByHandle[v.MainVehicle.Handle] = v;
            }
            else
            {
                VehiclesByHandle.Add(v.MainVehicle.Handle, v);
            }
            if (v.IsLocal)
            {
                API.Events.InvokeVehicleSpawned(v);
            }
        }
        public static void RemoveVehicle(int id, string reason = "Cleanup")
        {
            if (VehiclesByID.ContainsKey(id))
            {
                SyncedVehicle v = VehiclesByID[id];
                var veh = v.MainVehicle;
                if (veh != null)
                {
                    if (VehiclesByHandle.ContainsKey(veh.Handle))
                    {
                        VehiclesByHandle.Remove(veh.Handle);
                    }
                    // Main.Logger.Debug($"Removing vehicle {v.ID}. Reason:{reason}");
                    veh.AttachedBlip?.Delete();
                    veh.Model.MarkAsNoLongerNeeded();
                    veh.MarkAsNoLongerNeeded();
                    veh.Delete();
                }
                VehiclesByID.Remove(id);
                if (v.IsLocal) { API.Events.InvokeVehicleDeleted(v); }
            }
        }

        #endregion

        #region PROJECTILES
        public static SyncedProjectile GetProjectileByID(int id)
        {
            return ProjectilesByID.TryGetValue(id, out var p) ? p : null;
        }
        public static void Add(SyncedProjectile p)
        {
            if (!p.IsValid) { return; }
            if (p.WeaponHash == (WeaponHash)VehicleWeaponHash.Tank)
            {
                Networking.SendBullet(p.Position, p.Position + p.Velocity, (uint)VehicleWeaponHash.Tank, ((SyncedVehicle)p.Shooter).MainVehicle.Driver.GetSyncEntity().ID);
            }
            if (ProjectilesByID.ContainsKey(p.ID))
            {
                ProjectilesByID[p.ID] = p;
            }
            else
            {
                ProjectilesByID.Add(p.ID, p);
            }
            if (p.MainProjectile == null) { return; }
            if (ProjectilesByHandle.ContainsKey(p.MainProjectile.Handle))
            {
                ProjectilesByHandle[p.MainProjectile.Handle] = p;
            }
            else
            {
                ProjectilesByHandle.Add(p.MainProjectile.Handle, p);
            }
        }
        public static void RemoveProjectile(int id, string reason)
        {
            if (ProjectilesByID.ContainsKey(id))
            {
                SyncedProjectile sp = ProjectilesByID[id];
                var p = sp.MainProjectile;
                if (p != null)
                {
                    if (ProjectilesByHandle.ContainsKey(p.Handle))
                    {
                        ProjectilesByHandle.Remove(p.Handle);
                    }
                    //Main.Logger.Debug($"Removing projectile {sp.ID}. Reason:{reason}");
                    if (sp.Exploded) p.Explode();
                }
                ProjectilesByID.Remove(id);
            }
        }

        public static bool PedExists(int id) => PedsByID.ContainsKey(id);
        public static bool VehicleExists(int id) => VehiclesByID.ContainsKey(id);
        public static bool ProjectileExists(int id) => ProjectilesByID.ContainsKey(id);
        #endregion
        private static int vehStateIndex;
        private static int pedStateIndex;
        private static int vehStatesPerFrame;
        private static int pedStatesPerFrame;
        private static int i;
        public static Ped[] allPeds = new Ped[0];
        public static Vehicle[] allVehicles = new Vehicle[0];
        public static Projectile[] allProjectiles = new Projectile[0];

        public static void DoSync()
        {
            UpdateTargets();
#if BENCHMARK
            PerfCounter.Restart();
            Debug.TimeStamps[TimeStamp.CheckProjectiles]=PerfCounter.ElapsedTicks;
#endif
            allPeds = World.GetAllPeds();
            allVehicles = World.GetAllVehicles();
            allProjectiles = World.GetAllProjectiles();
            vehStatesPerFrame = allVehicles.Length * 2 / (int)Game.FPS + 1;
            pedStatesPerFrame = allPeds.Length * 2 / (int)Game.FPS + 1;
#if BENCHMARK

            Debug.TimeStamps[TimeStamp.GetAllEntities]=PerfCounter.ElapsedTicks;
#endif

            lock (ProjectilesLock)
            {

                foreach (Projectile p in allProjectiles)
                {
                    if (!ProjectilesByHandle.ContainsKey(p.Handle))
                    {
                        Add(new SyncedProjectile(p));
                    }
                }

                foreach (SyncedProjectile p in ProjectilesByID.Values.ToArray())
                {
                    // Outgoing sync
                    if (p.IsLocal)
                    {
                        if (p.MainProjectile.AttachedEntity == null)
                        {
                            // Prevent projectiles from exploding next to vehicle
                            if (p.WeaponHash == (WeaponHash)VehicleWeaponHash.Tank || (p.MainProjectile.OwnerEntity?.EntityType == EntityType.Vehicle && p.MainProjectile.Position.DistanceTo(p.Origin) < 2))
                            {
                                continue;
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

            i = -1;

            lock (PedsLock)
            {
                AddPlayer();
                var mainCharacters = new List<PedHash> { PedHash.Michael, PedHash.Franklin, PedHash.Franklin02, PedHash.Trevor };

                foreach (Ped p in allPeds)
                {
                    if (!PedsByHandle.ContainsKey(p.Handle) && p != Game.Player.Character && !mainCharacters.Contains((PedHash)p.Model.Hash))
                    {
                        if (PedsByID.Count(x => x.Value.IsLocal) > Main.Settings.WorldPedSoftLimit)
                        {
                            if (p.PopulationType == EntityPopulationType.RandomAmbient && !p.IsInVehicle())
                            {
                                p.Delete();
                                continue;
                            }
                            if (p.PopulationType == EntityPopulationType.RandomScenario) continue;
                        }
                        // Main.Logger.Trace($"Creating SyncEntity for ped, handle:{p.Handle}");

                        Add(new SyncedPed(p));
                    }
                }
#if BENCHMARK

                Debug.TimeStamps[TimeStamp.AddPeds]=PerfCounter.ElapsedTicks;
#endif
                var ps = PedsByID.Values.ToArray();
                pedStateIndex += pedStatesPerFrame;
                if (pedStateIndex >= ps.Length)
                {
                    pedStateIndex = 0;
                }

                foreach (SyncedPed c in ps)
                {
                    i++;
                    if ((c.MainPed != null) && (!c.MainPed.Exists()))
                    {
                        RemovePed(c.ID, "non-existent");
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

                        Networking.SendPed(c, (i - pedStateIndex) < pedStatesPerFrame);
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
            var check = Main.Ticked % 100 == 0;
            i = -1;
            lock (VehiclesLock)
            {
                foreach (Vehicle veh in allVehicles)
                {
                    if (!VehiclesByHandle.ContainsKey(veh.Handle))
                    {
                        if (VehiclesByID.Count(x => x.Value.IsLocal) > Main.Settings.WorldVehicleSoftLimit)
                        {
                            if (veh.PopulationType == EntityPopulationType.RandomAmbient || veh.PopulationType == EntityPopulationType.RandomParked)
                            {
                                foreach (var p in veh.Occupants)
                                {
                                    p.Delete();
                                    var c = GetPedByHandle(p.Handle);
                                    if (c != null)
                                    {
                                        RemovePed(c.ID, "ThrottleTraffic");
                                    }
                                }
                                veh.Delete();
                                continue;
                            }
                        }
                        // Main.Logger.Debug($"Creating SyncEntity for vehicle, handle:{veh.Handle}");

                        Add(new SyncedVehicle(veh));
                    }
                }
#if BENCHMARK
                Debug.TimeStamps[TimeStamp.AddVehicles]=PerfCounter.ElapsedTicks;
#endif
                var vs = VehiclesByID.Values.ToArray();
                vehStateIndex += vehStatesPerFrame;
                if (vehStateIndex >= vs.Length)
                {
                    vehStateIndex = 0;
                }

                foreach (SyncedVehicle v in vs)
                {
                    i++;
                    if ((v.MainVehicle != null) && (!v.MainVehicle.Exists()))
                    {
                        RemoveVehicle(v.ID, "non-existent");
                        continue;
                    }
                    if (check)
                    {
                        v.SetUpFixedData();
                    }
                    // Outgoing sync
                    if (v.IsLocal)
                    {
                        if (!v.MainVehicle.IsVisible) { continue; }
                        SyncEvents.Check(v);

                        Networking.SendVehicle(v, (i - vehStateIndex) < vehStatesPerFrame);
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
            Networking.Peer.FlushSendQueue();
        }

        private static void UpdateTargets()
        {
            Networking.Targets = new List<NetConnection>(PlayerList.Players.Count) { Networking.ServerConnection };
            foreach (var p in PlayerList.Players.Values.ToArray())
            {
                if (p.HasDirectConnection && p.Position.DistanceTo(Main.PlayerPosition) < 500)
                {
                    Networking.Targets.Add(p.Connection);
                }
            }
        }

        public static void RemoveAllFromPlayer(int playerPedId)
        {
            foreach (SyncedPed p in PedsByID.Values.ToArray())
            {
                if (p.OwnerID == playerPedId)
                {
                    RemovePed(p.ID);
                }
            }

            foreach (SyncedVehicle v in VehiclesByID.Values.ToArray())
            {
                if (v.OwnerID == playerPedId)
                {
                    RemoveVehicle(v.ID);
                }
            }
        }

        public static int RequestNewID()
        {
            int ID = 0;
            while ((ID == 0) || PedsByID.ContainsKey(ID) || VehiclesByID.ContainsKey(ID) || ProjectilesByID.ContainsKey(ID))
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
            return $"\nID_Peds: {PedsByID.Count}" +
                   $"\nHandle_Peds: {PedsByHandle.Count}" +
                   $"\nID_Vehicles: {VehiclesByID.Count}" +
                   $"\nHandle_vehicles: {VehiclesByHandle.Count}" +
                   $"\nID_Projectiles: {ProjectilesByID.Count}" +
                   $"\nHandle_Projectiles: {ProjectilesByHandle.Count}" +
                   $"\npedStatesPerFrame: {pedStatesPerFrame}" +
                   $"\nvehStatesPerFrame: {vehStatesPerFrame}";
        }
        public static class ThreadSafe
        {
            public static void Add(SyncedVehicle v)
            {
                lock (VehiclesLock)
                {
                    EntityPool.Add(v);
                }
            }
            public static void Add(SyncedPed p)
            {
                lock (PedsLock)
                {
                    EntityPool.Add(p);
                }
            }
            public static void Add(SyncedProjectile sp)
            {
                lock (ProjectilesLock)
                {
                    EntityPool.Add(sp);
                }
            }
        }
    }
}
