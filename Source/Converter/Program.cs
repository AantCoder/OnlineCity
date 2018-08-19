using OCServer.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;

namespace Converter
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args[0] == "0")
            {
                var workPort = args == null || args.Length < 2 ? 0 : int.Parse(args[1]);
                var workPath = @"C:\World" + (workPort == 0 ? "" : "\\" + workPort.ToString());
                
                var SaveFileName = Path.Combine(workPath, "World.dat");
                
                if (!File.Exists(SaveFileName)) 
                {
                    return;
                }
                using (var fs = File.OpenRead(SaveFileName))
                {
                    var bf = new BinaryFormatter();
                    var Data = (BaseContainer)bf.Deserialize(fs);

                }


                }
            else if (args[0] == "1")
            {
            }
        }
    }
}
