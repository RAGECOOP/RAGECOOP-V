using GTA;
using GTA.Math;
using RageCoop.Core;
using RageCoop.Core.Scripting;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Security.Cryptography;

namespace RageCoop.Server.Scripting
{

    /// <summary>
    /// Manipulate entities from the server
    /// </summary>
    public class ServerEntities
    {
        private readonly Server Server;
        internal ServerEntities(Server server)
        {
            Server = server;
        }
        internal ConcurrentDictionary<int, ServerPed> Peds { get; set; } = new();
        internal ConcurrentDictionary<int, ServerVehicle> Vehicles { get; set; } = new();
        internal ConcurrentDictionary<int, ServerProp> ServerProps { get; set; } = new();
        internal ConcurrentDictionary<int, ServerBlip> Blips { get; set; } = new();

        /// <summary>
        /// Get a <see cref="ServerPed"/> by it's id
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public ServerPed GetPedByID(int id) => Peds.TryGetValue(id, out var ped) ? ped : null;

        /// <summary>
        /// Get a <see cref="ServerVehicle"/> by it's id
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public ServerVehicle GetVehicleByID(int id) => Vehicles.TryGetValue(id, out var veh) ? veh : null;

        /// <summary>
        /// Get a <see cref="ServerProp"/> owned by server from it's ID.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public ServerProp GetPropByID(int id) => ServerProps.TryGetValue(id, out var obj) ? obj : null;

        /// <summary>
        /// Get a <see cref="ServerBlip"/> by it's id.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public ServerBlip GetBlipByID(int id) => Blips.TryGetValue(id, out var obj) ? obj : null;

        /// <summary>
        /// Create a static prop owned by server.
        /// </summary>
        /// <param name="model"></param>
        /// <param name="pos"></param>
        /// <param name="rot"></param>
        /// <returns></returns>
        public ServerProp CreateProp(Model model, Vector3 pos, Vector3 rot)
        {
            int id = RequestNetworkID();
            ServerProp prop;
            ServerProps.TryAdd(id, prop = new ServerProp(Server)
            {
                ID = id,
                Model = model,
                _pos = pos,
                _rot = rot
            });
            prop.Update();
            return prop;
        }

        /// <summary>
        /// Create a vehicle
        /// </summary>
        /// <param name="owner">Owner of this vehicle</param>
        /// <param name="model">model</param>
        /// <param name="pos">position</param>
        /// <param name="heading">heading of this vehicle</param>
        /// <returns></returns>
        public ServerVehicle CreateVehicle(Client owner, Model model, Vector3 pos, float heading)
        {
            if (owner == null) { throw new ArgumentNullException("Owner cannot be null"); }
            ServerVehicle veh = new(Server)
            {
                Owner = owner,
                ID = RequestNetworkID(),
                Model = model,
                _pos = pos,
            };
            owner.SendCustomEventQueued(CustomEvents.CreateVehicle, veh.ID, model, pos, heading);
            Vehicles.TryAdd(veh.ID, veh);
            return veh;
        }

        /// <summary>
        /// Create a static <see cref="ServerBlip"/> owned by server.
        /// </summary>
        /// <param name="pos"></param>
        /// <param name="rotation"></param>
        /// <returns></returns>
        public ServerBlip CreateBlip(Vector3 pos, int rotation)
        {
            var b = new ServerBlip(Server)
            {
                ID = RequestNetworkID(),
                Position = pos,
                Rotation = rotation
            };
            Blips.TryAdd(b.ID, b);
            b.Update();
            return b;
        }

        /// <summary>
        /// Get all peds on this server
        /// </summary>
        /// <returns></returns>
        public ServerPed[] GetAllPeds() => Peds.Values.ToArray();

        /// <summary>
        /// Get all vehicles on this server
        /// </summary>
        /// <returns></returns>
        public ServerVehicle[] GetAllVehicles() => Vehicles.Values.ToArray();

        /// <summary>
        /// Get all static prop objects owned by server
        /// </summary>
        /// <returns></returns>
        public ServerProp[] GetAllProps() => ServerProps.Values.ToArray();

        /// <summary>
        /// Get all blips owned by server
        /// </summary>
        /// <returns></returns>
        public ServerBlip[] GetAllBlips() => Blips.Values.ToArray();

        /// <summary>
        /// Not thread safe
        /// </summary>
        internal void Update(Packets.PedSync p, Client sender)
        {
            if (!Peds.TryGetValue(p.ID, out ServerPed ped))
            {
                Peds.TryAdd(p.ID, ped = new ServerPed(Server));
                ped.ID = p.ID;
            }
            ped._pos = p.Position;
            ped.Owner = sender;
            ped.Health = p.Health;
            ped._rot = p.Rotation;
            ped._isInvincible = p.Flags.HasPedFlag(PedDataFlags.IsInvincible);
            if (p.Speed >= 4 && Vehicles.TryGetValue(p.VehicleID, out var v))
            {
                ped.LastVehicle = v;
            }

            if (ped.Owner != sender)
            {
                if (ped.Owner != null)
                {
                    ped.Owner.EntitiesCount--;
                }
                ped.Owner = sender;
                sender.EntitiesCount++;
            }
        }
        internal void Update(Packets.VehicleSync p, Client sender)
        {
            if (!Vehicles.TryGetValue(p.ID, out ServerVehicle veh))
            {
                Vehicles.TryAdd(p.ID, veh = new ServerVehicle(Server));
                veh.ID = p.ID;
            }
            veh._pos = p.Position + p.Velocity * sender.Latency;
            veh._quat = p.Quaternion;
            if (veh.Owner != sender)
            {
                if (veh.Owner != null)
                {
                    veh.Owner.EntitiesCount--;
                }
                veh.Owner = sender;
                sender.EntitiesCount++;
            }
        }
        internal void CleanUp(Client left)
        {
            // Server.Logger?.Trace("Removing all entities from: "+left.Username);

            foreach (var pair in Peds)
            {
                if (pair.Value.Owner == left)
                {
                    Server.QueueJob(() => Peds.TryRemove(pair.Key, out _));
                }
            }
            foreach (var pair in Vehicles)
            {
                if (pair.Value.Owner == left)
                {
                    Server.QueueJob(() => Vehicles.TryRemove(pair.Key, out _));
                }
            }
            // Server.QueueJob(() =>
            //  Server.Logger?.Trace("Remaining entities: "+(Peds.Count+Vehicles.Count+ServerProps.Count)));
        }
        internal void RemoveVehicle(int id)
        {
            Vehicles.TryRemove(id, out var veh);
            if (veh?.Owner != null)
            {
                veh.Owner.EntitiesCount--;
            }
        }

        internal void RemoveProp(int id) => ServerProps.TryRemove(id, out _);
        internal void RemoveServerBlip(int id) => Blips.TryRemove(id, out _);
        internal void RemovePed(int id)
        {
            Peds.TryRemove(id, out var ped);
            if (ped?.Owner != null)
            {
                ped.Owner.EntitiesCount--;
            }
        }

        internal void Add(ServerPed ped)
        {
            if (Peds.ContainsKey(ped.ID))
            {
                Peds[ped.ID] = ped;
                return;
            }

            Peds.TryAdd(ped.ID, ped);
        }
        internal int RequestNetworkID()
        {
            int ID = 0;
            while ((ID == 0)
                || ServerProps.ContainsKey(ID)
                || Peds.ContainsKey(ID)
                || Vehicles.ContainsKey(ID)
                || Blips.ContainsKey(ID))
            {
                byte[] rngBytes = new byte[4];

                RandomNumberGenerator.Create().GetBytes(rngBytes);

                // Convert the bytes into an integer
                ID = BitConverter.ToInt32(rngBytes, 0);
            }
            return ID;
        }
    }
}
