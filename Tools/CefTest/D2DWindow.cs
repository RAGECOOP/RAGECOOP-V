using System;
using System.Windows.Forms;
using SharpD2D.Drawing;
using SharpD2D.Windows;
using SharpDX.Direct2D1;
using Image = SharpD2D.Drawing.Image;

namespace CefTest
{
    public class D2DMedia : Control
    {
        private Canvas _canvas;
        private Image _img;

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            Console.WriteLine("Initialize");
            _canvas = new Canvas(Handle);
            _canvas.SetupGraphics += _canvas_SetupGraphics;
            _canvas.Initialize();
            _canvas.DrawGraphics += _canvas_DrawGraphics;
        }

        private void _canvas_DrawGraphics(object sender, DrawGraphicsEventArgs e)
        {
            e.Graphics.BeginScene();
            e.Graphics.DrawImage(_img, default(PointF));
            e.Graphics.EndScene();
        }

        public void UpdateAndPaint(int width, int height, int pitch, IntPtr scan0, PixelFormat format)
        {
            _img.Update(width, height, pitch, scan0, format);
            _canvas.SafeDraw();
        }

        private void _canvas_SetupGraphics(object sender, SetupGraphicsEventArgs e)
        {
            _img = e.Graphics.CreateImage();
        }
    }
}