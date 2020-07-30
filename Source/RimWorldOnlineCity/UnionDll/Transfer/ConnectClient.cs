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
        protected const int DefaultTimeout = 180000; //3 мин
        public DateTime LastSend;
        public long CurrentRequestLength = 0;
        public DateTime CurrentRequestStart = DateTime.MinValue;

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

            CurrentRequestStart = DateTime.UtcNow;
            CurrentRequestLength = message.Length + packlength.Length;
            try
            {
                ClientStream.Write(packlength, 0, packlength.Length);
                ClientStream.Write(message, 0, message.Length);            
            }
            finally
            {
                CurrentRequestStart = DateTime.MinValue;
            }

            LastSend = DateTime.UtcNow;
        }

        public byte[] ReceiveBytes()
        {
            //кол-во байт в начале в которых передается длинна сообщения
            int Int32Length = 4;
            //длина передаваемого сообщения (принимается в первых 4 байтах (константа Int32Length))
            int lenghtAllMessageByte;

            CurrentRequestStart = DateTime.UtcNow;
            CurrentRequestLength = Int32Length;
            try
            {

                byte[] receiveBuffer = ReceiveBytes(Int32Length);
                lenghtAllMessageByte = BitConverter.ToInt32(receiveBuffer, 0);
                if (lenghtAllMessageByte == 0) return new byte[0];

                CurrentRequestStart = DateTime.UtcNow;
                CurrentRequestLength = lenghtAllMessageByte;

                receiveBuffer = ReceiveBytes(lenghtAllMessageByte);
                return receiveBuffer;
            }
            finally
            {
                CurrentRequestStart = DateTime.MinValue;
            }
        }

        private long ReceiveId;
        private Dictionary<long, object> ReceiveReady = new Dictionary<long, object>();
        private long SilenceTime = 180000;

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
            int numberOfBytesRead = 0;
            //длина передаваемого сообщения (принимается в первых 4 байтах (константа Int32Length))
            int lenghtAllMessageByte = countByte;
            var timeOut = DateTime.UtcNow.AddMilliseconds(SilenceTime);

            while (lenghtAllMessageByte > 0)
            {
                int maxCountRead = receiveBuffer.Length;
                if (maxCountRead > lenghtAllMessageByte) maxCountRead = lenghtAllMessageByte;


                //numberOfBytesRead = ClientStream.Read(receiveBuffer, 0, maxCountRead);

                var receiveId = Interlocked.Increment(ref ReceiveId);
                ClientStream.BeginRead(receiveBuffer, 0, maxCountRead, ReceiveBytescallback, receiveId);

                while (!ReceiveReady.ContainsKey(receiveId)
                    && timeOut > DateTime.UtcNow)
                    Thread.Sleep(1);

                lock (ReceiveReady)
                {
                    if (ReceiveReady.ContainsKey(receiveId))
                    {
                        var objRes = ReceiveReady[receiveId];
                        if (objRes is Exception) throw (Exception)objRes;
                        numberOfBytesRead = (int)ReceiveReady[receiveId];
                        ReceiveReady.Remove(receiveId);
                    }
                    else
                        throw new ConnectSilenceTimeOutException();
                }

                if (!Client.Client.Connected)
                {
                    throw new ConnectNotConnectedException();
                }


                if (numberOfBytesRead == 0)
                {
                    if (timeOut < DateTime.UtcNow)
                        throw new ConnectSilenceTimeOutException();
                    Thread.Sleep(1);
                }
                else
                {
                    timeOut = DateTime.UtcNow.AddMilliseconds(SilenceTime);
                    Buffer.BlockCopy(receiveBuffer, 0, msg, offset, numberOfBytesRead);
                    offset += numberOfBytesRead;
                    lenghtAllMessageByte -= numberOfBytesRead;
                }
            };

            return msg;
        }

        public class ConnectSilenceTimeOutException : Exception
        { }
        public class ConnectNotConnectedException : Exception
        { }

        private void ReceiveBytescallback(IAsyncResult ar)
        {
            int numberOfBytesRead = 0;
            Exception exc = null;
            try
            {
                numberOfBytesRead = ClientStream.EndRead(ar);
            }
            catch (Exception e)
            {
                exc = e;
            }

            var receiveId = (long)ar.AsyncState;
            lock(ReceiveReady)
            {
                ReceiveReady.Add(receiveId, (object)exc ?? numberOfBytesRead);
            }
        }


    }
}
