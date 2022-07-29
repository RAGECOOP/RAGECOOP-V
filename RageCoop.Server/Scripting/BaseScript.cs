using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RageCoop.Core.Scripting;
using RageCoop.Core;

namespace RageCoop.Server.Scripting
{
    internal class BaseScript:ServerScript
    {
        private readonly Server Server;
        public BaseScript(Server server) { Server=server; }
        public override void OnStart()
        {
            API.RegisterCustomEventHandler(CustomEvents.NativeResponse, NativeResponse);
            API.RegisterCustomEventHandler(CustomEvents.OnVehicleDeleted, (e) =>
            {
                API.Entities.RemoveVehicle((int)e.Args[0]);
            });
            API.RegisterCustomEventHandler(CustomEvents.OnPedDeleted, (e) =>
            {
                API.Entities.RemovePed((int)e.Args[0]);
            });
            API.RegisterCustomEventHandler(CustomEvents.WeatherTimeSync, (e) =>
            {
                if (Server.Settings.WeatherTimeSync)
                {
                    foreach (var c in API.GetAllClients().Values)
                    {
                        if (c==e.Client)
                        {
                            continue;
                        }
                        c.SendCustomEventQueued(CustomEvents.WeatherTimeSync, e.Args);
                    }
                }
            });
            API.RegisterCustomEventHandler(CustomEvents.OnPlayerDied, (e) =>
            {
                API.SendCustomEventQueued(API.GetAllClients().Values.Where(x=>x!=e.Client).ToList(),CustomEvents.OnPlayerDied,e.Client.Username);
            });
        }
        public override void OnStop()
        {
        }
        public static void SetAutoRespawn(Client c,bool toggle)
        {
            c.SendCustomEvent(CustomEvents.SetAutoRespawn, toggle );
        }
        public void SetNameTag(Client c, bool toggle)
        {
            foreach(var other in API.GetAllClients().Values)
            {
                if (c==other) { continue; }
                other.SendCustomEvent(CustomEvents.SetDisplayNameTag,c.Player.ID, toggle);
            }
        }
        public void SendServerPropsTo(List<ServerProp> objects,List<Client> clients=null)
        {
            foreach(var obj in objects)
            {
                API.SendCustomEventQueued(clients, CustomEvents.ServerPropSync,obj.ID,  obj.Model ,obj.Position,obj.Rotation );
            }
        }
        public void SendServerBlipsTo(List<ServerBlip> objects, List<Client> clients = null)
        {
            foreach (var obj in objects)
            {
                API.SendCustomEventQueued(clients, CustomEvents.ServerBlipSync,   obj.ID, (ushort)obj.Sprite, (byte)obj.Color, obj.Scale,obj.Position,obj.Rotation,obj.Name  );
            }
        }
        void NativeResponse(CustomEventReceivedArgs e)
        {
            try
            {
                int id = (int)e.Args[0];
                Action<object> callback;
                lock (e.Client.Callbacks)
                {
                    if (e.Client.Callbacks.TryGetValue(id, out callback))
                    {
                        callback(e.Args[1]);
                        e.Client.Callbacks.Remove(id);
                    }
                }
            }
            catch (Exception ex)
            {
                API.Logger.Error("Failed to parse NativeResponse");
                API.Logger.Error(ex);
            }
        }
    }
}
