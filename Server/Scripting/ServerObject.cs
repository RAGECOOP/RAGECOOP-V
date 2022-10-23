using System;
using System.Threading;
using System.Threading.Tasks;
using GTA;
using GTA.Math;
using GTA.Native;
using RageCoop.Core;
using RageCoop.Core.Scripting;

namespace RageCoop.Server.Scripting;

/// <summary>
///     Server-side object controller
/// </summary>
public abstract class ServerObject
{
    /// <summary>
    ///     Server that this object belongs to
    /// </summary>
    internal readonly Server Server;

    internal Vector3 _pos;
    internal Vector3 _rot;

    internal ServerObject(Server server)
    {
        Server = server;
    }

    /// <summary>
    ///     Pass this as an argument in CustomEvent or NativeCall to convert this object to handle at client side.
    /// </summary>
    public Tuple<byte, byte[]> Handle => new Tuple<byte, byte[]>(GetTypeByte(), BitConverter.GetBytes(ID));

    /// <summary>
    ///     The client that owns this object, null if it's owned by server.
    /// </summary>
    public Client Owner { get; internal set; }

    /// <summary>
    ///     Network ID of this object.
    /// </summary>
    public int ID { get; internal set; }

    /// <summary>
    ///     The object's model
    /// </summary>
    public Model Model { get; internal set; }

    /// <summary>
    ///     Gets or sets this object's position
    /// </summary>
    public virtual Vector3 Position
    {
        get => _pos;
        set
        {
            _pos = value;
            Owner.SendNativeCall(Hash.SET_ENTITY_COORDS_NO_OFFSET, Handle, value.X, value.Y, value.Z, 1, 1, 1);
        }
    }

    /// <summary>
    ///     Gets or sets this object's rotation
    /// </summary>
    public virtual Vector3 Rotation
    {
        get => _rot;
        set
        {
            _rot = value;
            Owner.SendNativeCall(Hash.SET_ENTITY_ROTATION, Handle, value.X, value.Y, value.Z, 2, 1);
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
    ///     Send updated information to clients, would be called automatically.
    /// </summary>
    /// <summary>
    ///     Delete this object
    /// </summary>
    public virtual void Delete()
    {
        Owner?.SendCustomEvent(CustomEventFlags.Queued, CustomEvents.DeleteEntity, Handle);
    }

    /// <summary>
    ///     Freeze this object, will throw an exception if it's a ServerProp.
    /// </summary>
    /// <param name="toggle"></param>
    /// <exception cref="InvalidOperationException"></exception>
    public virtual void Freeze(bool toggle)
    {
        if (GetTypeByte() == 50)
            throw new InvalidOperationException("Can't freeze or unfreeze static server object");
        Owner.SendNativeCall(Hash.FREEZE_ENTITY_POSITION, Handle, toggle);
    }
}

/// <summary>
///     Represents an prop owned by server.
/// </summary>
public class ServerProp : ServerObject
{
    internal ServerProp(Server server) : base(server)
    {
    }

    /// <summary>
    ///     Gets or sets this object's position
    /// </summary>
    public override Vector3 Position
    {
        get => _pos;
        set
        {
            _pos = value;
            Server.API.SendNativeCall(null, Hash.SET_ENTITY_COORDS_NO_OFFSET, Handle, value.X, value.Y, value.Z, 1, 1,
                1);
        }
    }

    /// <summary>
    ///     Gets or sets this object's rotation
    /// </summary>
    public override Vector3 Rotation
    {
        get => _rot;
        set
        {
            _rot = value;
            Server.API.SendNativeCall(null, Hash.SET_ENTITY_ROTATION, Handle, value.X, value.Y, value.Z, 2, 1);
        }
    }

    /// <summary>
    ///     Delete this prop
    /// </summary>
    public override void Delete()
    {
        Server.API.SendCustomEvent(CustomEventFlags.Queued, null, CustomEvents.DeleteServerProp, ID);
        Server.API.Entities.RemoveProp(ID);
    }


    /// <summary>
    ///     Send updated information to clients, would be called automatically.
    /// </summary>
    internal void Update()
    {
        Server.API.Server.BaseScript.SendServerPropsTo(new List<ServerProp> { this });
    }
}

/// <summary>
///     Represents a ped from a client
/// </summary>
public class ServerPed : ServerObject
{
    internal bool _isInvincible;

    internal ServerPed(Server server) : base(server)
    {
    }

    /// <summary>
    ///     Get the ped's last vehicle
    /// </summary>
    public ServerVehicle LastVehicle { get; internal set; }

    /// <summary>
    ///     Get the <see cref="PedBlip" /> attached to this ped.
    /// </summary>
    public PedBlip AttachedBlip { get; internal set; }

    /// <summary>
    ///     Health
    /// </summary>
    public int Health { get; internal set; }

    /// <summary>
    ///     Get or set whether this ped is invincible
    /// </summary>
    public bool IsInvincible
    {
        get => _isInvincible;
        set => Owner.SendNativeCall(Hash.SET_ENTITY_INVINCIBLE, Handle, value);
    }

    /// <summary>
    ///     Attach a blip to this ped.
    /// </summary>
    /// <returns></returns>
    public PedBlip AddBlip()
    {
        AttachedBlip = new PedBlip(this);
        AttachedBlip.Update();
        return AttachedBlip;
    }
}

/// <summary>
///     Represents a vehicle from a client
/// </summary>
public class ServerVehicle : ServerObject
{
    internal Quaternion _quat;

    internal ServerVehicle(Server server) : base(server)
    {
    }

    /// <summary>
    ///     Gets or sets vehicle rotation
    /// </summary>
    public override Vector3 Rotation
    {
        get => _quat.ToEulerAngles().ToDegree();
        set => Owner.SendNativeCall(Hash.SET_ENTITY_ROTATION, Handle, value.X, value.Y, value.Z);
    }

    /// <summary>
    ///     Get this vehicle's quaternion
    /// </summary>
    public Quaternion Quaternion
    {
        get => _quat;
        set
        {
            _quat = value;
            Owner.SendNativeCall(Hash.SET_ENTITY_QUATERNION, Handle, value.X, value.Y, value.Z, value.W);
        }
    }
}

/// <summary>
///     A static blip owned by server.
/// </summary>
public class ServerBlip
{
    private readonly Server Server;


    private bool _bouncing;


    internal BlipColor _color;

    internal string _name = "Beeeeeee";

    internal Vector3 _pos;

    internal int _rot;

    internal float _scale = 1;

    internal BlipSprite _sprite = BlipSprite.Standard;

    internal ServerBlip(Server server)
    {
        Server = server;
    }


    /// <summary>
    ///     Pass this as an argument in CustomEvent or NativeCall to convert this object to handle at client side.
    /// </summary>
    public Tuple<byte, byte[]> Handle => new Tuple<byte, byte[]>(60, BitConverter.GetBytes(ID));

    /// <summary>
    ///     Network ID (not handle!)
    /// </summary>
    public int ID { get; internal set; }

    /// <summary>
    ///     Color of this blip
    /// </summary>
    public BlipColor Color
    {
        get => _color;
        set
        {
            _color = value;
            Update();
        }
    }

    /// <summary>
    ///     Sprite of this blip
    /// </summary>
    public BlipSprite Sprite
    {
        get => _sprite;
        set
        {
            _sprite = value;
            Update();
        }
    }

    /// <summary>
    ///     Scale of this blip
    /// </summary>
    public float Scale
    {
        get => _scale;
        set
        {
            _scale = value;
            Update();
        }
    }

    /// <summary>
    ///     Position of this blip
    /// </summary>
    public Vector3 Position
    {
        get => _pos;
        set
        {
            _pos = value;
            Update();
        }
    }

    /// <summary>
    ///     Rotation of this blip
    /// </summary>
    public int Rotation
    {
        get => _rot;
        set
        {
            _rot = value;
            Update();
        }
    }

    /// <summary>
    ///     Name of this blip
    /// </summary>
    public string Name
    {
        get => _name;
        set
        {
            _name = value;
            Update();
        }
    }

    /// <summary>
    ///     Delete this blip
    /// </summary>
    public void Delete()
    {
        Server.API.SendCustomEvent(CustomEventFlags.Queued, null, CustomEvents.DeleteServerBlip, ID);
        Server.Entities.RemoveServerBlip(ID);
    }

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
        Server.BaseScript.SendServerBlipsTo(new List<ServerBlip> { this });
    }
}

/// <summary>
///     Represent a blip attached to ped.
/// </summary>
public class PedBlip
{
    private bool _bouncing;


    internal BlipColor _color;

    internal float _scale = 1;

    internal BlipSprite _sprite = BlipSprite.Standard;

    internal PedBlip(ServerPed ped)
    {
        Ped = ped;
    }

    /// <summary>
    ///     Get the <see cref="ServerPed" /> that this blip attached to.
    /// </summary>
    public ServerPed Ped { get; internal set; }

    /// <summary>
    ///     Color of this blip
    /// </summary>
    public BlipColor Color
    {
        get => _color;
        set
        {
            _color = value;
            Update();
        }
    }

    /// <summary>
    ///     Sprite of this blip
    /// </summary>
    public BlipSprite Sprite
    {
        get => _sprite;
        set
        {
            _sprite = value;
            Update();
        }
    }

    /// <summary>
    ///     Scale of this blip
    /// </summary>
    public float Scale
    {
        get => _scale;
        set
        {
            _scale = value;
            Update();
        }
    }

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
        Ped.Owner.SendCustomEvent(CustomEventFlags.Queued, CustomEvents.UpdatePedBlip, Ped.Handle, (byte)Color,
            (ushort)Sprite, Scale);
    }
}