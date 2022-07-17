using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RageCoop.Core;
using RageCoop.Core.Scripting;
using System.Security.Cryptography;
using GTA.Math;
using GTA;

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
        internal Dictionary<int, ServerPed> Peds { get; set; } = new();
        internal Dictionary<int, ServerVehicle> Vehicles { get; set; } = new();
        internal Dictionary<int,ServerProp> ServerProps { get; set; }=new();
        internal Dictionary<int,ServerBlip> Blips { get; set; } = new();
        
        /// <summary>
        /// Get a <see cref="ServerPed"/> by it's id
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public ServerPed GetPedByID(int id)
        {
            if(Peds.TryGetValue(id,out var ped))
            {
                return ped;
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Get a <see cref="ServerVehicle"/> by it's id
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public ServerVehicle GetVehicleByID(int id)
        {
            if (Vehicles.TryGetValue(id, out var veh))
            {
                return veh;
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Get a <see cref="ServerProp"/> owned by server from it's ID.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public ServerProp GetPropByID(int id)
        {
            if (ServerProps.TryGetValue(id, out var obj))
            {
                return obj;
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Get a <see cref="ServerBlip"/> by it's id.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public ServerBlip GetBlipByID(int id)
        {
            if (Blips.TryGetValue(id, out var obj))
            {
                return obj;
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Create a static prop owned by server.
        /// </summary>
        /// <param name="model"></param>
        /// <param name="pos"></param>
        /// <param name="rot"></param>
        /// <returns></returns>
        public ServerProp CreateProp(Model model,Vector3 pos,Vector3 rot)
        {
            int id = RequestNetworkID();
            ServerProp prop;
            ServerProps.Add(id,prop=new ServerProp(Server)
            {
                ID=id,
                Model=model,
                _pos=pos,
                _rot=rot
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
        public ServerVehicle CreateVehicle(Client owner,Model model,Vector3 pos,float heading)
        {
            if(owner == null) { throw new ArgumentNullException("Owner cannot be null"); }
            ServerVehicle veh = new(Server)
            {
                Owner=owner,
                ID=RequestNetworkID(),
                Model=model,
                _pos= pos,
            };
            owner.SendCustomEventQueued(CustomEvents.CreateVehicle,veh.ID, model, pos, heading);
            Vehicles.Add(veh.ID, veh);
            return veh;
        }

        /// <summary>
        /// Create a static <see cref="ServerBlip"/> owned by server.
        /// </summary>
        /// <param name="pos"></param>
        /// <param name="rotation"></param>
        /// <returns></returns>
        public ServerBlip CreateBlip(Vector3 pos,int rotation)
        {
            var b = new ServerBlip(Server)
            {
                ID=RequestNetworkID(),
                Position=pos,
                Rotation=rotation
            };
            Blips.Add(b.ID,b);
            b.Update();
            return b;
        }

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
        public ServerVehicle[] GetAllVehicles()
        {
            return Vehicles.Values.ToArray();
        }

        /// <summary>
        /// Get all static prop objects owned by server
        /// </summary>
        /// <returns></returns>
        public ServerProp[] GetAllProps()
        {
            return ServerProps.Values.ToArray();
        }

        /// <summary>
        /// Get all blips owned by server
        /// </summary>
        /// <returns></returns>
        public ServerBlip[] GetAllBlips()
        {
            return Blips.Values.ToArray();
        }

        /// <summary>
        /// Not thread safe
        /// </summary>
        internal void Update(Packets.PedSync p,Client sender)
        {
            ServerPed ped;
            if(!Peds.TryGetValue(p.ID,out ped))
            {
                Peds.Add(p.ID,ped=new ServerPed(Server));
                ped.ID=p.ID;
            }
            ped._pos = p.Position;
            ped.Owner=sender;
            ped.Health=p.Health;
            ped._rot=p.Rotation;
            ped.Owner=sender;
        }
        internal void Update(Packets.VehicleSync p, Client sender)
        {
            ServerVehicle veh;
            if (!Vehicles.TryGetValue(p.ID, out veh))
            {
                Vehicles.Add(p.ID, veh=new ServerVehicle(Server));
                veh.ID=p.ID;
            }
            veh._pos = p.Position;
            veh.Owner=sender;
            veh._quat=p.Quaternion;
            if (p.Flag.HasVehFlag(VehicleDataFlags.IsFullSync))
            {
                foreach (var pair in p.Passengers)
                {
                    if (Peds.TryGetValue(pair.Value, out var ped))
                    {
                        ped.LastVehicle=veh;
                    }
                }
            }
        }
        internal void CleanUp(Client left)
        {
            // Server.Logger?.Trace("Removing all entities from: "+left.Username);

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
            // Server.QueueJob(() =>
           //  Server.Logger?.Trace("Remaining entities: "+(Peds.Count+Vehicles.Count+ServerProps.Count)));
        }
        internal void RemoveVehicle(int id)
        {
            // Server.Logger?.Trace($"Removing vehicle:{id}");
            if (Vehicles.ContainsKey(id)) { Vehicles.Remove(id); }
        }

        internal void RemoveProp(int id)
        {
            // Server.Logger?.Trace($"Removing vehicle:{id}");
            if (ServerProps.ContainsKey(id)) { ServerProps.Remove(id); }
        }
        internal void RemoveServerBlip(int id)
        {
            // Server.Logger?.Trace($"Removing vehicle:{id}");
            if (Blips.ContainsKey(id)) { Blips.Remove(id); }
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
        internal int RequestNetworkID()
        {
            int ID = 0;
            while ((ID==0)
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
