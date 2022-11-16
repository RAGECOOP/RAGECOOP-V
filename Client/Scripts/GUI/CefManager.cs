using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using DXHook.Hook.Common;
using GTA;
using GTA.Native;
using RageCoop.Client.CefHost;
using RageCoop.Client.Scripting;
using RageCoop.Core;
using static RageCoop.Client.Shared;

namespace RageCoop.Client.GUI
{
    internal static class CefManager
    {
        private static readonly ConcurrentDictionary<int, CefClient> Clients =
            new ConcurrentDictionary<int, CefClient>();

        private static readonly Overlay CefOverlay = new Overlay
        {
            Elements = new List<IOverlayElement>(),
            Hidden = false
        };

        static CefManager()
        {
            Main.CefRunning = true;
            HookManager.Initialize();
            CefController.Initialize(CefSubProcessPath);
            CefController.OnCefMessage = m => API.Logger.Debug(m);
            HookManager.AddOverLay(CefOverlay);
        }

        public static CefClient ActiveClient { get; set; }

        public static void Tick()
        {
            if (ActiveClient != null)
            {
                Game.DisableAllControlsThisFrame();
                Function.Call(Hash._SET_MOUSE_CURSOR_ACTIVE_THIS_FRAME);
                ActiveClient.Tick();
            }
        }

        public static void KeyDown(Keys key)
        {
        }

        public static void KeyUp(Keys key)
        {
        }

        public static bool DestroyClient(CefClient client)
        {
            lock (Clients)
            {
                if (Clients.TryRemove(client.Id, out var c) && client == c)
                {
                    client.Destroy();
                    CefOverlay.Elements.Remove(client.MainFrame);
                    if (ActiveClient == client) ActiveClient = null;
                    return true;
                }
            }

            return false;
        }

        public static CefClient CreateClient(Size size)
        {
            lock (Clients)
            {
                var id = 0;
                while (id == 0 || Clients.ContainsKey(id)) id = CoreUtils.RandInt(0, int.MaxValue);
                var client = new CefClient(id, size);
                if (Clients.TryAdd(id, client))
                {
                    CefOverlay.Elements.Add(client.MainFrame);
                    return client;
                }

                API.Logger.Warning("Failed to create CefClient");
                client.Destroy();
                return null;
            }
        }

        public static void CleanUp()
        {
            Main.CefRunning = false;
            ActiveClient = null;
            try
            {
                lock (Clients)
                {
                    foreach (var c in Clients.Values) DestroyClient(c);

                    Clients.Clear();
                }

                CefController.ShutDown();
            }
            catch (Exception ex)
            {
                API.Logger.Error(ex);
            }
        }
    }
}