using System.Drawing;
using System.Drawing.Imaging;
using System.Security.Permissions;
using System.Threading;
using System.Windows.Forms;
using RageCoop.Client.CefHost;

namespace CefTest
{
    internal class Program
    {
        [SecurityPermission(SecurityAction.Demand)]
        private static void Main(string[] args)
        {
            CefController.Initialize();
            var thread = new Thread(() =>
            {
                var controller = CefController.Create(0, new Size(800, 600), out var adapter, BufferMode.Dirty);
                Application.Run(new Test(adapter, controller));
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            var controller2 = CefController.Create(1, new Size(800, 600), out var adapter2, BufferMode.Full);
            Application.Run(new Test2(adapter2, controller2));
        }
    }

    internal class Test2 : Test
    {
        public Test2(CefAdapter adapter, CefController controller) : base(adapter, controller)
        {
            Text = "test2: full update";
        }

        protected override void CefPaint(int bufferSize, Rectangle dirtyRect)
        {
            lock (_adapter)
            {
                var size = _adapter.Size;
                var draw = new Bitmap(size.Width, size.Height, size.Width * 4, PixelFormat.Format32bppArgb,
                    _adapter.PtrBuffer);
                _graphics.DrawImage(draw, Point.Empty);
                draw.Dispose();
            }
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
            AutoScaleMode = AutoScaleMode.None;
            Application.EnableVisualStyles();
            Size = adapter.Size;
            _adapter = adapter;
            _controller = controller;
            _adapter.OnPaint += CefPaint;
            controller.LoadUrl("https://www.youtube.com/watch?v=w3rQ3328Tok");
            KeyDown += TestForm_KeyDown;
            MouseClick += TestForm_MouseClick;
            BackColor = Color.AliceBlue;
            _graphics = CreateGraphics();
        }

        private void TestForm_MouseClick(object sender, MouseEventArgs e)
        {
            _controller?.SendMouseClick(e.X, e.Y, 0, GetFrom(e.Button), false, 1);
            _controller?.SendMouseClick(e.X, e.Y, 0, GetFrom(e.Button), true, 1);
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

        private void TestForm_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.R) _controller.LoadUrl("https://www.youtube.com/watch?v=x53lfkuP044/");
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