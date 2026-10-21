// Cemengal: take_screenshot (docs/decisions/0010), so an agent can see what an action did, as with
// CEM_RevitMCP's take_screenshot. "view" asks Navisworks to render its 3D view (View.GenerateImage),
// without the user interface; "window" captures the whole main window, panels and dialogs included
// (PrintWindow, with the flag that also copies DirectX content). Both are scaled to MaxSide at most
// and returned as a PNG in memory; nothing is written to disk (docs/decisions/0007).
using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using Autodesk.Navisworks.Api;
using CEM_NavisIAModeler.Contracts;

namespace CEM_NavisIAModeler.Services.Backends
{
    public static class ScreenCapture
    {
        /// <summary>The longest side when the caller gives none.</summary>
        public const int DefaultSide = 1600;

        /// <summary>The longest side ever returned.</summary>
        public const int MaxSide = 4096;

        private const uint PW_RENDERFULLCONTENT = 2;

        [StructLayout(LayoutKind.Sequential)]
        private struct Rect { public int Left, Top, Right, Bottom; }

        [DllImport("user32.dll")]
        private static extern bool GetWindowRect(IntPtr hWnd, out Rect rect);

        [DllImport("user32.dll")]
        private static extern bool PrintWindow(IntPtr hWnd, IntPtr hdc, uint flags);

        [DllImport("user32.dll")]
        private static extern bool IsIconic(IntPtr hWnd);

        /// <summary>Captures Navisworks; call it on the UI thread. <paramref name="mode"/> is "view" or "window".</summary>
        public static ScreenshotDto Capture(string mode, int longestSide)
        {
            var side = longestSide <= 0 ? DefaultSide : Math.Max(64, Math.Min(MaxSide, longestSide));
            var window = string.Equals(mode, "window", StringComparison.OrdinalIgnoreCase);
            var doc = Application.ActiveDocument;

            using (var image = window ? CaptureWindow() : RenderView(doc, side))
            using (var fitted = Fit(image, side))
            using (var png = new MemoryStream())
            {
                fitted.Save(png, ImageFormat.Png);
                return new ScreenshotDto
                {
                    mode = window ? "window" : "view",
                    width = fitted.Width,
                    height = fitted.Height,
                    png_base64 = Convert.ToBase64String(png.ToArray()),
                    document_title = doc?.Title ?? ""
                };
            }
        }

        /// <summary>
        /// Navisworks renders the active view at the size of its viewport, scaled to <paramref name="side"/>,
        /// with the view's section cuts.
        /// </summary>
        private static Bitmap RenderView(Document doc, int side)
        {
            var view = doc?.ActiveView ?? throw new InvalidOperationException("No document is open, so there is no 3D view to render.");
            int width = view.Width, height = view.Height;
            if (width <= 0 || height <= 0) { width = side; height = side * 9 / 16; }

            var scale = Math.Min(1.0, (double)side / Math.Max(width, height));
            width = Math.Max(1, (int)(width * scale));
            height = Math.Max(1, (int)(height * scale));

            return view.GenerateImage(ImageGenerationStyle.ScenePlusOverlay, width, height, enableSectioning: true)
                   ?? throw new InvalidOperationException("Navisworks rendered no image of the view.");
        }

        /// <summary>The Navisworks main window as it is on screen, even where other windows cover it.</summary>
        private static Bitmap CaptureWindow()
        {
            var hwnd = Process.GetCurrentProcess().MainWindowHandle;
            if (hwnd == IntPtr.Zero) throw new InvalidOperationException("Navisworks' main window was not found.");
            if (IsIconic(hwnd)) throw new InvalidOperationException("Navisworks is minimized; restore it, or use mode \"view\".");
            if (!GetWindowRect(hwnd, out var r) || r.Right <= r.Left || r.Bottom <= r.Top)
                throw new InvalidOperationException("Navisworks' main window has no size.");

            var bitmap = new Bitmap(r.Right - r.Left, r.Bottom - r.Top, PixelFormat.Format32bppArgb);
            using (var g = System.Drawing.Graphics.FromImage(bitmap))
            {
                var hdc = g.GetHdc();
                bool printed;
                try { printed = PrintWindow(hwnd, hdc, PW_RENDERFULLCONTENT); }
                finally { g.ReleaseHdc(hdc); }
                if (!printed) g.CopyFromScreen(r.Left, r.Top, 0, 0, bitmap.Size);
            }
            return bitmap;
        }

        /// <summary>The image itself when it fits in <paramref name="side"/>, else a scaled copy.</summary>
        private static Bitmap Fit(Image image, int side)
        {
            var scale = Math.Min(1.0, (double)side / Math.Max(image.Width, image.Height));
            var width = Math.Max(1, (int)Math.Round(image.Width * scale));
            var height = Math.Max(1, (int)Math.Round(image.Height * scale));

            var fitted = new Bitmap(width, height, PixelFormat.Format24bppRgb);
            using (var g = System.Drawing.Graphics.FromImage(fitted))
            {
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.DrawImage(image, 0, 0, width, height);
            }
            return fitted;
        }
    }
}
