using System.Collections.Generic;
using DXHook.Hook;
using DXHook.Hook.Common;
using DXHook.Interface;
using RageCoop.Client.Scripting;

namespace RageCoop.Client.GUI
{
    internal static class HookManager
    {
        public static readonly CaptureInterface Interface = new CaptureInterface();
        private static DXHookD3D11 _hook;
        public static Overlay DefaultOverlay = new Overlay();
        public static bool Hooked => _hook != null;

        public static void GetOverlays()
        {
            new List<IOverlay>(_hook.Overlays);
        }

        public static void AddOverLay(IOverlay ol)
        {
            _hook.Overlays.Add(ol);
            _hook.IsOverlayUpdatePending = true;
        }

        public static void RemoveOverlay(IOverlay ol)
        {
            _hook.Overlays.Remove(ol);
            _hook.IsOverlayUpdatePending = true;
        }

        public static void Initialize()
        {
            if (_hook != null) return;
            _hook = new DXHookD3D11(Interface);
            _hook.Config = new CaptureConfig
            {
                Direct3DVersion = Direct3DVersion.Direct3D11,
                ShowOverlay = true
            };
            _hook.Overlays = new List<IOverlay>();
            _hook.Hook();
            _hook.OnPresent += Present;
            DefaultOverlay.Elements = new List<IOverlayElement>();
            AddOverLay(DefaultOverlay);
            Interface.RemoteMessage += m => { API.Logger.Debug("DXHook: " + m.Message); };
            API.Logger.Debug("Hooked DX3D11");
        }

        private static void Present()
        {
        }

        public static void CleanUp()
        {
            _hook?.Cleanup();
            _hook?.Dispose();
            _hook = null;
        }
    }
}