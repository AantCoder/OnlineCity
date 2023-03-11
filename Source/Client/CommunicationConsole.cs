using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ClientAncillary
{
    public class CommunicationConsole
    {
        public void SendData(int code, byte[] data)
        {
            using (var client = new NamedPipeClientStream("OCClientAncillary" + code))
            {
                client.Connect();
                using (StreamWriter writer = new StreamWriter(client))
                {
                    writer.Write(Convert.ToBase64String(data));
                    writer.Flush();
                }
            }
        }

        public byte[] ReceiveData(int code, Action beforeWait)
        {
            using (var server = new NamedPipeServerStream("OCClientAncillary" + code, PipeDirection.InOut))
            {
                Thread thread = new Thread(() =>
                {
                    try
                    {
                        beforeWait();
                    }
                    catch { }
                });
                thread.Start();

                server.WaitForConnection();

                StreamReader reader = new StreamReader(server);
                byte[] data = Convert.FromBase64String(reader.ReadToEnd());

                thread.Join();
                return data;
            }
        }
    }
}
