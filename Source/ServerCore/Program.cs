using OCUnion;
using System;
using System.IO;
using System.Text;

namespace ServerOnlineCity
{
    class Program
    {
        static void Main(string[] args)
        {           
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance); // fix error encoding 1252 from encoding with netcore framework
            
            var defaultPath = args.Length > 0 ? args[0] : "World";
            var workPath = Path.Combine(Directory.GetCurrentDirectory(), defaultPath);
            Directory.CreateDirectory(workPath);

            Loger.LogMessage += (msg) => Console.WriteLine(msg);
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);

            var serverManager = new ServerManager();
            serverManager.Start(workPath);
        }

        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {            
            var date = DateTime.Now.ToString("yyyy-MM-dd-hh-mm");
            var fileName = Path.Combine(Directory.GetCurrentDirectory(), "!UnhandledException" + date + ".log");
            File.WriteAllText(fileName, e.ExceptionObject.ToString(), Encoding.UTF8);
        }
    }
}
