using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Point = System.Drawing.Point;

namespace Sidekick.Sidekick.Model
{
    public class OCVImage : IDisposable
    {
        private Bitmap _Image = null;
        private Mat _Mat = null;
        public int Width => _Mat?.Width ?? _Image.Width;
        public int Height => _Mat?.Height ?? _Image.Height;


        public Bitmap Image
        {
            get
            {
                if (_Image != null) return _Image;
                if (_Mat == null) return null;

                return _Image = MatToBitmap(_Mat);
            }
            set { _Image = value; }
        }

        public Mat Mat
        {
            get
            {
                if (_Mat != null) return _Mat;
                if (_Image == null) return null;

                return _Mat = BitmapToMat(_Image);
            }
            set { _Mat = value; }
        }

        public OCVImage SubRect(Rectangle rect)
        {
            return new OCVImage()
            {
                Mat = Mat.SubMat(new OpenCvSharp.Rect(rect.X, rect.Y, rect.Width, rect.Height))
            };
        }

        public Point Find(OCVImage image, double quality = 0.96, bool center = true)
            => Find(image, out _, quality, center);

        public Point Find(OCVImage image, out double resultQuality, double quality = 0.96, bool center = true)
        {
            resultQuality = 0;
            var result = this.Mat.MatchTemplate(image.Mat, TemplateMatchModes.CCoeffNormed);
            double minValues, maxValues;
            OpenCvSharp.Point minLocations, maxLocations;
            result.MinMaxLoc(out minValues, out maxValues, out minLocations, out maxLocations);
            if (maxValues >= quality)
            {
                resultQuality = maxValues;
                return center ? new Point(maxLocations.X, maxLocations.Y).Center(image) : new Point(maxLocations.X, maxLocations.Y);
            }
            return PointExt.Null;
        }


        public static OCVImage CreateFromFile(string fileName)
        {
            if (!File.Exists(fileName)) throw new FileNotFoundException("Нет файла " + fileName);
            return new OCVImage() { Mat = new Mat(fileName) };
        }

        public static Bitmap MatToBitmap(Mat image)
        {
            //OpenCvSharp.Extensions.BitmapConverter.ToBitmap(image); не работает т_т

            var data = image.ToBytes();
            using (var stream = new MemoryStream(data))
            {
                stream.Seek(0, SeekOrigin.Begin);
                return new Bitmap(stream);
            }
        }

        public static Mat BitmapToMat(Bitmap image, ImreadModes imreadModes = ImreadModes.Color)
        {
            //Mat mat = OpenCvSharp.Extensions.BitmapConverter.ToMat(bitmap); не работает т_т

            using (var stream = new MemoryStream())
            {
                image.Save(stream, ImageFormat.Bmp);
                stream.Seek(0, SeekOrigin.Begin);
                var imageData = stream.ToArray();
                Mat mat = Mat.FromImageData(imageData, imreadModes);
                //Mat mat = Cv2.ImDecode(imageData, imreadModes);
                return mat;
            }
        }

        public void Dispose()
        {
            if (_Image != null) _Image.Dispose();
            if (_Mat != null) _Mat.Dispose();
        }
    }

    public static class PointExt
    {
        public static Point Null => new Point(int.MinValue, int.MinValue);

        public static bool IsNull(this Point point)
            => point.X == int.MinValue && point.Y == int.MinValue;

        public static Point Center(this Point point, OCVImage image)
            => new Point(point.X + (image.Width >> 1), point.Y + (image.Height >> 1));

        public static Point Add(this Point point, Point add)
            => new Point(point.X + add.X, point.Y + add.Y);

        public static Point Add(this Point point, int x, int y)
            => new Point(point.X + x, point.Y + y);


    }
}
