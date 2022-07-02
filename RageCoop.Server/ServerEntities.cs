using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RageCoop.Core;
using GTA.Math;
using GTA;

namespace RageCoop.Server
{

    /// <summary>
    /// Represents a ped from a client
    /// </summary>
    public class ServerPed
    {
        /// <summary>
        /// The <see cref="Client"/> that is responsible synchronizing for this ped.
        /// </summary>
        public Client Owner { get; internal set; }

        /// <summary>
        /// The ped's network ID (not handle!).
        /// </summary>
        public int ID { get; internal set; }

        /// <summary>
        /// Whether this ped is a player.
        /// </summary>
        public bool IsPlayer { get { return Owner?.Player==this; } }

        /// <summary>
        /// The ped's last vehicle.
        /// </summary>
        public ServerVehicle LastVehicle { get; internal set; }

        /// <summary>
        /// Position of this ped
        /// </summary>
        public Vector3 Position { get; internal set; }


        /// <summary>
        /// Gets or sets this ped's rotation
        /// </summary>
        public Vector3 Rotation { get; internal set; }

        /// <summary>
        /// Health
        /// </summary>
        public int Health { get; internal set; }
    }
    /// <summary>
    /// Represents a vehicle from a client
    /// </summary>
    public class ServerVehicle
    {
        /// <summary>
        /// The <see cref="Client"/> that is responsible synchronizing for this vehicle.
        /// </summary>
        public Client Owner { get; internal set; }

        /// <summary>
        /// The vehicle's network ID (not handle!).
        /// </summary>
        public int ID { get; internal set; }

        /// <summary>
        /// Position of this vehicle
        /// </summary>
        public Vector3 Position { get; internal set; }

        /// <summary>
        /// Gets or sets this vehicle's quaternion
        /// </summary>
        public Quaternion Quaternion { get; internal set; }
    }
    
    /// <summary>
    /// Represents an object owned by server.
    /// </summary>
    public class ServerObject
    {
        internal ServerObject()
        {

        }
        /// <summary>
        /// The object's model
        /// </summary>
        public Model Model { get; internal set; }

        /// <summary>
        /// Gets or sets this object's position
        /// </summary>
        public Vector3 Position { get;set; }
        
        /// <summary>
        /// Gets or sets this object's quaternion
        /// </summary>
        public Quaternion Quaternion { get; set; }

        /// <summary>
        /// Whether this object is invincible
        /// </summary>
        public bool IsInvincible { get; set; }
    }

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
        internal Dictionary<int, ServerPed> Peds { get; set; } = new();
        internal Dictionary<int, ServerVehicle> Vehicles { get; set; } = new();
        internal Dictionary<int,ServerObject> ServerObjects { get; set; }=new();
        
        /// <summary>
        /// Get all peds on this server
        /// </summary>
        /// <returns></returns>
        public ServerPed[] GetAllPeds()
        {
            return Peds.Values.ToArray();
        }

        /// <summary>
        /// Get all vehicles on this server
        /// </summary>
        /// <returns></returns>
        public ServerVehicle[] GetAllVehicle()
        {
            return Vehicles.Values.ToArray();
        }

        /// <summary>
        /// Get all static objects owned by server
        /// </summary>
        /// <returns></returns>
        public ServerObject[] GetAllObjects()
        {
            return ServerObjects.Values.ToArray();
        } 

        /// <summary>
        /// Not thread safe
        /// </summary>
        internal void Update(Packets.PedSync p,Client sender)
        {
            ServerPed ped;
            if(!Peds.TryGetValue(p.ID,out ped))
            {
                Peds.Add(p.ID,ped=new ServerPed());
                ped.ID=p.ID;
            }
            ped.Position = p.Position;
            ped.Owner=sender;
            ped.Health=p.Health;
            ped.Rotation=p.Rotation;
            ped.Owner=sender;
        }
        internal void Update(Packets.VehicleSync p, Client sender)
        {
            ServerVehicle veh;
            if (!Vehicles.TryGetValue(p.ID, out veh))
            {
                Vehicles.Add(p.ID, veh=new ServerVehicle());
                veh.ID=p.ID;
            }
            veh.Position = p.Position;
            veh.Owner=sender;
            veh.Quaternion=p.Quaternion;
        }
        internal void Update(Packets.VehicleStateSync p, Client sender)
        {
            ServerVehicle veh;
            if (!Vehicles.TryGetValue(p.ID, out veh))
            {
                Vehicles.Add(p.ID, veh=new ServerVehicle());
                veh.ID=p.ID;
            }
            foreach(var pair in p.Passengers)
            {
                if(Peds.TryGetValue(pair.Value,out var ped))
                {
                    ped.LastVehicle=veh;
                }
            }
        }
        internal void CleanUp(Client left)
        {
            Server.Logger?.Trace("Removing all entities from: "+left.Username);

            foreach (var pair in Peds)
            {
                if (pair.Value.Owner==left)
                {
                    Server.QueueJob(()=>Peds.Remove(pair.Key));
                }
            }
            foreach (var pair in Vehicles)
            {
                if (pair.Value.Owner==left)
                {
                    Server.QueueJob(() => Vehicles.Remove(pair.Key));
                }
            }
            Server.QueueJob(() =>
            Server.Logger?.Trace("Remaining entities: "+(Peds.Count+Vehicles.Count)));
        }
        internal void RemoveVehicle(int id)
        {
            // Server.Logger?.Trace($"Removing vehicle:{id}");
            if (Vehicles.ContainsKey(id)) { Vehicles.Remove(id); }
        }
        internal void RemovePed(int id)
        {
            // Server.Logger?.Trace($"Removing ped:{id}");
            if (Peds.ContainsKey(id)) { Peds.Remove(id); }
        }

        internal void Add(ServerPed ped)
        {
            if (Peds.ContainsKey(ped.ID))
            {
                Peds[ped.ID]=ped;
            }
            else
            {
                Peds.Add(ped.ID, ped);
            }
        }
    }
}
