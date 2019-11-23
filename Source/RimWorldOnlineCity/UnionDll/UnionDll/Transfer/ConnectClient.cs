using OCUnion;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace Transfer
{
    public class ConnectClient : IDisposable
    {
        public TcpClient Client;
        protected NetworkStream ClientStream;
        public readonly Encoding MessageEncoding = Encoding.UTF8;
        protected const int DefaultTimeout = 600000; //10 мин
        public DateTime LastSend;

        public ConnectClient(string addr, int port)
            : this(new TcpClient(addr, port))
        { }
        
        public ConnectClient(TcpClient client)
        {
            Client = client;
            Client.SendTimeout = DefaultTimeout;
            Client.ReceiveTimeout = DefaultTimeout;
            ClientStream = Client.GetStream();
            LastSend = DateTime.UtcNow;
        }

        public void Dispose()
        {
            ClientStream.Close();
            Client.Close();
        }

        public void SendMessage(byte[] message)
        {
            byte[] packlength = BitConverter.GetBytes(message.Length);
            ClientStream.Write(packlength, 0, packlength.Length);
            ClientStream.Write(message, 0, message.Length);
            LastSend = DateTime.UtcNow;
        }

        public byte[] ReceiveBytes()
        {
            //кол-во байт в начале в которых передается длинна сообщения
            int Int32Length = 4;
            //длина передаваемого сообщения (принимается в первых 4 байтах (константа Int32Length))
            int lenghtAllMessageByte;

            byte[] receiveBuffer = ReceiveBytes(Int32Length);
            lenghtAllMessageByte = BitConverter.ToInt32(receiveBuffer, 0);
            if (lenghtAllMessageByte == 0) return new byte[0];

            receiveBuffer = ReceiveBytes(lenghtAllMessageByte);

            return receiveBuffer;
        }

        private byte[] ReceiveBytes(int countByte)
        {
            //if (!Loger.IsServer) Loger.Log("Client ReceiveBytes " + countByte.ToString() + ", " + Client.ReceiveBufferSize);
            //результат
            byte[] msg = new byte[countByte];
            //сколько уже считано
            int offset = 0;
            //буфер результата
            byte[] receiveBuffer = new byte[Client.ReceiveBufferSize];
            //кол-во считано байт последний раз
            int numberOfBytesRead;
            //длина передаваемого сообщения (принимается в первых 4 байтах (константа Int32Length))
            int lenghtAllMessageByte = countByte;

            while (lenghtAllMessageByte > 0)
            {
                int maxCountRead = receiveBuffer.Length;
                if (maxCountRead > lenghtAllMessageByte) maxCountRead = lenghtAllMessageByte;

                numberOfBytesRead = ClientStream.Read(receiveBuffer, 0, maxCountRead);
                if (numberOfBytesRead == 0)
                {
                    Thread.Sleep(1);
                }
                else
                {
                    Buffer.BlockCopy(receiveBuffer, 0, msg, offset, numberOfBytesRead);
                    offset += numberOfBytesRead;
                    lenghtAllMessageByte -= numberOfBytesRead;
                }
            };

            return msg;
        }
    }
}
