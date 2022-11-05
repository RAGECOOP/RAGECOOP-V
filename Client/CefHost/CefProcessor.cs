using System;
using System.Drawing;
using BitmapUtil;
using CefSharp;
using CefSharp.Enums;
using CefSharp.OffScreen;
using CefSharp.Structs;
using Size = System.Drawing.Size;

namespace RageCoop.Client.CefHost
{
    internal class CefProcessor : IRenderHandler
    {
        private readonly CefAdapter _adapter;
        private readonly BufferMode _mode;
        private readonly IntPtr _pSharedBuffer;
        private Rect _rect;

        public CefProcessor(Size size, CefAdapter adapter, IntPtr pSharedBuffer, BufferMode mode)
        {
            _adapter = adapter;
            _rect = new Rect(0, 0, size.Width, size.Height);
            _pSharedBuffer = pSharedBuffer;
            _mode = mode;
            _adapter?.Resized(size);
        }

        public Size Size
        {
            get => new Size(_rect.Width, _rect.Height);
            set
            {
                _rect = new Rect(0, 0, value.Width, value.Height);
                _adapter?.Resized(value);
            }
        }

        public void Dispose()
        {
        }

        public ScreenInfo? GetScreenInfo()
        {
            return null;
        }

        public Rect GetViewRect()
        {
            return _rect;
        }

        public bool GetScreenPoint(int viewX, int viewY, out int screenX, out int screenY)
        {
            screenX = viewX;
            screenY = viewY;
            return true;
        }

        public void OnAcceleratedPaint(PaintElementType type, Rect dirtyRect, IntPtr sharedHandle)
        {
        }

        public void OnPaint(PaintElementType type, Rect dirtyRect, IntPtr buffer, int width, int height)
        {
            var dirty = new Rectangle
            {
                Width = dirtyRect.Width,
                Height = dirtyRect.Height,
                X = dirtyRect.X,
                Y = dirtyRect.Y
            };
            var source = new BitmapInfo
            {
                Width = width,
                Height = height,
                BytesPerPixel = 4,
                Scan0 = buffer
            };

            switch (_mode)
            {
                case BufferMode.Dirty:
                    Unsafe.CopyRegion(source, _pSharedBuffer, dirty);
                    break;
                case BufferMode.Full:
                {
                    var target = source;
                    target.Scan0 = _pSharedBuffer;
                    Unsafe.UpdateRegion(source, target, dirty, dirty.Location);
                    break;
                }
            }

            _adapter?.Paint(dirty);
        }

        public void OnCursorChange(IntPtr cursor, CursorType type, CursorInfo customCursorInfo)
        {
        }

        public bool StartDragging(IDragData dragData, DragOperationsMask mask, int x, int y)
        {
            return true;
        }

        public void UpdateDragCursor(DragOperationsMask operation)
        {
        }

        public void OnPopupShow(bool show)
        {
        }

        public void OnPopupSize(Rect rect)
        {
        }

        public void OnImeCompositionRangeChanged(Range selectedRange, Rect[] characterBounds)
        {
            ;
        }

        public void OnVirtualKeyboardRequested(IBrowser browser, TextInputMode inputMode)
        {
            ;
        }
    }
}