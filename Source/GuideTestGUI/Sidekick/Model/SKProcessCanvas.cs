using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Point = System.Drawing.Point;

namespace Sidekick.Sidekick.Model
{
    public class SKProcessCanvas : SKProcess, IDisposable
    {
        private ILoaderImage Loader;
        public OCVImage Screenshot { get; private set; }

        private Dictionary<Rectangle, OCVImage> ScreenshotRect = null;


        public SKProcessCanvas(IntPtr proc, ILoaderImage loader)
            : base(proc)
        {
            Loader = loader;
        }

        public void UpdateScreenshot(bool withActivate = false)
        {
            Bitmap bitmap = CaptureWindow.GetCaptureWindow(Proc);

            if (ScreenshotRect != null) foreach(var sr in ScreenshotRect.Values) sr.Dispose();
            if (Screenshot != null) Screenshot.Dispose();

            Screenshot = new OCVImage()
            {
                Image = bitmap
            };
            ScreenshotRect = null;
        }

        public Rectangle GetWindowsPos()
        {
            return CaptureWindow.GetWindowRect(Proc);
        }

        public void SetWindowsPos(Rectangle rect)
        {
            CaptureWindow.SetWindowRect(Proc, rect);
        }

        public OCVImage ScreenshotSubRect(Rectangle rect)
        {
            OCVImage result;
            if (ScreenshotRect == null)
            {
                result = Screenshot.SubRect(rect);
                ScreenshotRect = new Dictionary<Rectangle, OCVImage>() { { rect, result } };
                return result;
            }
            if (!ScreenshotRect.TryGetValue(rect, out result))
            {
                result = Screenshot.SubRect(rect);
                ScreenshotRect[rect] = result;
            }
            return result;
        }

        public OCVImage Image(string imageName)
        {
            return Loader.Get(imageName);
        }

        public List<OCVImage> ImageList(params string[] imageNames)
        {
            return imageNames.Select(im => Loader.Get(im)).ToList();
        }

        public Point FindWait(string image, int ms, double quality = 0.96, bool center = true, Rectangle? rect = null)
            => FindWait(Image(image), ms, quality, center, rect);

        public Point FindWait(OCVImage image, int millisecond, double quality = 0.96, bool center = true, Rectangle? rect = null)
            => FindWait(new List<OCVImage>(){ image }, millisecond, quality, center, rect);

        public Point FindWait(List<OCVImage> images, int millisecond, double quality = 0.96, bool center = true, Rectangle? rect = null)
        {
            var wait = DateTime.UtcNow;
            Point pos;
            do
            {
                UpdateScreenshot();
                for (int i = 0; i < images.Count; i++)
                {
                    pos = Find(images[i], quality, center, rect);//"ButtonOnlineCity"
                    if (!pos.IsNull()) return pos;
                }
                Thread.Sleep(100);
            }
            while ((DateTime.UtcNow - wait).TotalMilliseconds < millisecond);
            return PointExt.Null;
        }

        public Point Find(string image, double quality = 0.96, bool center = true, Rectangle? rect = null) 
            => Find(image, out _, quality, center, rect);

        public Point Find(string image, out double resultQuality, double quality = 0.96, bool center = true, Rectangle? rect = null) 
            => Find(Image(image), out resultQuality, quality, center, rect);

        public Point Find(OCVImage image, double quality = 0.96, bool center = true, Rectangle? rect = null) 
            => Find(image, out _, quality, center, rect);

        public Point Find(OCVImage image, out double resultQuality, double quality = 0.96, bool center = true, Rectangle? rect = null)
        {
            if (rect == null) return Screenshot.Find(image, out resultQuality, quality, center);
            var source = ScreenshotSubRect(rect.Value);
            var point = source.Find(image, out resultQuality, quality, center);
            return point.IsNull() ? point : new Point(point.X + rect.Value.X, point.Y + rect.Value.Y);
        }

        public void Test(string anyImage)
        {
            using (var src = new Mat(anyImage, ImreadModes.Grayscale))
            using (var dst = new Mat())
            {
                Cv2.Canny(src, dst, 50, 200);
                using (new Window("src image", src))
                using (new Window("dst image", dst))
                {
                    Cv2.WaitKey();
                }
            }
        }

        public void Dispose()
        {
            if (Screenshot != null) Screenshot.Dispose();
            if (Loader != null) Loader.Dispose();
        }
    }
}
