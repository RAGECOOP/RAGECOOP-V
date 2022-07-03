using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using GTA;
using GTA.Native;
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
        internal ServerObject() { }
        
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
        /// Send updated information to clients, would be called automatically.
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

        /// <summary>
        /// Freeze this object, will throw an exception if it's a ServerProp.
        /// </summary>
        /// <param name="toggle"></param>
        /// <exception cref="InvalidOperationException"></exception>
        public virtual void Freeze(bool toggle)
        {
            if (GetTypeByte()==50)
            {
                throw new InvalidOperationException("Can't freeze or unfreeze static server object");
            }
            else
            {
                Owner.SendNativeCall(Hash.FREEZE_ENTITY_POSITION, Handle, toggle);
            }
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
            Server.Entities.RemoveProp(ID);
        }

        


        /// <summary>
        /// Send updated information to clients, would be called automatically.
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

    /// <summary>
    /// A static blip owned by server.
    /// </summary>
    public class ServerBlip
    {
        private readonly Server Server;
        internal ServerBlip(Server server)
        {
            Server = server;
        }

        /// <summary>
        /// Network ID (not handle!)
        /// </summary>
        public int ID { get; internal set; }


        internal BlipColor _color;
        /// <summary>
        /// Color of this blip
        /// </summary>
        public BlipColor Color { 
            get { return _color; } 
            set { _color=value; Update(); } 
        }

        internal BlipSprite _sprite;
        /// <summary>
        /// Sprite of this blip
        /// </summary>
        public BlipSprite Sprite {
            get { return _sprite; }
            set { _sprite=value; Update();}
        }

        internal Vector2 _scale=new(1f,1f);
        /// <summary>
        /// Scale of this blip
        /// </summary>
        public Vector2 Scale
        {
            get { return _scale; }
            set { _scale=value;Update(); }
        }

        internal Vector3 _pos = new();
        /// <summary>
        /// Position of this blip
        /// </summary>
        public Vector3 Position
        {
            get { return _pos; }
            set { _pos=value; Update(); }
        }

        internal int _rot;
        /// <summary>
        /// Scale of this blip
        /// </summary>
        public int Rotation
        {
            get { return _rot; }
            set { _rot=value; Update(); }
        }

        /// <summary>
        /// Delete this blip
        /// </summary>
        public void Delete()
        {
            Server.API.SendCustomEvent(CustomEvents.DeleteServerBlip, new() { ID });
            Server.Entities.RemoveServerBlip(ID);
        }

        private bool _bouncing=false;
        internal void Update()
        {
            // 5ms debounce
            if (!_bouncing)
            {
                _bouncing=true;
                Task.Run(() =>
                {
                    Thread.Sleep(5);
                    DoUpdate();
                    _bouncing=false;
                });
            }
        }
        private void DoUpdate()
        {
            Server.Logger?.Debug("bee");
            // Serve-side blip
            Server.BaseScript.SendServerBlipsTo(new() { this });

        }
    }
}
