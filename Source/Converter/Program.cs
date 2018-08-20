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

            if (args[0] == "0")
            {                
                if (!File.Exists(SaveFileName)) 
                {
                    return;
                }
                File.Copy(SaveFileName, SaveFileNameOld, true);
                using (var fs = File.OpenRead(SaveFileName))
                {
                    var bf = new BinaryFormatter();
                    var Data = (BaseContainer)bf.Deserialize(fs);

                    XmlSerializer formatter = new XmlSerializer(typeof(BaseContainer));

                    using (FileStream fsc = new FileStream("convert.xml", FileMode.OpenOrCreate))
                    {
                        formatter.Serialize(fsc, Data);
                    }
                }
            }
            else if (args[0] == "1")
            {
                XmlSerializer formatter = new XmlSerializer(typeof(BaseContainer));
                using (FileStream fsc = new FileStream("convert.xml", FileMode.OpenOrCreate))
                {
                    var Data = (BaseContainer)formatter.Deserialize(fsc);

                    //объектные ссылки только в List<ModelMailTrade> Mails поля From и To
                    foreach (var player in Data.PlayersAll)
                    {
                        foreach (var mail in player.Mails)
                        {
                            if (mail.From != null)
                            {
                                var pl = Data.PlayersAll.FirstOrDefault(p => p.Public.Login == mail.From.Login);
                                mail.From = pl == null ? null : pl.Public;
                            }
                            if (mail.To != null)
                            {
                                var pl = Data.PlayersAll.FirstOrDefault(p => p.Public.Login == mail.To.Login);
                                mail.To = pl == null ? null : pl.Public;
                            }
                        }
                    }

                    using (var fs = File.OpenWrite(SaveFileName))
                    {
                        var bf = new BinaryFormatter();
                        bf.Serialize(fs, Data);
                    }
                }
            }
        }
    }
}
