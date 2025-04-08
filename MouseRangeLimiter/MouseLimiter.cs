using System;
using System.Runtime.InteropServices;

namespace MouseRangeLimiter
{
    public class MouseLimiter
    {
        [DllImport("user32.dll")]
        static extern bool GetCursorPos(out POINT lpPoint);

        [DllImport("user32.dll")]
        static extern bool ClipCursor([In] ref RECT lpRect);

        [DllImport("user32.dll")]
        static extern bool ClipCursor(IntPtr lpRect);

        [DllImport("user32.dll")]
        static extern short GetAsyncKeyState(int vKey);

        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int X;
            public int Y;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        private RECT? clipRect;
        private bool isClippingEnabled = false;

        public void SetClipArea(int width, int height)
        {
            GetCursorPos(out POINT currentPos);

            clipRect = new RECT
            {
                Left = currentPos.X - width / 2,
                Top = currentPos.Y - height / 2,
                Right = currentPos.X + width / 2,
                Bottom = currentPos.Y + height / 2
            };

            ApplyClip();
        }

        public void ApplyClip()
        {
            if (clipRect.HasValue && isClippingEnabled)
            {
                RECT rect = clipRect.Value;
                ClipCursor(ref rect);
            }
        }

        public void ReleaseClip()
        {
            ClipCursor(IntPtr.Zero);
        }

        public void ToggleClipping()
        {
            isClippingEnabled = !isClippingEnabled;

            if (isClippingEnabled)
                ApplyClip();
            else
                ReleaseClip();
        }

        public void CheckHotkey()
        {
            if ((GetAsyncKeyState(0x71) & 0x8000) != 0) // F2
            {
                SetClipArea(200, 200);
            }

            if ((GetAsyncKeyState(0x74) & 0x8000) != 0) // F5
            {
                ToggleClipping();
            }
        }
    }
}