﻿using System;
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
        }
        public override void OnStop()
        {
        }
        public void SetAutoRespawn(Client c,bool toggle)
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
                API.SendCustomEvent(clients, CustomEvents.ServerPropSync,obj.ID,  obj.Model ,obj.Position,obj.Rotation );
            }
        }
        public void SendServerBlipsTo(List<ServerBlip> objects, List<Client> clients = null)
        {
            foreach (var obj in objects)
            {
                API.SendCustomEvent(clients, CustomEvents.ServerBlipSync,   obj.ID, (short)obj.Sprite, (byte)obj.Color, obj.Scale,obj.Position,obj.Rotation,obj.Name  );
            }
        }
        void NativeResponse(CustomEventReceivedArgs e)
        {
            try
            {
                int id = (int)e.Args[0];
                Action<object> callback;
                lock (e.Sender.Callbacks)
                {
                    if (e.Sender.Callbacks.TryGetValue(id, out callback))
                    {
                        callback(e.Args[1]);
                        e.Sender.Callbacks.Remove(id);
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
