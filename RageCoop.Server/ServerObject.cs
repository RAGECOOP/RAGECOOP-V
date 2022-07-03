using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GTA;
using GTA.Math;
using RageCoop.Core;
using RageCoop.Core.Scripting;

namespace RageCoop.Server
{

    /// <summary>
    /// Server-side object controller
    /// </summary>
    public abstract class ServerObject
    {
        /// <summary>
        /// Pass this as an argument in CustomEvent or NativeCall to convert this object to handle at client side.
        /// </summary>
        public Tuple<byte, byte[]> Handle
        {
            get
            {
                return new(GetTypeByte(), BitConverter.GetBytes(ID));
            }
        }

        private byte GetTypeByte()
        {
            switch (this)
            {
                case ServerProp _:
                    return 50;
                case ServerPed _:
                    return 51;
                case ServerVehicle _:
                    return 52;
                default:
                    throw new NotImplementedException();
            }
        }

        /// <summary>
        /// The client that owns this object, null if it's owned by server.
        /// </summary>
        public Client Owner { get; internal set; }

        /// <summary>
        /// Network ID of this object.
        /// </summary>
        public int ID { get; internal set; }

        /// <summary>
        /// The object's model
        /// </summary>
        public Model Model { get; internal set; }

        /// <summary>
        /// Gets or sets this object's position
        /// </summary>
        public virtual Vector3 Position
        {
            get { return _pos; }
            set { _pos=value; Update(); }
        }
        internal Vector3 _pos;

        /// <summary>
        /// Gets or sets this object's rotation
        /// </summary>
        public virtual Vector3 Rotation
        {
            get { return _rot; }
            set { _rot=value; Update(); }
        }
        internal Vector3 _rot;

        /// <summary>
        /// Send updated information to clients
        /// </summary>
        public virtual void Update() {
            Owner.SendCustomEvent(CustomEvents.SetEntity, Handle, Position, Rotation);
        }

        /// <summary>
        /// Delete this object
        /// </summary>
        public virtual void Delete()
        {
            Owner?.SendCustomEvent(CustomEvents.DeleteEntity, Handle);
        }
    }
    /// <summary>
    /// Represents an prop owned by server.
    /// </summary>
    public class ServerProp : ServerObject
    {
        private Server Server;
        internal ServerProp(Server server)
        {
            Server= server;
        }


        

        /// <summary>
        /// Delete this prop
        /// </summary>
        public override void Delete()
        {
            Server.API.SendCustomEvent(CustomEvents.DeleteServerProp, new() { ID });
        }

        


        /// <summary>
        /// Send updated information to clients
        /// </summary>
        public override void Update()
        {
             Server.BaseScript.SendServerPropsTo(new() { this });
        }
    }
    /// <summary>
    /// Represents a ped from a client
    /// </summary>
    public class ServerPed : ServerObject
    {
        internal ServerPed()
        {

        }

        /// <summary>
        /// Get the ped's last vehicle
        /// </summary>
        public ServerVehicle LastVehicle { get; internal set; }

        /// <summary>
        /// Health
        /// </summary>
        public int Health { get; internal set; }
    }
    /// <summary>
    /// Represents a vehicle from a client
    /// </summary>
    public class ServerVehicle : ServerObject
    {
        internal ServerVehicle()
        {

        }

        /// <summary>
        /// Gets or sets vehicle rotation
        /// </summary>
        public override Vector3 Rotation
        {
            get { return Quaternion.ToEulerAngles().ToDegree(); }
            set { Quaternion=value.ToQuaternion(); Update(); }
        }

        /// <summary>
        /// Get this vehicle's quaternion
        /// </summary>
        public Quaternion Quaternion { get; internal set; }
    }
    internal class ServerBlip : ServerObject
    {
        internal ServerBlip()
        {

        }

        /// <summary>
        /// Invalid
        /// </summary>
        private new Vector3 Rotation { get; set; }

        BlipColor _color;
        public BlipColor Color {
            get {
                return _color; 
            } 
            set { 
                if(Owner != null)
                {

                }
                else
                {
                    throw new NotImplementedException();
                }
                _color = value;
            } 
        }
        private new Vector2 Position
        {
            get; set;
        }
    }
}
