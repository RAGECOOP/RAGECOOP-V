using System;
using System.Collections.Generic;
using GTA.Native;
using GTA.Math;
using GTA;
using RageCoop.Core;
using RageCoop.Core.Scripting;
using System.Linq;

namespace RageCoop.Client.Scripting
{
    internal class BaseScript : ClientScript
    {
        public override void OnStart()
        {
            API.RegisterCustomEventHandler(CustomEvents.SetAutoRespawn,SetAutoRespawn);
            API.RegisterCustomEventHandler(CustomEvents.NativeCall,NativeCall);
            API.RegisterCustomEventHandler(CustomEvents.ServerPropSync, ServerObjectSync);
            API.RegisterCustomEventHandler(CustomEvents.DeleteServerProp, DeleteServerProp);
            API.RegisterCustomEventHandler(CustomEvents.DeleteEntity, DeleteEntity);
            API.RegisterCustomEventHandler(CustomEvents.SetDisplayNameTag, SetNameTag);
            API.RegisterCustomEventHandler(CustomEvents.SetEntity, SetEntity);
            API.RegisterCustomEventHandler(CustomEvents.ServerBlipSync, ServerBlipSync);
            API.RegisterCustomEventHandler(CustomEvents.DeleteServerBlip, DeleteServerBlip);
            API.Events.OnPedDeleted+=(s,p) => { API.SendCustomEvent(CustomEvents.OnPedDeleted,p.ID); };
            API.Events.OnVehicleDeleted+=(s, p) => { API.SendCustomEvent(CustomEvents.OnVehicleDeleted, p.ID); };
        }

        private void DeleteServerBlip(CustomEventReceivedArgs e)
        {
            if (EntityPool.ServerBlips.TryGetValue((int)e.Args[0], out var blip))
            {
                EntityPool.ServerBlips.Remove((int)e.Args[0]);
                API.QueueAction(()=>{
                    blip?.Delete();
                });
            }
        }

        private void ServerBlipSync(CustomEventReceivedArgs obj)
        {
            int id= (int)obj.Args[0];
            var sprite=(BlipSprite)(short)obj.Args[1];
            var color = (BlipColor)(byte)obj.Args[2];
            var scale=(float)obj.Args[3];
            var pos=(Vector3)obj.Args[4];
            int rot= (int)obj.Args[5];
            var name=(string)obj.Args[6];
            Blip blip;
            API.QueueAction(() =>
            {
                // Main.Logger.Debug($"{sprite},{color},{scale},{pos},{rot}");
                if (!EntityPool.ServerBlips.TryGetValue(id, out blip))
                {
                    EntityPool.ServerBlips.Add(id, blip=World.CreateBlip(pos));
                }
                blip.Sprite = sprite;
                blip.Color = color;
                blip.Scale = scale;
                blip.Position = pos;
                blip.Rotation = rot;
                blip.Name = name;
            });
        }

        private void SetEntity(CustomEventReceivedArgs obj)
        {
            Main.QueueAction(() =>
            {
                var e=Entity.FromHandle((int)obj.Args[0]);
                e.Position = (Vector3)obj.Args[1];
                e.Rotation = (Vector3)obj.Args[2];
            });
        }

        private void DeleteEntity(CustomEventReceivedArgs e)
        {
            Entity.FromHandle((int)e.Args[0]).Delete();
        }

        public override void OnStop()
        {
        }
        private void SetNameTag(CustomEventReceivedArgs e)
        {
            var p = EntityPool.GetPedByID((int)e.Args[0]);
            if(p!= null)
            {
                p.DisplayNameTag=(bool)e.Args[1];
            }
        }
        private void SetAutoRespawn(CustomEventReceivedArgs args)
        {
            API.Config.EnableAutoRespawn=(bool)args.Args[0];
        }
        private void DeleteServerProp(CustomEventReceivedArgs e)
        {
            var id = (int)e.Args[0];
            if (EntityPool.ServerProps.TryGetValue(id, out var prop))
            {
                EntityPool.ServerProps.Remove(id);
                API.QueueAction(() => { prop?.MainProp?.Delete(); });
            }
        }
        private void ServerObjectSync(CustomEventReceivedArgs e)
        {
            SyncedProp prop;
            var id = (int)e.Args[0];
            lock (EntityPool.PropsLock)
            {
                if (!EntityPool.ServerProps.TryGetValue(id, out prop))
                {
                    EntityPool.ServerProps.Add(id, prop=new SyncedProp(id));
                }
            }
            prop.LastSynced=Main.Ticked+1;
            prop.ModelHash= (Model)e.Args[1];
            prop.Position=(Vector3)e.Args[2];
            prop.Rotation=(Vector3)e.Args[3];
            Main.QueueAction(() => { prop.Update(); });
        }
        private void NativeCall(CustomEventReceivedArgs e)
        {
            List<InputArgument> arguments = new List<InputArgument>();
            int i;
            var ty = (byte)e.Args[0];
            TypeCode returnType=(TypeCode)ty;
            i = returnType==TypeCode.Empty ? 1 : 2;
            var hash = (Hash)e.Args[i++];
            for(; i<e.Args.Count;i++)
            {
                arguments.Add(GetInputArgument(e.Args[i]));
            }
            Main.QueueAction(() =>
            {

                if (returnType==TypeCode.Empty)
                {
                    Function.Call(hash, arguments.ToArray());
                    return;
                }
                var t = returnType;
                int id = (int)e.Args[1];


                switch (returnType)
                {
                    case TypeCode.Boolean:
                        API.SendCustomEvent(CustomEvents.NativeResponse,
             new List<object> { id, Function.Call<bool>(hash, arguments.ToArray()) });
                        break;
                    case TypeCode.Byte:
                        API.SendCustomEvent(CustomEvents.NativeResponse,
             new List<object> { id, Function.Call<byte>(hash, arguments.ToArray()) });
                        break;
                    case TypeCode.Char:
                        API.SendCustomEvent(CustomEvents.NativeResponse,
             new List<object> { id, Function.Call<char>(hash, arguments.ToArray()) });
                        break;

                    case TypeCode.Single:
                        API.SendCustomEvent(CustomEvents.NativeResponse,
             new List<object> { id, Function.Call<float>(hash, arguments.ToArray()) });
                        break;

                    case TypeCode.Double:
                        API.SendCustomEvent(CustomEvents.NativeResponse,
             new List<object> { id, Function.Call<double>(hash, arguments.ToArray()) });
                        break;

                    case TypeCode.Int16:
                        API.SendCustomEvent(CustomEvents.NativeResponse,
             new List<object> { id, Function.Call<short>(hash, arguments.ToArray()) });
                        break;

                    case TypeCode.Int32:
                        API.SendCustomEvent(CustomEvents.NativeResponse,
             new List<object> { id, Function.Call<int>(hash, arguments.ToArray()) });
                        break;

                    case TypeCode.Int64:
                        API.SendCustomEvent(CustomEvents.NativeResponse,
             new List<object> { id, Function.Call<long>(hash, arguments.ToArray()) });
                        break;

                    case TypeCode.String:
                        API.SendCustomEvent(CustomEvents.NativeResponse,
             new List<object> { id, Function.Call<string>(hash, arguments.ToArray()) });
                        break;

                    case TypeCode.UInt16:
                        API.SendCustomEvent(CustomEvents.NativeResponse,
             new List<object> { id, Function.Call<ushort>(hash, arguments.ToArray()) });
                        break;

                    case TypeCode.UInt32:
                        API.SendCustomEvent(CustomEvents.NativeResponse,
             new List<object> { id, Function.Call<uint>(hash, arguments.ToArray()) });
                        break;

                    case TypeCode.UInt64:
                        API.SendCustomEvent(CustomEvents.NativeResponse,
             new List<object> { id, Function.Call<ulong>(hash, arguments.ToArray()) });
                        break;
                }
            });

        }
        private InputArgument GetInputArgument(object obj)
        {
            // Implicit conversion
            switch (obj)
            {
                case byte _:
                    return (byte)obj;
                case short _:
                    return (short)obj;
                case ushort _:
                    return (ushort)obj;
                case int _:
                    return (int)obj;
                case uint _:
                    return (uint)obj;
                case long _:
                    return (long)obj;
                case ulong _:
                    return (ulong)obj;
                case float _:
                    return (float)obj;
                case bool _:
                    return (bool)obj;
                case string _:
                    return (obj as string);
                default:
                    return null;
            }
        } 
    }
}
