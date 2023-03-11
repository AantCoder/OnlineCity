using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;
using System.Diagnostics;

namespace Sidekick.Sidekick.Model
{
    public class SKText
    {
        public static string CacheFileName = "CacheSK.dat";
        //private tessnet2.Tesseract OCR = null;
        //private IsolatedRW OCRRemote = null;
        private static MMFRW OCRRemote = null;
        private bool NumbersMode = false;
        private int Timeout;

        public SKText(bool numbersMode = false, int timeout = -1)
        {
            NumbersMode = numbersMode;
            if (timeout == -1) timeout = 5;
            Timeout = timeout;
        }

        public static string ReplaceBadFind(string str)
        {
            str = str.Trim().ToLower()
                .Replace("\t", " ").Replace("\r", " ").Replace("\n", " ")
                .Replace("    ", " ").Replace("   ", " ").Replace("  ", " ");
            if (str.Length < 3) return str;
            str = str.Remove(str.Length - 1).Substring(1);
            return str
                .Replace("•", "")
                .Replace("2", "e")
                .Replace("i", "l")
                .Replace("1", "l")
                .Replace("0", "o")
                .Replace("y", "u")
                .Replace("u", "o")
                .Replace("j", "v")
                .Replace("q", "g")
                .Replace("r", "p") //из-за больших букв
                .Replace("b", "o") //жесть какая-то
                .Replace("c", "o")
                .Replace("¤", "d") //из отладки, не понятно почему так
                ;
        }
        public static string ReplaceBadNumber(string str)
        {
            str = str.Trim().ToLower()
                .Replace("\t", " ").Replace("\r", " ").Replace("\n", " ")
                .Replace("    ", " ").Replace("   ", " ").Replace("  ", " ");
            str = str
                .Replace("i", "1")
                .Replace("l", "1")
                .Replace("u", "0")
                .Replace("o", "0")
                .Replace("b", "0")
                .Replace("d", "0")
                .Replace(".", "")
                .Replace("*", "")
                .Replace("-", "")
                .Replace("`", "")
                .Replace(":", "")
                .Replace(" ", "")
                ;
            return str.Length == 0 || str[0] == '0' && str != "0" ? "unknow" : str;
        }

        public string ReadTest(SKImage source, Action<string> addLog, Rectangle? rectangle = null, bool small = false)
        {
            return ReadTest(source.Image, addLog, rectangle, small);
        }

        public string ReadTest(Bitmap source, Action<string> addLog, Rectangle? rectangle = null, bool small = false)
        {
            Bitmap image;
            if (rectangle != null
                && (rectangle.Value.Size != source.Size || rectangle.Value.X != 0 || rectangle.Value.Y != 0))
            {
                image = SKImage.BitmapCopyRect(source, rectangle.Value);
            }
            else
                image = source;

            string res = "";
            
            try
            {
                if (OCRRemote == null)
                {
                    OCRRemote = new MMFRW();
                    OCRRemote.LogSend = addLog;
                }
                OCRRemote.Timeout = Timeout;

                ImageConverter converter = new ImageConverter();
                var imageByte = (byte[])converter.ConvertTo(image, typeof(byte[]));
                var hash = GetHash(imageByte);
                if (Cache.ContainsKey(hash))
                    res = Cache[hash];
                else
                {
                    ATimer.Start();
                    res = OCRRemote.ReadTest(imageByte, NumbersMode, small);
                    ATimer.Stop(); 
                    Cache.Add(hash, res);
                }

            }
            catch
            {
            }
            return res;
        }

        private string GetHash(byte[] src)
        {
            SHA1 sha = new SHA1CryptoServiceProvider();
            return Convert.ToBase64String(sha.ComputeHash(src));
        }

        public void Test(string imageName)
        {
            /*
            Bitmap image = new Bitmap(imageName);
            image = PrepareBitmap(image);
            tessnet2.Tesseract ocr = new tessnet2.Tesseract();
            //ocr.SetVariable("tessedit_char_whitelist", "0123456789"); // If digit only
            ocr.SetVariable("tessedit_char_whitelist", "asdfghjklpoiuytrewqzxcvbnmABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789.,()");
            ocr.Init(@"tessdata", "eng", false); // To use correct tessdata
            List<tessnet2.Word> result = ocr.DoOCR(image, Rectangle.Empty);
            foreach (tessnet2.Word word in result)
                Console.WriteLine("{0} : {1}", word.Confidence, word.Text);
            */
        }

        #region Cache
        private readonly static Dictionary<string, string> Cache;

        public static long CacheLength
        {
            get { return Cache.Count; }
        }

        private static Stopwatch ATimer;

        public static long TimerAnalize
        {
            get { return ATimer.ElapsedMilliseconds; }
        }

        private class CacheDic
        {
            public List<string> Key;
            public List<string> Value;
        }

        static SKText()
        {
            ATimer = new Stopwatch();

            Cache = new Dictionary<string, string>();
            if (File.Exists(CacheFileName))
            { 
                FileStream fs = new FileStream(CacheFileName, FileMode.Open);
                try 
                {
                    BinaryFormatter formatter = new BinaryFormatter();

                    Cache = (Dictionary<string, string>)formatter.Deserialize(fs);
                    CacheLastSaveCount = Cache.Count;
                    /*
                    var cache = (CacheDic) formatter.Deserialize(fs);
                    if (cache.Key.Count == cache.Value.Count) 
                    {
                        var tCache = new Dictionary<string, string>();
                        for(int i =0; i < cache.Key.Count; i++)
                        {
                            tCache.Add(cache.Key[i], cache.Value[i]);
                        }
                        Cache = tCache;
                        CacheLastSaveCount = Cache.Count;
                    }
                    */
                }
                catch (Exception e) 
                {
                    Console.WriteLine("Failed to deserialize. Reason: " + e.Message);
                    Cache = new Dictionary<string, string>();
                }
                finally 
                {
                    fs.Close();
                }
            }

            var th = new Thread(() =>
                {
                    while (true)
                    {
#if DEBUG
                        Thread.Sleep(1 * 60000);
#else
                        Thread.Sleep(5 * 60000); //5 минут перерыв
#endif
                        CacheSave();
                    }
                });
            th.IsBackground = true;
            th.Start();
        }

        private static long CacheLastSaveCount = 0;
        public static void CacheSave()
        {
            lock (Cache)
            {
                if (CacheLastSaveCount == Cache.Count) return;

                FileStream fs = new FileStream(CacheFileName, FileMode.Create);
                BinaryFormatter formatter = new BinaryFormatter();
                try
                {
                    /*
                    var tCache = new CacheDic()
                    {
                        Key = new List<string>(),
                        Value = new List<string>()
                    };
                    foreach (var key in Cache.Keys)
                    {
                        tCache.Key.Add(key);
                        tCache.Value.Add(Cache[key]);
                    }*/
                    formatter.Serialize(fs, Cache);
                    CacheLastSaveCount = Cache.Count;
                }
                catch (Exception e)
                {
                    Console.WriteLine("Failed to serialize. Reason: " + e.Message);
                }
                finally
                {
                    fs.Close();
                }
            }
        }
        #endregion

    }
}
