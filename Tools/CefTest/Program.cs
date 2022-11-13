using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Security.Permissions;
using System.Threading;
using System.Windows.Forms;
using RageCoop.Client.CefHost;
using SharpDX.DXGI;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using AlphaMode = SharpDX.Direct2D1.AlphaMode;
using SharpD2D;
using SharpD2D.Windows;

namespace CefTest
{
    internal class Program
    {
        [SecurityPermission(SecurityAction.Demand)]
        private static void Main(string[] args)
        {
            WindowHelper.DisableScalingGlobal();
            TimerService.EnableHighPrecisionTimers();
            CefController.Initialize(@"M:\SandBox-Shared\repos\RAGECOOP\RAGECOOP-V\bin\Debug\Client\SubProcess\RageCoop.Client.CefHost.exe");
            CefController.OnCefMessage += s => Console.WriteLine(s);
            Test2();
        }

        static void Test1()
        {

            var controller2 = CefController.Create(1, new Size(1920, 1080), out var adapter2, BufferMode.Dirty);
            Application.Run(new Test(adapter2, controller2));
        }
        private static void Test2()
        {
            var controller = CefController.Create(0, new Size(1920, 1080), out var adapter, BufferMode.Full);
            controller.FrameRate = 60;
            Application.Run(new Test2(adapter, controller));
        }
    }

    internal class Test2 : Test
    {
        private D2DMedia _con;
        public Test2(CefAdapter adapter, CefController controller) : base(adapter, controller)
        {
            Text = "test2: d2d";
            _con = new D2DMedia { Size = Size };
            Controls.Add(_con);
            SizeChanged += (s, e) =>
            {
                _con.Size = Size;
            };
            _con.KeyDown += CefKeyDown;
            _con.MouseDown += (s, e) => MouseKey(e, false);
            _con.MouseUp += (s, e) => MouseKey(e, true);
            _con.MouseMove += (s, e) => controller?.SendMouseMove(Cursor.Position.X, Cursor.Position.Y - (Height - ClientRectangle.Height));

        }

        protected override void CefPaint(int bufferSize, Rectangle dirtyRect)
        {
            try
            {

                lock (_adapter)
                {
                    var size = _adapter.Size;
                    _con.UpdateAndPaint(size.Width, size.Height, size.Width * 4, _adapter.PtrBuffer, new SharpDX.Direct2D1.PixelFormat(Format.B8G8R8A8_UNorm, AlphaMode.Premultiplied));
                }
            }
            catch (Exception ex) { Console.WriteLine(ex); }
        }
    }

    internal class Test : Form
    {
        protected readonly CefAdapter _adapter;
        protected readonly CefController _controller;
        protected readonly Graphics _graphics;

        public Test(CefAdapter adapter, CefController controller)
        {
            Text = "test1: partial update";
            Size = adapter.Size;
            _adapter = adapter;
            _controller = controller;
            _adapter.OnPaint += CefPaint;
            controller.LoadUrl("https://www.youtube.com/watch?v=LXb3EKWsInQ");
            KeyDown += CefKeyDown;
            MouseDown += (s, e) => MouseKey(e, false);
            MouseUp += (s, e) => MouseKey(e, true);
            MouseMove += (s, e) => controller?.SendMouseMove(Cursor.Position.X, Cursor.Position.Y - (Height - ClientRectangle.Height));
            BackColor = Color.AliceBlue;
            _graphics = CreateGraphics();
            AutoScaleMode = AutoScaleMode.None;
            SizeChanged += (s, e) => _controller.Resize(ClientRectangle.Size);
        }

        public void MouseKey(MouseEventArgs e, bool up)
        {
            _controller?.SendMouseClick(e.X, e.Y, 0, GetFrom(e.Button), up, 1);
        }

        public static MouseButton GetFrom(MouseButtons b)
        {
            switch (b)
            {
                case MouseButtons.Left: return MouseButton.Left;
                case MouseButtons.Middle: return MouseButton.Middle;
                case MouseButtons.Right: return MouseButton.Right;
                default:
                    return MouseButton.Left;
            }
        }

        public void CefKeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.R) _controller.LoadUrl("https://www.youtube.com/watch?v=x53lfkuP044/");
            else if (e.KeyCode == Keys.F11)
            {
                if (WindowState != FormWindowState.Maximized)
                {
                    FormBorderStyle = FormBorderStyle.None;
                    WindowState = FormWindowState.Maximized;
                }
                else
                {
                    FormBorderStyle = FormBorderStyle.Sizable;
                    WindowState = FormWindowState.Normal;
                }

            }
        }

        protected virtual void CefPaint(int bufferSize, Rectangle dirtyRect)
        {
            lock (_adapter)
            {
                var draw = new Bitmap(dirtyRect.Width, dirtyRect.Height, dirtyRect.Width * 4,
                    PixelFormat.Format32bppArgb, _adapter.PtrBuffer);
                _graphics.DrawImage(draw, dirtyRect.Location);
                draw.Dispose();
            }
        }
    }
}