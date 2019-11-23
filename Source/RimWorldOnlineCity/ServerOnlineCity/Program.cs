using OCServer;
using OCUnion;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Util;

namespace ServerOnlineCity
{
    class Program
    {
        static void Main(string[] args)
        {
            var workPort = args == null || args.Length < 1 ? 0 : int.Parse(args[0]);
            var defaultPath = Path.GetDirectoryName(Environment.GetCommandLineArgs()[0]);
            var workPath = /*@"C:\World"*/ Path.Combine(defaultPath, "World" + (workPort == 0 ? "" : "\\" + workPort.ToString()));

            Directory.CreateDirectory(workPath);
            //File.Delete(workPath + @"\Log.txt");

            Loger.PathLog = workPath;
            Loger.IsServer = true;
            var serverManader = new ServerManager();
            serverManader.LogMessage += (msg) => Console.WriteLine(msg);
            Loger.Log("Server Console Start!");
            if (workPort == 0)
                serverManader.Start(workPath);
            else
                serverManader.Start(workPath, workPort);
        }
    }
}
