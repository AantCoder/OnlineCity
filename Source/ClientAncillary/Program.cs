using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ClientAncillary
{
    class Program
    {
        static void Main(string[] args)
        {
#if DEBUG
            Console.WriteLine("...");
#else
            if (args.Length < 1) return;
            if (!int.TryParse(args[0], out var code)) return;
#endif
            byte[] data = null;

            Thread staThread = new Thread(x =>
            {

                data = ClipboardRead();
            });
            staThread.SetApartmentState(ApartmentState.STA);
            staThread.Start();
            staThread.Join();

            if (data == null) data = new byte[0];
            var com = new CommunicationConsole();
#if DEBUG
            Console.WriteLine("SendData " + Encoding.ASCII.GetString(data).Length);


            var coms = new CommunicationConsole();
            var rd = coms.ReceiveData(5, () =>
            {
                Console.WriteLine("SendData N0");
                com.SendData(5, data);
                Console.WriteLine("SendData N1");
            });

            Console.WriteLine("ReceiveData " + rd.Length);

            Console.ReadKey();
#else
            com.SendData(code, data);
#endif
        }

        private static byte[] ClipboardRead()
        {
            byte[] data = null;
            try
            {
                //Читаем из буфера имя файла
                var list = Clipboard.ContainsFileDropList() ? Clipboard.GetFileDropList() : null;
                if (list != null && list.Count > 0)
                {
                    data = File.ReadAllBytes(list[0].Replace("\\", "" + Path.DirectorySeparatorChar));
#if DEBUG
                    Console.WriteLine("file " + list[0]);
#endif
                }

                //читаем из буфера картинку
                if (data == null && Clipboard.ContainsImage())
                {
                    var image = Clipboard.GetImage();
                    using (MemoryStream ms = new MemoryStream())
                    {
                        image.Save(ms, ImageFormat.Png);
                        data = ms.ToArray();
                    }
#if DEBUG
                    Console.WriteLine("image " + data.Length);
#endif
                }

                //читаем из буфера текст
                if (data == null && Clipboard.ContainsText())
                {
                    var txt = Clipboard.GetText();
                    data = Encoding.UTF8.GetBytes(txt);
#if DEBUG
                    Console.WriteLine("txt " + data.Length);
#endif
                }
            }
            catch (Exception e)
            {
#if DEBUG
                Console.WriteLine(e.ToString());
#endif
                data = null;
            }
            return data;
        }
    }
}
