using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GTA;
using GTA.Math;
using GTA.Native;
using GTA.UI;
using RageCoop.Core.Scripting;

namespace RageCoop.Client.Scripting
{
    internal static class BaseScript
    {
        private static bool _isHost;

        public static void OnStart()
        {
            API.Events.OnPedDeleted += (s, p) => { API.SendCustomEvent(CustomEvents.OnPedDeleted, p.ID); };
            API.Events.OnVehicleDeleted += (s, p) => { API.SendCustomEvent(CustomEvents.OnVehicleDeleted, p.ID); };
            API.Events.OnPlayerDied += () => { API.SendCustomEvent(CustomEvents.OnPlayerDied); };

            API.RegisterCustomEventHandler(CustomEvents.SetAutoRespawn, SetAutoRespawn);
            API.RegisterCustomEventHandler(CustomEvents.SetDisplayNameTag, SetDisplayNameTag);
            API.RegisterCustomEventHandler(CustomEvents.NativeCall, NativeCall);
            API.RegisterCustomEventHandler(CustomEvents.ServerPropSync, ServerObjectSync);
            API.RegisterCustomEventHandler(CustomEvents.DeleteServerProp, DeleteServerProp);
            API.RegisterCustomEventHandler(CustomEvents.DeleteEntity, DeleteEntity);
            API.RegisterCustomEventHandler(CustomEvents.SetDisplayNameTag, SetNameTag);
            API.RegisterCustomEventHandler(CustomEvents.ServerBlipSync, ServerBlipSync);
            API.RegisterCustomEventHandler(CustomEvents.DeleteServerBlip, DeleteServerBlip);
            API.RegisterCustomEventHandler(CustomEvents.CreateVehicle, CreateVehicle);
            API.RegisterCustomEventHandler(CustomEvents.UpdatePedBlip, UpdatePedBlip);
            API.RegisterCustomEventHandler(CustomEvents.IsHost, e => { _isHost = (bool)e.Args[0]; });
            API.RegisterCustomEventHandler(CustomEvents.WeatherTimeSync, WeatherTimeSync);
            API.RegisterCustomEventHandler(CustomEvents.OnPlayerDied,
                e => { Notification.Show($"~h~{e.Args[0]}~h~ died."); });
            ThreadManager.CreateThread(() =>
             {
                 while (!IsUnloading)
                 {
                     if (Networking.IsOnServer && _isHost)
                         API.QueueAction(() =>
                         {
                             unsafe
                             {
                                 var date = World.CurrentDate;
                                 var weather1 = default(int);
                                 var weather2 = default(int);
                                 var percent2 = default(float);
                                 Call(GET_CURR_WEATHER_STATE, &weather1, &weather2, &percent2);
                                 API.SendCustomEvent(CustomEvents.WeatherTimeSync, date.Year, date.Month, date.Day, date.Hour, date.Minute,
                                 date.Second, weather1, weather2, percent2);
                             }
                         });

                     Thread.Sleep(1000);
                 }
             }, "BaseScript");
        }

        private static void WeatherTimeSync(CustomEventReceivedArgs e)
        {
            World.CurrentDate = new DateTime((int)e.Args[0], (int)e.Args[1], (int)e.Args[2], (int)e.Args[3], (int)e.Args[4], (int)e.Args[5]);
            Call(SET_CURR_WEATHER_STATE, (int)e.Args[6], (int)e.Args[7], (float)e.Args[8]);
        }

        private static void SetDisplayNameTag(CustomEventReceivedArgs e)
        {
            var p = PlayerList.GetPlayer((int)e.Args[0]);
            if (p != null) p.DisplayNameTag = (bool)e.Args[1];
        }

        private static void UpdatePedBlip(CustomEventReceivedArgs e)
        {
            var p = Entity.FromHandle((int)e.Args[0]);
            if (p == null) return;
            if (p.Handle == Game.Player.Character.Handle)
            {
                API.Config.BlipColor = (BlipColor)(byte)e.Args[1];
                API.Config.BlipSprite = (BlipSprite)(ushort)e.Args[2];
                API.Config.BlipScale = (float)e.Args[3];
            }
            else
            {
                var b = p.AttachedBlip;
                if (b == null) b = p.AddBlip();
                b.Color = (BlipColor)(byte)e.Args[1];
                b.Sprite = (BlipSprite)(ushort)e.Args[2];
                b.Scale = (float)e.Args[3];
            }
        }

        private static void CreateVehicle(CustomEventReceivedArgs e)
        {
            var vehicleModel = (Model)e.Args[1];
            vehicleModel.Request(1000);
            Vehicle veh;
            while ((veh = World.CreateVehicle(vehicleModel, (Vector3)e.Args[2], (float)e.Args[3])) == null)
                Thread.Sleep(10);
            veh.CanPretendOccupants = false;
            var v = new SyncedVehicle
            {
                ID = (int)e.Args[0],
                MainVehicle = veh,
                OwnerID = LocalPlayerID
            };
            EntityPool.Add(v);
        }

        private static void DeleteServerBlip(CustomEventReceivedArgs e)
        {
            if (EntityPool.ServerBlips.TryGetValue((int)e.Args[0], out var blip))
            {
                EntityPool.ServerBlips.Remove((int)e.Args[0]);
                blip?.Delete();
            }
        }

        private static void ServerBlipSync(CustomEventReceivedArgs obj)
        {
            var id = (int)obj.Args[0];
            var sprite = (BlipSprite)(ushort)obj.Args[1];
            var color = (BlipColor)(byte)obj.Args[2];
            var scale = (float)obj.Args[3];
            var pos = (Vector3)obj.Args[4];
            var rot = (int)obj.Args[5];
            var name = (string)obj.Args[6];
            if (!EntityPool.ServerBlips.TryGetValue(id, out var blip))
                EntityPool.ServerBlips.Add(id, blip = World.CreateBlip(pos));
            blip.Sprite = sprite;
            blip.Color = color;
            blip.Scale = scale;
            blip.Position = pos;
            blip.Rotation = rot;
            blip.Name = name;
        }


        private static void DeleteEntity(CustomEventReceivedArgs e)
        {
            Entity.FromHandle((int)e.Args[0])?.Delete();
        }

        private static void SetNameTag(CustomEventReceivedArgs e)
        {
            var p = PlayerList.GetPlayer((int)e.Args[0]);
            if (p != null) p.DisplayNameTag = (bool)e.Args[1];
        }

        private static void SetAutoRespawn(CustomEventReceivedArgs args)
        {
            API.Config.EnableAutoRespawn = (bool)args.Args[0];
        }

        private static void DeleteServerProp(CustomEventReceivedArgs e)
        {
            var id = (int)e.Args[0];
            if (EntityPool.ServerProps.TryGetValue(id, out var prop))
            {
                EntityPool.ServerProps.Remove(id);
                prop?.MainProp?.Delete();
            }
        }

        private static void ServerObjectSync(CustomEventReceivedArgs e)
        {
            SyncedProp prop;
            var id = (int)e.Args[0];
            lock (EntityPool.PropsLock)
            {
                if (!EntityPool.ServerProps.TryGetValue(id, out prop))
                    EntityPool.ServerProps.Add(id, prop = new SyncedProp(id));
            }

            prop.LastSynced = Ticked + 1;
            prop.Model = (Model)e.Args[1];
            prop.Position = (Vector3)e.Args[2];
            prop.Rotation = (Vector3)e.Args[3];
            prop.Model.Request(1000);
            prop.Update();
        }

        private static void NativeCall(CustomEventReceivedArgs e)
        {
            var arguments = new List<InputArgument>();
            int i;
            var ty = (byte)e.Args[0];
            var returnType = (TypeCode)ty;
            i = returnType == TypeCode.Empty ? 1 : 2;
            var hash = (Hash)e.Args[i++];
            for (; i < e.Args.Length; i++) arguments.Add(GetInputArgument(e.Args[i]));

            if (returnType == TypeCode.Empty)
            {
                Call(hash, arguments.ToArray());
                return;
            }

            var id = (int)e.Args[1];


            switch (returnType)
            {
                case TypeCode.Boolean:
                    API.SendCustomEvent(CustomEvents.NativeResponse, id,
                        Call<bool>(hash, arguments.ToArray()));
                    break;
                case TypeCode.Byte:
                    API.SendCustomEvent(CustomEvents.NativeResponse, id,
                        Call<byte>(hash, arguments.ToArray()));
                    break;
                case TypeCode.Char:
                    API.SendCustomEvent(CustomEvents.NativeResponse, id,
                        Call<char>(hash, arguments.ToArray()));
                    break;

                case TypeCode.Single:
                    API.SendCustomEvent(CustomEvents.NativeResponse, id,
                        Call<float>(hash, arguments.ToArray()));
                    break;

                case TypeCode.Double:
                    API.SendCustomEvent(CustomEvents.NativeResponse, id,
                        Call<double>(hash, arguments.ToArray()));
                    break;

                case TypeCode.Int16:
                    API.SendCustomEvent(CustomEvents.NativeResponse, id,
                        Call<short>(hash, arguments.ToArray()));
                    break;

                case TypeCode.Int32:
                    API.SendCustomEvent(CustomEvents.NativeResponse, id, Call<int>(hash, arguments.ToArray()));
                    break;

                case TypeCode.Int64:
                    API.SendCustomEvent(CustomEvents.NativeResponse, id,
                        Call<long>(hash, arguments.ToArray()));
                    break;

                case TypeCode.String:
                    API.SendCustomEvent(CustomEvents.NativeResponse, id,
                        Call<string>(hash, arguments.ToArray()));
                    break;

                case TypeCode.UInt16:
                    API.SendCustomEvent(CustomEvents.NativeResponse, id,
                        Call<ushort>(hash, arguments.ToArray()));
                    break;

                case TypeCode.UInt32:
                    API.SendCustomEvent(CustomEvents.NativeResponse, id,
                        Call<uint>(hash, arguments.ToArray()));
                    break;

                case TypeCode.UInt64:
                    API.SendCustomEvent(CustomEvents.NativeResponse, id,
                        Call<ulong>(hash, arguments.ToArray()));
                    break;
            }
        }

        private static InputArgument GetInputArgument(object obj)
        {
            // Implicit conversion
            switch (obj)
            {
                case byte stuff:
                    return stuff;
                case short stuff:
                    return stuff;
                case ushort stuff:
                    return stuff;
                case int stuff:
                    return stuff;
                case uint stuff:
                    return stuff;
                case long stuff:
                    return stuff;
                case ulong stuff:
                    return stuff;
                case float stuff:
                    return stuff;
                case bool stuff:
                    return stuff;
                case string stuff:
                    return stuff;
                default:
                    return default;
            }
        }
    }
}