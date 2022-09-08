using GTA;
using GTA.Math;
using GTA.Native;
using RageCoop.Core;
using RageCoop.Core.Scripting;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace RageCoop.Server.Scripting
{

    /// <summary>
    /// Server-side object controller
    /// </summary>
    public abstract class ServerObject
    {
        /// <summary>
        /// Server that this object belongs to
        /// </summary>
        internal readonly Server Server;
        internal ServerObject(Server server) { Server = server; }

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
            get => _pos;
            set { _pos = value; Owner.SendNativeCall(Hash.SET_ENTITY_COORDS_NO_OFFSET, Handle, value.X, value.Y, value.Z, 1, 1, 1); }
        }
        internal Vector3 _pos;

        /// <summary>
        /// Gets or sets this object's rotation
        /// </summary>
        public virtual Vector3 Rotation
        {
            get => _rot;
            set { _rot = value; Owner.SendNativeCall(Hash.SET_ENTITY_ROTATION, Handle, value.X, value.Y, value.Z, 2, 1); }
        }
        internal Vector3 _rot;

        /// <summary>
        /// Send updated information to clients, would be called automatically.
        /// </summary>


        /// <summary>
        /// Delete this object
        /// </summary>
        public virtual void Delete()
        {
            Owner?.SendCustomEventQueued(CustomEvents.DeleteEntity, Handle);
        }

        /// <summary>
        /// Freeze this object, will throw an exception if it's a ServerProp.
        /// </summary>
        /// <param name="toggle"></param>
        /// <exception cref="InvalidOperationException"></exception>
        public virtual void Freeze(bool toggle)
        {
            if (GetTypeByte() == 50)
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

        internal ServerProp(Server server) : base(server) { }

        /// <summary>
        /// Delete this prop
        /// </summary>
        public override void Delete()
        {
            Server.API.SendCustomEventQueued(null, CustomEvents.DeleteServerProp, ID);
            Server.API.Entities.RemoveProp(ID);
        }

        /// <summary>
        /// Gets or sets this object's position
        /// </summary>
        public override Vector3 Position
        {
            get => _pos;
            set { _pos = value; Server.API.SendNativeCall(null, Hash.SET_ENTITY_COORDS_NO_OFFSET, Handle, value.X, value.Y, value.Z, 1, 1, 1); }
        }

        /// <summary>
        /// Gets or sets this object's rotation
        /// </summary>
        public override Vector3 Rotation
        {
            get => _rot;
            set { _rot = value; Server.API.SendNativeCall(null, Hash.SET_ENTITY_ROTATION, Handle, value.X, value.Y, value.Z, 2, 1); }
        }


        /// <summary>
        /// Send updated information to clients, would be called automatically.
        /// </summary>
        internal void Update()
        {
            Server.API.Server.BaseScript.SendServerPropsTo(new() { this });
        }

    }
    /// <summary>
    /// Represents a ped from a client
    /// </summary>
    public class ServerPed : ServerObject
    {
        internal ServerPed(Server server) : base(server) { }

        /// <summary>
        /// Get the ped's last vehicle
        /// </summary>
        public ServerVehicle LastVehicle { get; internal set; }

        /// <summary>
        /// Get the <see cref="PedBlip"/> attached to this ped.
        /// </summary>
        public PedBlip AttachedBlip { get; internal set; }

        /// <summary>
        /// Attach a blip to this ped.
        /// </summary>
        /// <returns></returns>
        public PedBlip AddBlip()
        {
            AttachedBlip = new PedBlip(this);
            AttachedBlip.Update();
            return AttachedBlip;
        }

        /// <summary>
        /// Health
        /// </summary>
        public int Health { get; internal set; }


        internal bool _isInvincible;
        /// <summary>
        /// Get or set whether this ped is invincible
        /// </summary>
        public bool IsInvincible
        {
            get => _isInvincible;
            set => Owner.SendNativeCall(Hash.SET_ENTITY_INVINCIBLE, Handle, value);
        }
    }
    /// <summary>
    /// Represents a vehicle from a client
    /// </summary>
    public class ServerVehicle : ServerObject
    {
        internal ServerVehicle(Server server) : base(server) { }

        /// <summary>
        /// Gets or sets vehicle rotation
        /// </summary>
        public override Vector3 Rotation
        {
            get => _quat.ToEulerAngles().ToDegree();
            set { Owner.SendNativeCall(Hash.SET_ENTITY_ROTATION, Handle, value.X, value.Y, value.Z); }
        }

        internal Quaternion _quat;
        /// <summary>
        /// Get this vehicle's quaternion
        /// </summary>
        public Quaternion Quaternion
        {
            get => _quat;
            set { _quat = value; Owner.SendNativeCall(Hash.SET_ENTITY_QUATERNION, Handle, value.X, value.Y, value.Z, value.W); }
        }
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
        /// Pass this as an argument in CustomEvent or NativeCall to convert this object to handle at client side.
        /// </summary>
        public Tuple<byte, byte[]> Handle
        {
            get
            {
                return new(60, BitConverter.GetBytes(ID));
            }
        }

        /// <summary>
        /// Network ID (not handle!)
        /// </summary>
        public int ID { get; internal set; }


        internal BlipColor _color;
        /// <summary>
        /// Color of this blip
        /// </summary>
        public BlipColor Color
        {
            get => _color;
            set { _color = value; Update(); }
        }

        internal BlipSprite _sprite = BlipSprite.Standard;
        /// <summary>
        /// Sprite of this blip
        /// </summary>
        public BlipSprite Sprite
        {
            get => _sprite;
            set { _sprite = value; Update(); }
        }

        internal float _scale = 1;
        /// <summary>
        /// Scale of this blip
        /// </summary>
        public float Scale
        {
            get => _scale;
            set { _scale = value; Update(); }
        }

        internal Vector3 _pos = new();
        /// <summary>
        /// Position of this blip
        /// </summary>
        public Vector3 Position
        {
            get => _pos;
            set { _pos = value; Update(); }
        }

        internal int _rot;
        /// <summary>
        /// Rotation of this blip
        /// </summary>
        public int Rotation
        {
            get => _rot;
            set { _rot = value; Update(); }
        }

        internal string _name = "Beeeeeee";
        /// <summary>
        /// Name of this blip
        /// </summary>
        public string Name
        {
            get => _name;
            set { _name = value; Update(); }
        }

        /// <summary>
        /// Delete this blip
        /// </summary>
        public void Delete()
        {
            Server.API.SendCustomEventQueued(null, CustomEvents.DeleteServerBlip, ID);
            Server.Entities.RemoveServerBlip(ID);
        }


        private bool _bouncing = false;
        internal void Update()
        {
            // 5ms debounce
            if (!_bouncing)
            {
                _bouncing = true;
                Task.Run(() =>
                {
                    Thread.Sleep(5);
                    DoUpdate();
                    _bouncing = false;
                });
            }
        }
        private void DoUpdate()
        {
            // Server.Logger?.Debug("bee");
            // Serve-side blip
            Server.BaseScript.SendServerBlipsTo(new() { this });

        }
    }

    /// <summary>
    /// Represent a blip attached to ped.
    /// </summary>
    public class PedBlip
    {
        /// <summary>
        /// Get the <see cref="ServerPed"/> that this blip attached to.
        /// </summary>
        public ServerPed Ped { get; internal set; }
        internal PedBlip(ServerPed ped)
        {
            Ped = ped;
        }


        internal BlipColor _color;
        /// <summary>
        /// Color of this blip
        /// </summary>
        public BlipColor Color
        {
            get => _color;
            set { _color = value; Update(); }
        }

        internal BlipSprite _sprite = BlipSprite.Standard;
        /// <summary>
        /// Sprite of this blip
        /// </summary>
        public BlipSprite Sprite
        {
            get => _sprite;
            set { _sprite = value; Update(); }
        }

        internal float _scale = 1;
        /// <summary>
        /// Scale of this blip
        /// </summary>
        public float Scale
        {
            get => _scale;
            set { _scale = value; Update(); }
        }

        private bool _bouncing = false;
        internal void Update()
        {
            // 5ms debounce
            if (!_bouncing)
            {
                _bouncing = true;
                Task.Run(() =>
                {
                    Thread.Sleep(5);
                    DoUpdate();
                    _bouncing = false;
                });
            }
        }
        private void DoUpdate()
        {
            Ped.Owner.SendCustomEventQueued(CustomEvents.UpdatePedBlip, Ped.Handle, (byte)Color, (ushort)Sprite, Scale);

        }
    }
}
