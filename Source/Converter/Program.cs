using OCServer.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Converter
{
    class Program
    {
        static void Main(string[] args)
        {
            var workPort = args == null || args.Length < 2 ? 0 : int.Parse(args[1]);
            var workPath = @"C:\World" + (workPort == 0 ? "" : "\\" + workPort.ToString());

            var SaveFileName = Path.Combine(workPath, "World.dat");
            var SaveFileNameOld = Path.Combine(workPath, "WorldOld.dat");
    
            if (!File.Exists(SaveFileName)) 
            {
                return;
            }
            File.Copy(SaveFileName, SaveFileNameOld, true);

            BaseContainer Data;
            using (var fs = File.OpenRead(SaveFileName))
            {
                var bf = new BinaryFormatter();
                Data = (BaseContainer)bf.Deserialize(fs);
            }

            //тут будет код конвертации, если понадобиться


            using (var fs = File.OpenWrite(SaveFileName))
            {
                var bf = new BinaryFormatter();
                bf.Serialize(fs, Data);
            }
        }
    }
}
