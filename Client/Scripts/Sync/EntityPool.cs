using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Security.Cryptography;
using GTA;
using GTA.Native;
using Lidgren.Network;
using RageCoop.Client.Scripting;
using SHVDN;

namespace RageCoop.Client
{
    internal static unsafe class EntityPool
    {
        public static object PedsLock = new object();
#if BENCHMARK
        private static Stopwatch PerfCounter = new Stopwatch();
        private static Stopwatch PerfCounter2 = Stopwatch.StartNew();
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
                if ((keepPlayer && ped.ID == Main.LocalPlayerID) ||
                    (keepMine && ped.OwnerID == Main.LocalPlayerID)) continue;
                RemovePed(ped.ID);
            }

            PedsByID.Clear();
            PedsByHandle.Clear();

            foreach (var id in VehiclesByID.Keys.ToArray())
            {
                if (keepMine && VehiclesByID[id].OwnerID == Main.LocalPlayerID) continue;
                RemoveVehicle(id);
            }

            VehiclesByID.Clear();
            VehiclesByHandle.Clear();

            foreach (var p in ProjectilesByID.Values.ToArray())
                if (p.Shooter.ID != Main.LocalPlayerID && p.MainProjectile != null && p.MainProjectile.Exists())
                    p.MainProjectile.Delete();
            ProjectilesByID.Clear();
            ProjectilesByHandle.Clear();

            foreach (var p in ServerProps.Values) p?.MainProp?.Delete();
            ServerProps.Clear();

            foreach (var b in ServerBlips.Values)
                if (b.Exists())
                    b.Delete();
            ServerBlips.Clear();
        }

        #region PEDS

        public static SyncedPed GetPedByID(int id)
            => PedsByID.TryGetValue(id, out var p) ? p : null;

        public static SyncedPed GetPedByHandle(int handle)
            => PedsByHandle.TryGetValue(handle, out var p) ? p : null;

        public static List<int> GetPedIDs()
        {
            return new List<int>(PedsByID.Keys);
        }

        public static bool AddPlayer()
        {
            var p = Game.Player.Character;
            // var clipset=p.Gender==Gender.Male? "MOVE_M@TOUGH_GUY@" : "MOVE_F@TOUGH_GUY@";
            // Call(SET_PED_MOVEMENT_CLIPSET,p,clipset,1f);
            var player = GetPedByID(Main.LocalPlayerID);
            if (player == null)
            {
                Main.Logger.Debug($"Creating SyncEntity for player, handle:{p.Handle}");
                var c = new SyncedPed(p);
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
                    if (PedsByHandle.ContainsKey(p.Handle)) RemovePed(PedsByHandle[p.Handle].ID);
                    PedsByHandle.Add(p.Handle, player);
                }
            }

            return false;
        }

        public static void Add(SyncedPed c)
        {
            if (PedsByID.ContainsKey(c.ID))
                PedsByID[c.ID] = c;
            else
                PedsByID.Add(c.ID, c);
            if (c.MainPed == null) return;
            if (PedsByHandle.ContainsKey(c.MainPed.Handle))
                PedsByHandle[c.MainPed.Handle] = c;
            else
                PedsByHandle.Add(c.MainPed.Handle, c);
            if (c.IsLocal) API.Events.InvokePedSpawned(c);
        }

        public static void RemovePed(int id, string reason = "Cleanup")
        {
            if (PedsByID.ContainsKey(id))
            {
                var c = PedsByID[id];
                var p = c.MainPed;
                if (p != null)
                {
                    if (PedsByHandle.ContainsKey(p.Handle)) PedsByHandle.Remove(p.Handle);
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
                if (c.IsLocal) API.Events.InvokePedDeleted(c);
            }
        }

        #endregion

        #region VEHICLES

        public static SyncedVehicle GetVehicleByID(int id)
            => VehiclesByID.TryGetValue(id, out var v) ? v : null;

        public static SyncedVehicle GetVehicleByHandle(int handle)
            => VehiclesByHandle.TryGetValue(handle, out var v) ? v : null;

        public static List<int> GetVehicleIDs()
        {
            return new List<int>(VehiclesByID.Keys);
        }

        public static void Add(SyncedVehicle v)
        {
            if (VehiclesByID.ContainsKey(v.ID))
                VehiclesByID[v.ID] = v;
            else
                VehiclesByID.Add(v.ID, v);
            if (v.MainVehicle == null) return;
            if (VehiclesByHandle.ContainsKey(v.MainVehicle.Handle))
                VehiclesByHandle[v.MainVehicle.Handle] = v;
            else
                VehiclesByHandle.Add(v.MainVehicle.Handle, v);
            if (v.IsLocal) API.Events.InvokeVehicleSpawned(v);
        }

        public static void RemoveVehicle(int id, string reason = "Cleanup")
        {
            if (VehiclesByID.ContainsKey(id))
            {
                var v = VehiclesByID[id];
                var veh = v.MainVehicle;
                if (veh != null)
                {
                    if (VehiclesByHandle.ContainsKey(veh.Handle)) VehiclesByHandle.Remove(veh.Handle);
                    // Main.Logger.Debug($"Removing vehicle {v.ID}. Reason:{reason}");
                    veh.AttachedBlip?.Delete();
                    veh.Model.MarkAsNoLongerNeeded();
                    veh.MarkAsNoLongerNeeded();
                    veh.Delete();
                }

                VehiclesByID.Remove(id);
                if (v.IsLocal) API.Events.InvokeVehicleDeleted(v);
            }
        }

        #endregion

        #region PROJECTILES

        public static SyncedProjectile GetProjectileByID(int id)
            => ProjectilesByID.TryGetValue(id, out var p) ? p : null;

        public static void Add(SyncedProjectile p)
        {
            if (!p.IsValid) return;
            if (p.WeaponHash == (WeaponHash)VehicleWeaponHash.Tank)
            {
                Networking.SendBullet(((SyncedVehicle)p.Shooter).MainVehicle.Driver.GetSyncEntity().ID, (uint)VehicleWeaponHash.Tank, p.Position + p.Velocity);
                return;
            }
            if (ProjectilesByID.ContainsKey(p.ID))
                ProjectilesByID[p.ID] = p;
            else
                ProjectilesByID.Add(p.ID, p);
            if (p.MainProjectile == null) return;
            if (ProjectilesByHandle.ContainsKey(p.MainProjectile.Handle))
                ProjectilesByHandle[p.MainProjectile.Handle] = p;
            else
                ProjectilesByHandle.Add(p.MainProjectile.Handle, p);
        }

        public static void RemoveProjectile(int id, string reason)
        {
            if (ProjectilesByID.ContainsKey(id))
            {
                var sp = ProjectilesByID[id];
                var p = sp.MainProjectile;
                if (p != null)
                {
                    if (ProjectilesByHandle.ContainsKey(p.Handle)) ProjectilesByHandle.Remove(p.Handle);
                    Main.Logger.Debug($"Removing projectile {sp.ID}. Reason:{reason}");
                    p.Explode();
                }

                ProjectilesByID.Remove(id);
            }
        }

        public static bool PedExists(int id)
        {
            return PedsByID.ContainsKey(id);
        }

        public static bool VehicleExists(int id)
        {
            return VehiclesByID.ContainsKey(id);
        }

        public static bool ProjectileExists(int id)
        {
            return ProjectilesByID.ContainsKey(id);
        }

        #endregion

        #region SERVER OBJECTS
        
        public static SyncedProp GetPropByID(int id)
            => ServerProps.TryGetValue(id, out var p) ? p : null;

        public static Blip GetBlipByID(int id)
            => ServerBlips.TryGetValue(id, out var p) ? p : null;

        #endregion

        private static int vehStateIndex;
        private static int pedStateIndex;
        private static int vehStatesPerFrame;
        private static int pedStatesPerFrame;
        private static int i;

        public static void DoSync()
        {
            UpdateTargets();

#if BENCHMARK
            PerfCounter.Restart();
            Debug.TimeStamps[TimeStamp.CheckProjectiles] = PerfCounter.ElapsedTicks;
#endif

            var allPeds = NativeMemory.GetPedHandles();
            var allVehicles = NativeMemory.GetVehicleHandles();
            var allProjectiles = NativeMemory.GetProjectileHandles();
            vehStatesPerFrame = allVehicles.Length * 2 / (int)Main.FPS + 1;
            pedStatesPerFrame = allPeds.Length * 2 / (int)Main.FPS + 1;

#if BENCHMARK
            Debug.TimeStamps[TimeStamp.GetAllEntities] = PerfCounter.ElapsedTicks;
#endif


            lock (ProjectilesLock)
            {
                foreach (var p in allProjectiles)
                    if (!ProjectilesByHandle.ContainsKey(p))
                        Add(new SyncedProjectile(Projectile.FromHandle(p)));

                foreach (var p in ProjectilesByID.Values.ToArray())
                    // Outgoing sync
                    if (p.IsLocal)
                    {
                        if (p.MainProjectile.AttachedEntity == null)
                        {
                            // Prevent projectiles from exploding next to vehicle
                            if (p.WeaponHash == (WeaponHash)VehicleWeaponHash.Tank ||
                                (p.MainProjectile.OwnerEntity?.EntityType == EntityType.Vehicle &&
                                 p.MainProjectile.Position.DistanceTo(p.Origin) < 2)) continue;
                            Networking.SendProjectile(p);
                        }
                    }
                    else // Incoming sync
                    {
                        if (p.Exploded || p.IsOutOfSync)
                            RemoveProjectile(p.ID, "OutOfSync | Exploded");
                        else
                            p.Update();
                    }
            }


            i = -1;

            lock (PedsLock)
            {
                AddPlayer();

                foreach (var p in allPeds)
                {
                    var c = GetPedByHandle(p);
                    if (c == null && p != Game.Player.Character.Handle)
                    {
                        var type = Util.GetPopulationType(p);
                        if (allPeds.Length > Main.Settings.WorldPedSoftLimit &&
                            type == EntityPopulationType.RandomAmbient && !Call<bool>(IS_PED_IN_ANY_VEHICLE, p, 0))
                        {
                            Util.DeleteEntity(p);
                            continue;
                        }

                        // Main.Logger.Trace($"Creating SyncEntity for ped, handle:{p.Handle}");
                        c = new SyncedPed((Ped)Entity.FromHandle(p));

                        Add(c);
                    }
                }
#if BENCHMARK
                Debug.TimeStamps[TimeStamp.AddPeds] = PerfCounter.ElapsedTicks;
#endif
                var ps = PedsByID.Values.ToArray();
                pedStateIndex += pedStatesPerFrame;
                if (pedStateIndex >= ps.Length) pedStateIndex = 0;

                foreach (var c in ps)
                {
                    i++;
                    if (c.MainPed != null && !c.MainPed.Exists())
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

                        Networking.SendPed(c, i - pedStateIndex < pedStatesPerFrame);
#if BENCHMARK
                        Debug.TimeStamps[TimeStamp.SendPed] = PerfCounter2.ElapsedTicks-start;
#endif
                    }
                    else // Incoming sync
                    {
#if BENCHMARK
                        var start = PerfCounter2.ElapsedTicks;
#endif
                        c.Update();
                        if (c.IsOutOfSync) RemovePed(c.ID, "OutOfSync");
#if BENCHMARK
                        Debug.TimeStamps[TimeStamp.UpdatePed] = PerfCounter2.ElapsedTicks-start;
#endif
                    }
                }
#if BENCHMARK
                Debug.TimeStamps[TimeStamp.PedTotal] = PerfCounter.ElapsedTicks;
#endif
            }

            var check = Main.Ticked % 100 == 0;
            i = -1;
            lock (VehiclesLock)
            {
                foreach (var veh in allVehicles)
                    if (!VehiclesByHandle.ContainsKey(veh))
                    {
                        var cveh = (Vehicle)Entity.FromHandle(veh);
                        if (allVehicles.Length > Main.Settings.WorldVehicleSoftLimit)
                        {
                            var type = cveh.PopulationType;
                            if (type == EntityPopulationType.RandomAmbient || type == EntityPopulationType.RandomParked)
                            {
                                foreach (var p in cveh.Occupants)
                                {
                                    p.Delete();
                                    var c = GetPedByHandle(p.Handle);
                                    if (c != null) RemovePed(c.ID, "ThrottleTraffic");
                                }

                                cveh.Delete();
                                continue;
                            }
                        }
                        // Main.Logger.Debug($"Creating SyncEntity for vehicle, handle:{veh.Handle}");

                        Add(new SyncedVehicle(cveh));
                    }
#if BENCHMARK
                Debug.TimeStamps[TimeStamp.AddVehicles] = PerfCounter.ElapsedTicks;
#endif
                var vs = VehiclesByID.Values.ToArray();
                vehStateIndex += vehStatesPerFrame;
                if (vehStateIndex >= vs.Length) vehStateIndex = 0;

                foreach (var v in vs)
                {
                    i++;
                    if (v.MainVehicle != null && !v.MainVehicle.Exists())
                    {
                        RemoveVehicle(v.ID, "non-existent");
                        continue;
                    }

                    if (check) v.SetUpFixedData();
                    // Outgoing sync
                    if (v.IsLocal)
                    {
                        if (!v.MainVehicle.IsVisible) continue;
                        SyncEvents.Check(v);

                        Networking.SendVehicle(v, i - vehStateIndex < vehStatesPerFrame);
                    }
                    else // Incoming sync
                    {
                        v.Update();
                        if (v.IsOutOfSync) RemoveVehicle(v.ID, "OutOfSync");
                    }
                }

#if BENCHMARK
                Debug.TimeStamps[TimeStamp.VehicleTotal] = PerfCounter.ElapsedTicks;
#endif
            }

            Networking.Peer.FlushSendQueue();
        }

        private static void UpdateTargets()
        {
            Networking.Targets = new List<NetConnection>(PlayerList.Players.Count) { Networking.ServerConnection };
            foreach (var p in PlayerList.Players.Values.ToArray())
                if (p.HasDirectConnection && p.Position.DistanceTo(Main.PlayerPosition) < 500)
                    Networking.Targets.Add(p.Connection);
        }

        public static void RemoveAllFromPlayer(int playerPedId)
        {
            foreach (var p in PedsByID.Values.ToArray())
                if (p.OwnerID == playerPedId)
                    RemovePed(p.ID);

            foreach (var v in VehiclesByID.Values.ToArray())
                if (v.OwnerID == playerPedId)
                    RemoveVehicle(v.ID);
        }

        public static int RequestNewID()
        {
            var ID = 0;
            while (ID == 0 || PedsByID.ContainsKey(ID) || VehiclesByID.ContainsKey(ID) ||
                   ProjectilesByID.ContainsKey(ID))
            {
                var rngBytes = new byte[4];

                RandomNumberGenerator.Create().GetBytes(rngBytes);

                // Convert the bytes into an integer
                ID = BitConverter.ToInt32(rngBytes, 0);
            }

            return ID;
        }

        private static void SetBudget(int b)
        {
            Call(SET_PED_POPULATION_BUDGET, b); // 0 - 3
            Call(SET_VEHICLE_POPULATION_BUDGET, b); // 0 - 3
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