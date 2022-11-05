using System;
using System.Drawing;
using DXHook.Hook.Common;
using GTA;
using RageCoop.Client.CefHost;
using RageCoop.Client.Scripting;

namespace RageCoop.Client.GUI
{
    public class CefClient
    {
        public readonly int Id;
        internal CefAdapter Adapter;
        internal CefController Controller;
        internal ImageElement MainFrame;

        internal CefClient(int id, Size size)
        {
            Id = id;
            Controller = CefController.Create(id, size, out Adapter, BufferMode.Full);
            MainFrame = new ImageElement(size.Width, size.Height, 4, Adapter.PtrBuffer);
            Adapter.OnPaint += (len, dirty) =>
            {
                try
                {
                    // Image is using same shared buffer, so just need to make it re-copied to GPU
                    MainFrame.Invalidate();
                }
                catch (Exception ex)
                {
                    API.Logger.Error(ex);
                }
            };
        }

        internal void Destroy()
        {
            Controller.Dispose();
            Adapter.Dispose();
            MainFrame.Dispose();
        }

        public Point GetLocationInFrame(Point screenPos)
        {
            screenPos.X -= MainFrame.Location.X;
            screenPos.Y -= MainFrame.Location.Y;
            return screenPos;
        }

        public Point GetLocationInCef(Point screenPos)
        {
            var p = GetLocationInFrame(screenPos);
            p.X = (int)(p.X / Scale);
            p.Y = (int)(p.Y / Scale);
            return p;
        }

        internal bool PointInArea(Point screen)
        {
            screen = GetLocationInFrame(screen);
            return screen.X.IsBetween(0, Width) && screen.Y.IsBetween(0, Height);
        }

        internal void Tick()
        {
            var mousePos = Util.CursorPosition;
            if (!PointInArea(mousePos)) return;
            var pos = GetLocationInCef(mousePos);
            if (Game.IsControlJustPressed(Control.CursorAccept))
                Controller.SendMouseClick(pos.X, pos.Y, 0, MouseButton.Left, false, 1);
            else if (Game.IsControlJustReleased(Control.CursorAccept))
                Controller.SendMouseClick(pos.X, pos.Y, 0, MouseButton.Left, true, 1);
        }

        #region FRAME-APPERANCE

        public float Scale
        {
            get => MainFrame.Scale;
            set => MainFrame.Scale = value;
        }

        public Color Tint
        {
            get => MainFrame.Tint;
            set => MainFrame.Tint = value;
        }

        public byte Opacity
        {
            get => MainFrame.Opacity;
            set => MainFrame.Opacity = value;
        }

        public Point Location
        {
            get => MainFrame.Location;
            set => MainFrame.Location = value;
        }

        public int Width => MainFrame.Width;
        public int Height => MainFrame.Height;

        public bool Hidden
        {
            get => MainFrame.Hidden;
            set => MainFrame.Hidden = value;
        }

        #endregion
    }
}