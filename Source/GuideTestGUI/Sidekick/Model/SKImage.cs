using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace Sidekick.Sidekick.Model
{
    public enum SKImageColorMode
    {
        Bit1Color6 = 1,
        Bit2Color12 = 2,
        Bit3Color24 = 3,
        Bit4Color48 = 4,
        Bit5Color96 = 5
    }

    public class SKImage
    {
        private const int HashFindMaxCount = 5;

        /// <summary>
        /// Изображение, после закгрузки не должно изменяться
        /// </summary>
        public Bitmap Image;

        /// <summary>
        /// Изображение с уменьшенным цветовом формате в виде массива.
        /// [высота, ширина] 2 байта цвета (таким образом цвет сохраняется до 5 бит на канал, 96 цветов)
        /// 0xFFFF - это цвет прозрачности
        /// </summary>
        private ushort[,] ImageMap;

        /// <summary>
        /// Маска прозрачности изображения.
        /// [высота, ширина] 0xFFFF - это цвет прозрачности, 0 - непрозрачно
        /// </summary>
        private ushort[,] ImageMapMask;

        /// <summary>
        /// Изображение получаемое из ImageMap обратным преобразованием, заполняется при необходимости
        /// </summary>
        private Bitmap ImageInColorMode = null;

        /// <summary>
        /// Тип хранимых цветов для операций сравнения картинок. 
        /// Технически кол-во битов смещения цвета
        /// </summary>
        private byte ColorMode;

        /// <summary>
        /// Цветокорекция. Добавляемый свет, от 0 до 255
        /// </summary>
        public byte Brightness = 0;

        /// <summary>
        /// Цвет в исходном изображении принимаемый за прозарчность.
        /// </summary>
        private Color? Transparency;

        private class HashPoint
        {
            public SKPos Pos;
            public long Hash;
        }

        /// <summary>
        /// 4 пикселя рядом в виде long, для быстрого поиска.
        /// Вненшний List строго из 4 элиментов - различные варианты смещения.
        /// Следующий List - такие хеши, в которых 4 пикселя не одинакового цвета, от 1 до HashFindMaxCount.
        /// Если невозможно найти подходящие пиксели HashFind.Count = 0
        /// </summary>
        private List<List<HashPoint>> HashFind
        {
            get
            {
                if (m_HashFind == null) m_HashFind = MapToHash();
                return m_HashFind;
            }
        }
        private List<List<HashPoint>> m_HashFind;

#if DEBUG
        public int StatCount1;
        public int StatCount2;
        public int StatCount3;
        public int StatCount4;
#endif

        private Size m_Size;
        public Size Size
        {
            get { return m_Size; }
        }

        private void SetImage(Bitmap bitmap, SKImageColorMode colorMode, Color? transparency = null)
        { 
            //единственное место установки Image
            Image = bitmap;

            ColorMode = (byte)colorMode;
            Transparency = transparency;
            m_HashFind = null;
            ImageInColorMode = null;
            m_Size = Image.Size;
        }

        public void LoadFromBitmap(Bitmap bitmap, SKImageColorMode colorMode, Color? transparency = null)
        {
            SetImage(bitmap, colorMode, transparency);
            BitmapToMap();
        }

        public void LoadFromSKImage(SKImage image, SKImageColorMode colorMode, Color? transparency = null)
        {
            LoadFromBitmap(image.Image, colorMode, transparency);
        }

        public void LoadFromFile(string fileName, SKImageColorMode colorMode, Color? transparency = null)
        {
            //Источник: https://habrahabr.ru/post/196578/
            Bitmap bitmap;
            using (FileStream fs = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read))
                bitmap = new Bitmap(fs);

            LoadFromBitmap(bitmap, colorMode, transparency);
        }

        public void LoadFromScreen(IntPtr windowHandle, SKImageColorMode colorMode, Color? transparency = null, Rectangle? rectangle = null)
        {
            var img = CaptureWindow.GetCaptureWindow(windowHandle);
            Bitmap bitmap;

            if (rectangle == null)
                bitmap = new Bitmap(img);
            else
            {
                bitmap = new Bitmap(rectangle.Value.Width, rectangle.Value.Height);
                using (Graphics gr = Graphics.FromImage(bitmap))
                {
                    gr.DrawImage(img, 0, 0, rectangle.Value, GraphicsUnit.Pixel);
                }
            }

            LoadFromBitmap(bitmap, colorMode, transparency);
        }

        public void SaveToFile(string fileName, ImageFormat format = null)
        {
            Image.Save(fileName, format ?? ImageFormat.Png);
        }
        
        public void SaveInColorModeToFile(string fileName, ImageFormat format = null)
        {
            GetImageInColorMode().Save(fileName, format ?? ImageFormat.Png);
        }

        public Bitmap GetImageInColorMode()
        { 
            if (ImageInColorMode == null)
            {
                ImageInColorMode = MapToBitmap(ImageMap);
            }
            return ImageInColorMode;
        }

        /// <summary>
        /// Возвращает копию, например, для применения фильтров
        /// </summary>
        public unsafe SKImage GetCopy(bool bitmapCopy = false)
        {
            var res = new SKImage();

            var img = bitmapCopy ? GetBitmapCopyRect() : Image;
            res.SetImage(img, (SKImageColorMode)ColorMode, Transparency);
            res.m_HashFind = m_HashFind;
            res.ImageInColorMode = ImageInColorMode;
            res.m_Size = Image.Size;

            //вместо повторного BitmapToMap вызова копируем 
            res.ImageMap = new ushort[ImageMap.GetLength(0), ImageMap.GetLength(1)];
            res.ImageMapMask = new ushort[ImageMapMask.GetLength(0), ImageMapMask.GetLength(1)];
            fixed (ushort* _ImageMap = ImageMap, _ImageMapMask = ImageMapMask
                , _ResImageMap = res.ImageMap, _ResImageMapMask = res.ImageMapMask)
            {
                int len = ImageMap.Length * 2;
                byte[] buf = new byte[len];
                Marshal.Copy(new IntPtr(_ImageMap), buf, 0, len);
                Marshal.Copy(buf, 0, new IntPtr(_ResImageMap), len);

                Marshal.Copy(new IntPtr(_ImageMapMask), buf, 0, len);
                Marshal.Copy(buf, 0, new IntPtr(_ResImageMapMask), len);
            }

            return res;
        }

        public unsafe void FilterMonochromeByColor(Color color, bool negative)
        {
            ImageInColorMode = null;

            int width = Image.Width,
                height = Image.Height;
            int colorModeRev = 8 - ColorMode;
            ushort colorMask = (ushort)((1 << ColorMode) - 1);
            ushort white = (ushort)((1 << (ColorMode * 3)) - 1);
            ushort findColor = (ushort)((color.R >> colorModeRev)
                + ((color.G >> colorModeRev) << ColorMode)
                + ((color.B >> colorModeRev) << (ColorMode * 2)));

            fixed (ushort* _res = ImageMap)
            {
                ushort* __res = _res;
                for (int h = 0; h < height; h++)
                {
                    for (int w = 0; w < width; w++)
                    {
                        ushort point = *__res;
                        /*byte b = (byte)((point & colorMask) * 255 / colorMask); point >>= ColorMode;
                        byte g = (byte)((point & colorMask) * 255 / colorMask); point >>= ColorMode;
                        byte r = (byte)((point & colorMask) * 255 / colorMask);
                        */
                        if (findColor == point)
                        {
                            *__res = negative ? (ushort)0 : white;
                        }
                        else
                        {
                            *__res = negative ? white : (ushort)0;
                        }

                        /*
                        *__res = (ushort)((r >> colorModeRev)
                            + ((g >> colorModeRev) << ColorMode)
                            + ((b >> colorModeRev) << (ColorMode * 2))
                            );
                        */
                        __res += 1;
                    }
                }
            }
        }

        public unsafe void FilterMonochromeByLevel(byte level, bool negative)
        {
            ImageInColorMode = null;

            int width = Image.Width,
                height = Image.Height;
            int colorModeRev = 8 - ColorMode;
            ushort colorMask = (ushort)((1 << ColorMode) - 1);
            ushort white = (ushort)((1 << (ColorMode * 3)) - 1);
            int level3 = (int)level * 3;

            fixed (ushort* _res = ImageMap)
            {
                ushort* __res = _res;
                for (int h = 0; h < height; h++)
                {
                    for (int w = 0; w < width; w++)
                    {
                        ushort point = *__res;
                        byte b = (byte)((point & colorMask) * 255 / colorMask); point >>= ColorMode;
                        byte g = (byte)((point & colorMask) * 255 / colorMask); point >>= ColorMode;
                        byte r = (byte)((point & colorMask) * 255 / colorMask);

                        if ((int)b + (int)g + (int)r > level3)
                        {
                            *__res = negative ? (ushort)0 : white;
                        }
                        else
                        {
                            *__res = negative ? white : (ushort)0;
                        }

                        /*
                        *__res = (ushort)((r >> colorModeRev)
                            + ((g >> colorModeRev) << ColorMode)
                            + ((b >> colorModeRev) << (ColorMode * 2))
                            );
                        */
                        __res += 1;
                    }
                }
            }
        }

        #region Поиск вхождения изображения

        private unsafe void BitmapToMap()
        {
            //Источник: https://habrahabr.ru/post/196578/
            int width = Image.Width,
                height = Image.Height;
            ushort[,] res = new ushort[height, width];
            ushort[,] mask = new ushort[height, width];
            
            BitmapData bd = Image.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.ReadOnly,
                PixelFormat.Format24bppRgb);

            bool useTrans = Transparency != null;
            byte tr = 0, tg = 0, tb = 0;
            if (useTrans)
            {
                tr = Transparency.Value.R;
                tg = Transparency.Value.G;
                tb = Transparency.Value.B;
            }
            try
            {
                byte* curpos;
                int colorModeRev = 8 - ColorMode;
                ushort white = (ushort)((1 << (ColorMode * 3)) - 1);
                if (Brightness == 0)
                {
                    fixed (ushort* _res = res, _mask = mask)
                    {
                        ushort* __res = _res;
                        ushort* __mask = _mask;
                        for (int h = 0; h < height; h++)
                        {
                            curpos = ((byte*)bd.Scan0) + h * bd.Stride;
                            for (int w = 0; w < width; w++)
                            {
                                byte b = *(curpos++);
                                byte g = *(curpos++);
                                byte r = *(curpos++);
                                if (useTrans && tr == r && tg == g && tb == b)
                                {
                                    *__res = white;
                                    *__mask = white;
                                }
                                else
                                {
                                    *__res = (ushort)((r >> colorModeRev)
                                        + ((g >> colorModeRev) << ColorMode)
                                        + ((b >> colorModeRev) << (ColorMode * 2))
                                     );
                                    *__mask = 0;
                                }
                                __res += 1;
                                __mask += 1;
                            }
                        }
                    }
                }
                else //Ниже копия с добавлением преобразования
                {
                    ushort revertBrightness = (ushort)(255 - Brightness);
                    fixed (ushort* _res = res, _mask = mask)
                    {
                        ushort* __res = _res;
                        ushort* __mask = _mask;
                        for (int h = 0; h < height; h++)
                        {
                            curpos = ((byte*)bd.Scan0) + h * bd.Stride;
                            for (int w = 0; w < width; w++)
                            {
                                byte b = (byte)(*(curpos++) * revertBrightness / 255 + Brightness);
                                byte g = (byte)(*(curpos++) * revertBrightness / 255 + Brightness);
                                byte r = (byte)(*(curpos++) * revertBrightness / 255 + Brightness);
                                if (useTrans && tr == r && tg == g && tb == b)
                                {
                                    *__res = white;
                                    *__mask = white;
                                }
                                else
                                {
                                    *__res = (ushort)((r >> colorModeRev)
                                        + ((g >> colorModeRev) << ColorMode)
                                        + ((b >> colorModeRev) << (ColorMode * 2))
                                     );
                                    *__mask = 0;
                                }
                                __res += 1;
                                __mask += 1;
                            }
                        }
                    }
                }
            }
            finally
            {
                Image.UnlockBits(bd);
            }
            ImageMap = res;
            ImageMapMask = mask;
        }

        private unsafe Bitmap MapToBitmap(ushort[,] map)
        {
            int width = map.GetLength(1),
                height = map.GetLength(0);

            Bitmap bitmap = new Bitmap(width, height, PixelFormat.Format24bppRgb);

            BitmapData bd = bitmap.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.WriteOnly,
                PixelFormat.Format24bppRgb);

            try
            {
                byte* curpos;
                ushort colorMask = (ushort)((1 << ColorMode) - 1);
                fixed (ushort* _res = map)
                {
                    ushort* __res = _res;
                    for (int h = 0; h < height; h++)
                    {
                        curpos = ((byte*)bd.Scan0) + h * bd.Stride;
                        for (int w = 0; w < width; w++)
                        {
                            ushort point = *(__res++);
                            byte b = (byte)((point & colorMask) * 255 / colorMask); point >>= ColorMode;
                            byte g = (byte)((point & colorMask) * 255 / colorMask); point >>= ColorMode;
                            byte r = (byte)((point & colorMask) * 255 / colorMask);
                            *(curpos++) = r;
                            *(curpos++) = g;
                            *(curpos++) = b; 
                        }
                    }
                }
            }
            finally
            {
                bitmap.UnlockBits(bd);
            }
            return bitmap;
        }

        private unsafe List<List<HashPoint>> MapToHash()
        {
            var res = new List<List<HashPoint>>();
            if (Image.Width < 7) return res; //недостаточно ширины для всех смещений блока из 4 пикселей

            int thisHeight = Image.Height;
            int thisWidth = Image.Width;

            fixed (ushort* im = ImageMap)
            {
                for (int offset = 0; offset < 4; offset++)
                {
                    var resof = new List<HashPoint>();
                    
                    //получаем хеш цветов 4 пикселей в смещении offset
                    for (int ty = 0; ty < thisHeight; ty++)
                    {
                        //первый пиксель четверки
                        ushort* imx = im + offset + thisWidth * ty;
                        //номер этого пикселя в строке
                        int tx = offset;
                        //хеш первых 4 пикселей
                        long* iml = (long*)imx;
                        //цикл по всем четверкам строки (некратный конец игнорируется, по нему поиск хешем не выполняется)
                        for (int i = 0; i < (thisWidth - offset) / 4; i++, imx += 4, tx += 4, iml++)
                        { 
                            //если хотя бы 1 пиксель отличается
                            if (*imx != *(imx + 1)
                                || *(imx + 1) != *(imx + 2)
                                || *(imx + 2) != *(imx + 3)
                                )
                            {
                                resof.Add(new HashPoint() { Pos = new SKPos(tx, ty), Hash = *iml });
                                if (resof.Count >= HashFindMaxCount) break;
                            }

                        }
                        if (resof.Count >= HashFindMaxCount) break;
                    }

                    if (resof.Count == 0) return new List<List<HashPoint>>();
                    res.Add(resof);
                }
            }

            return res;
        }

        /// <summary>
        /// Поиск вхождения изображения.
        /// Для поиска больших по размеру вхождений, лучше не ограничивать область поиска rectangle
        /// </summary>
        /// <param name="img">Образец для поиска</param>
        /// <param name="rectangle">Ограничение области поиска</param>
        /// <returns>Список координат вхождений</returns>
        public unsafe List<SKPos> FindImage(SKImage img, Rectangle? rectangle = null)
        {
#if DEBUG
            StatCount1 = 0;
            StatCount2 = 0;
            StatCount3 = 0;
            StatCount4 = 0;
#endif
            int height = img.Image.Height;
            int width = img.Image.Width;
            int thisHeight = Image.Height;
            int thisWidth = Image.Width;

            if (rectangle != null || img.Transparency != null || height * width < 500 || thisWidth < 12 || thisHeight * thisWidth < 500
                || img.HashFind.Count == 0) return FindImageWithoutHash(img, rectangle);

#if DEBUG
            StatCount1++;
#endif
            //поиск с хешем заключается в поиске первого хеша (таких 4 штуки для всех смещений четверок пикселей)
            //для каждого совпадения первого хеша проверяються остальные (их максимум обычно штук 5)
            //если все совпали то переходим к полному сравнению изображения в данном месте

            var res = new List<SKPos>();

            if (thisWidth < width
                || thisHeight < height)
                return res;

            fixed (ushort* p1 = ImageMap, p2 = img.ImageMap)
            {
                //ищим совпадения хотя бы с одним из первых хешей в словарях разных смещений
                for (int ty = 0; ty < thisHeight; ty++)
                {
                    //первый пиксель четверки
                    ushort* timx = p1 + thisWidth * ty;
                    //номер этого пикселя в строке
                    int tx = 0;
                    //хеш первых 4 пикселей
                    long* timl = (long*)timx;
                    //цикл по всем четверкам строки (некратный конец будет отдельно проверен в конце)
                    for (int i = 0; i <= thisWidth / 4; i++, timx += 4, tx += 4, timl++)
                    {
                        if (i == thisWidth / 4)
                            if (thisWidth % 4 == 0) break;
                            else
                            {
                                //дополнительная итерация в конце: проверяем одну четверку пикселей с правого края
                                tx = thisWidth - 4;
                                timx = p1 + thisWidth * ty + tx;
                                timl = (long*)timx;
                            }
                        List<HashPoint> findOffsetHash = null; //признак совпадения с первым хешем любого смещения
                        for (int hashi = 0; hashi < 4; hashi++)
                            if (img.HashFind[hashi][0].Hash == *timl)
                            {
                                findOffsetHash = img.HashFind[hashi];
                                break;
                            }
                        if (findOffsetHash != null)
                        {
                            //первый хеш найден - проверяем остальные
#if DEBUG
                            StatCount2++;
#endif
                            bool check = true;
                            //координаты начала предпологаемого результата по найденному
                            int fx = tx - findOffsetHash[0].Pos.X;
                            int fy = ty - findOffsetHash[0].Pos.Y;

                            for(int ih = 1; ih < findOffsetHash.Count; ih++)
                            {
                                int fxh = fx + findOffsetHash[ih].Pos.X;
                                int fyh = fy + findOffsetHash[ih].Pos.Y;

                                if (fxh + 3 >= thisWidth
                                    || fyh >= thisHeight
                                    || findOffsetHash[ih].Hash != *((long*)(p1 + thisWidth * fyh + fxh)))
                                {
                                    check = false;
                                    break;
                                }
                            }
                            
                            if (check)
                            {
                                //все хеши подтверждены - производим полное сравнение
#if DEBUG
                                StatCount3++;
#endif

                                bool next = false;

                                for (int y = 0; y < height && !next; y++)
                                {
                                    ushort* x1 = p1 + (y + fy) * thisWidth + fx
                                        , x2 = p2 + y * width;
                                    int l = width;
                                    for (int ii = 0; ii < l / 4 && !next; ii++, x1 += 4, x2 += 4)
                                        if (*((long*)x1) != *((long*)x2))
                                        {
                                            next = true;
                                            break;
                                        }
                                    if ((l & 2) != 0 && !next)
                                    {
                                        if (*((int*)x1) != *((int*)x2))
                                        {
                                            next = true;
                                        }
                                        else
                                        {
                                            x1 += 2;
                                            x2 += 2;
                                        }
                                    }
                                    if ((l & 1) != 0 && !next)
                                        if (*((short*)x1) != *((short*)x2))
                                            next = true;
                                }

                                if (!next)
                                {
#if DEBUG
                                    StatCount4++;
#endif
                                    res.Add(new SKPos(fx, fy));
                                }

                                //
                            }
                        }
                    }
                }
            }

            return res;

        }

        public unsafe int FindColor(Rectangle rectangle, Color color)
        {
            int x1 = rectangle.Left, y1 = rectangle.Top, x2 = rectangle.Right - 1, y2 = rectangle.Bottom - 1;

            int res = 0;
            int thisHeight = Image.Height;
            int thisWidth = Image.Width;
            if (x2 >= thisWidth) x2 = thisWidth - 1;
            if (y2 >= thisHeight) y2 = thisHeight - 1;
            if (x1 < 0) x1 = 0;
            if (y1 < 0) y1 = 0;
            if (x1 > x2 || y1 > y2 || x1 > thisWidth || y1 > thisHeight || x2 < 0 || y2 < 0) return 0;

            int colorModeRev = 8 - ColorMode;
            ushort findColor =  (ushort)((color.R >> colorModeRev)
                + ((color.G >> colorModeRev) << ColorMode)
                + ((color.B >> colorModeRev) << (ColorMode * 2)));

            fixed (ushort* p1 = ImageMap)
            {
                for (int ty = y1; ty <= y2; ty++)
                {
                    for (int tx = x1; tx <= x2; tx++)
                    {
                        ushort* point = p1 + ty * thisWidth + tx;
                        if (*((ushort*)point) == findColor) res++;
                    }
                }
            }

            return res;
        }

        private unsafe List<SKPos> FindImageWithoutHash(SKImage img, Rectangle? rectangle = null)
        {
            var res = new List<SKPos>();

            int height = img.Image.Height;
            int width = img.Image.Width;
            int thisHeight = Image.Height;
            int thisWidth = Image.Width;

            int xmin = 0, ymin = 0, xmax = thisWidth - 1, ymax = thisHeight - 1; 
            if (rectangle != null)
            {
                xmin = rectangle.Value.Left;
                ymin = rectangle.Value.Top;
                xmax = rectangle.Value.Right - 1;
                ymax = rectangle.Value.Bottom - 1;

                if (xmax >= thisWidth) xmax = thisWidth - 1;
                if (ymax >= thisHeight) ymax = thisHeight - 1;
                if (xmin < 0) xmax = 0;
                if (ymin < 0) ymin = 0;
                if (xmin > xmax || ymin > ymax || xmin > thisWidth || ymin > thisHeight || xmax < 0 || ymax < 0) return res;
            }

            if (thisWidth < width
                || thisHeight < height)
                return res;

            bool useTrans = img.Transparency != null;

            fixed (ushort* p1 = ImageMap, p2 = img.ImageMap, p3 = img.ImageMapMask)
            {
                for (int ty = ymin; ty <= ymax + 1 - height; ty++)
                {
                    for (int tx = xmin; tx <= xmax + 1 - width; tx++)
                    {
                        bool next = false;

                        for (int y = 0; y < height && !next; y++)
                        {
                            ushort* x1 = p1 + (y + ty) * thisWidth + tx
                                , x2 = p2 + y * width
                                , x3 = p3 + y * width;
                            int l = width;
                            if (!useTrans)
                            {
                                for (int i = 0; i < l / 4 && !next; i++, x1 += 4, x2 += 4)
                                    if (*((ulong*)x1) != *((ulong*)x2))
                                    {
                                        next = true;
                                        break;
                                    }
                            }
                            else
                            {
                                for (int i = 0; i < l / 4 && !next; i++, x1 += 4, x2 += 4, x3 += 4)
                                {
                                    var point = *((ulong*)x1) | *((ulong*)x3);
                                    if (*((ulong*)x2) != point)
                                    {
                                        next = true;
                                        break;
                                    }
                                }
                            }
                            if ((l & 2) != 0 && !next)
                            {
                                var point = *((uint*)x1) | *((uint*)x3);
                                if (*((uint*)x2) != point)
                                {
                                    next = true;
                                }
                                else
                                {
                                    x1 += 2;
                                    x2 += 2;
                                    x3 += 2;
                                }
                            }
                            if ((l & 1) != 0 && !next)
                            {
                                var point = *((ushort*)x1) | *((ushort*)x3);
                                if (*((ushort*)x2) != point)
                                    next = true;
                            }
                        }

                        if (!next) res.Add(new SKPos(tx, ty));

                    }
                }
            }

            return res;
        }
        
        #endregion

        public static Bitmap BitmapCopyRect(Bitmap source, Rectangle rectangle)
        {    
            Bitmap image = new Bitmap(rectangle.Width, rectangle.Height);
            using (Graphics gr = Graphics.FromImage(image))
            {
                gr.DrawImage(source, 0, 0, rectangle, GraphicsUnit.Pixel);
            }
            return image;
        }

        public Bitmap GetBitmapCopyRect(Rectangle? rectangle = null)
        {
            if (rectangle == null) rectangle = new Rectangle(0, 0, Image.Width, Image.Height);
            return BitmapCopyRect(this.Image, rectangle.Value);
        }


        #region Устаревшее
        private List<SKPos> FindImageOld1(SKImage img)
        {
            var res = new List<SKPos>();
            for(int x = 0; x <= Image.Width - img.Image.Width; x++)
                for (int y = 0; y <= Image.Height - img.Image.Height; y++)
                {
                    if (EqualBitmap(img, x, y)) res.Add(new SKPos(x, y));
                }
            return res;
        }

        private unsafe bool EqualBitmap(SKImage img, int posx, int posy)
        {
            int height = img.Image.Height;
            int width = img.Image.Width;

            if (Image.Width < width + posx
                || Image.Height < height + posy)
                return false;

            int thisWidth = Image.Width;

            for (int h = 0; h < height; h++)
            {
                fixed (ushort* p1 = ImageMap, p2 = img.ImageMap)
                {
                    ushort* x1 = p1 + (h + posy) * thisWidth + posx
                        , x2 = p2 + h * width;
                    int l = width;
                    for (int i = 0; i < l / 4; i++, x1 += 4, x2 += 4)
                        if (*((long*)x1) != *((long*)x2))
                            return false;
                    if ((l & 2) != 0)
                    {
                        if (*((int*)x1) != *((int*)x2)) return false;
                        x1 += 2;
                        x2 += 2;
                    }
                    if ((l & 1) != 0)
                        if (*((short*)x1) != *((short*)x2))
                            return false;
                }
            }
            return true;
        }

        #endregion

    }
}
