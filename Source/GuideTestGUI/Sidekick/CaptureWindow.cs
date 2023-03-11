using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace Sidekick.Sidekick
{
    //Автор: Goal http://www.cyberforum.ru/csharp-net/thread723513.html
    static public class CaptureWindow
    {
        public static Image GetCaptureWindowOld(IntPtr handle)
        {
            // get te hDC of the target window
            IntPtr hdcSrc = User.GetWindowDC(handle);
            // get the size
            User.RECT windowRect = new User.RECT();
            User.GetWindowRect(handle, ref windowRect);
            int width = windowRect.right - windowRect.left;
            int height = windowRect.bottom - windowRect.top;
            // create a device context we can copy to
            IntPtr hdcDest = GDI.CreateCompatibleDC(hdcSrc);
            // create a bitmap we can copy it to,
            // using GetDeviceCaps to get the width/height
            IntPtr hBitmap = GDI.CreateCompatibleBitmap(hdcSrc, width, height);
            // select the bitmap object
            IntPtr hOld = GDI.SelectObject(hdcDest, hBitmap);
            // bitblt over
            GDI.BitBlt(hdcDest, 0, 0, width, height, hdcSrc, 0, 0, GDI.SRCCOPY);
            // restore selection
            GDI.SelectObject(hdcDest, hOld);
            // clean up 
            GDI.DeleteDC(hdcDest);
            User.ReleaseDC(handle, hdcSrc);
            // get a .NET image object for it
            Image img = Image.FromHbitmap(hBitmap);
            // free up the Bitmap object
            GDI.DeleteObject(hBitmap);
            return img;
        }

        public static Rectangle GetWindowRect(IntPtr handle)
        {
            User.RECT windowRect = new User.RECT();

            if (Environment.OSVersion.Version.Major < 6)
            {
                // Если Win XP и ранее то используем старый способ
                User.GetWindowRect(handle, ref windowRect);
            }
            else
            {
                var res = -1;
                try
                {
                    res = User.DwmGetWindowAttribute(handle, 9, out windowRect, Marshal.SizeOf(typeof(User.RECT)));
                }
                catch { }
                if (res < 0) User.GetWindowRect(handle, ref windowRect);
            }

            var bounds = new Rectangle(windowRect.left, windowRect.top, windowRect.right - windowRect.left, windowRect.bottom - windowRect.top);

            return bounds;
        }
        public static Rectangle GetWindowBorder(IntPtr handle)
        {
            User.RECT windowRect = new User.RECT();

            User.GetWindowRect(handle, ref windowRect);
            var bounds = new Rectangle(windowRect.left, windowRect.top, windowRect.right - windowRect.left, windowRect.bottom - windowRect.top);

            var rect = GetWindowRect(handle);

            bounds.X -= rect.X;
            bounds.Y -= rect.Y;
            bounds.Width -= rect.Width;
            bounds.Height -= rect.Height;

            return bounds;
        }
        public static void SetWindowRect(IntPtr handle, Rectangle rect)
        {
            var border = GetWindowBorder(handle);
            rect.X += border.X;
            rect.Y += border.Y;
            rect.Width += border.Width;
            rect.Height += border.Height;
            User.SetWindowPos(handle, 0, rect.Left, rect.Top, rect.Width, rect.Height, 0);
        }

        public static Bitmap GetCaptureWindow(IntPtr handle)
        {
            var bounds = GetWindowRect(handle);

            var result = new Bitmap(bounds.Width, bounds.Height);

            using (var g = Graphics.FromImage(result))
            {
                g.CopyFromScreen(new Point(bounds.Left, bounds.Top), Point.Empty, bounds.Size);
            }
            return result;
        }


        /// <summary>
        /// Helper class containing Gdi32 API functions
        /// </summary>
        private class GDI
        {

            public const int SRCCOPY = 0x00CC0020; // BitBlt dwRop parameter
            [DllImport("gdi32.dll")]
            public static extern bool BitBlt(IntPtr hObject, int nXDest, int nYDest,
                int nWidth, int nHeight, IntPtr hObjectSource,
                int nXSrc, int nYSrc, int dwRop);
            [DllImport("gdi32.dll")]
            public static extern IntPtr CreateCompatibleBitmap(IntPtr hDC, int nWidth,
                int nHeight);
            [DllImport("gdi32.dll")]
            public static extern IntPtr CreateCompatibleDC(IntPtr hDC);
            [DllImport("gdi32.dll")]
            public static extern bool DeleteDC(IntPtr hDC);
            [DllImport("gdi32.dll")]
            public static extern bool DeleteObject(IntPtr hObject);
            [DllImport("gdi32.dll")]
            public static extern IntPtr SelectObject(IntPtr hDC, IntPtr hObject);
        }
        /// <summary>
        /// Helper class containing User32 API functions
        /// </summary>
        private class User
        {
            [StructLayout(LayoutKind.Sequential)]
            public struct RECT
            {
                public int left;
                public int top;
                public int right;
                public int bottom;
            }
            [DllImport("user32.dll")]
            public static extern IntPtr GetDesktopWindow();
            [DllImport("user32.dll")]
            public static extern IntPtr GetWindowDC(IntPtr hWnd);
            [DllImport("user32.dll")]
            public static extern IntPtr ReleaseDC(IntPtr hWnd, IntPtr hDC);
            [DllImport("user32.dll")]
            public static extern IntPtr GetWindowRect(IntPtr hWnd, ref RECT rect);
            [DllImport("user32.dll", EntryPoint = "SetWindowPos")]
            public static extern IntPtr SetWindowPos(IntPtr hWnd, int hWndInsertAfter, int x, int Y, int cx, int cy, int wFlags);
            [DllImport(@"dwmapi.dll")]
            public static extern int DwmGetWindowAttribute(IntPtr hwnd, int dwAttribute, out RECT pvAttribute, int cbAttribute);
        }
    }
}
