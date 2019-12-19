using ServerOnlineCity;
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

            var serverManader = new ServerManager();
            serverManader.Start(workPath);
        }
    }
}
