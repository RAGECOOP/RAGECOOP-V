using System;
using System.Collections.Generic;
using GTA.Native;
using RageCoop.Core.Scripting;

namespace RageCoop.Client.Scripting
{
    internal class BaseScript : ClientScript
    {
        public override void OnStart()
        {
            API.RegisterCustomEventHandler(CustomEvents.SetAutoRespawn,SetAutoRespawn);
            API.RegisterCustomEventHandler(CustomEvents.NativeCall,NativeCall);
            API.Events.OnPedDeleted+=(s,p) => { API.SendCustomEvent(CustomEvents.OnPedDeleted,p.ID); };
            API.Events.OnVehicleDeleted+=(s, p) => { API.SendCustomEvent(CustomEvents.OnVehicleDeleted, p.ID); };

        }

        public override void OnStop()
        {
        }
        private void SetAutoRespawn(CustomEventReceivedArgs args)
        {
            API.Config.EnableAutoRespawn=(bool)args.Args[0];
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
