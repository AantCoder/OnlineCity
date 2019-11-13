using Model;
using OCUnion;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Transfer;
using Util;

namespace OCServer
{
    public class SessionServer : IDisposable
    {
        public event Action<string> LogMessage;

        private ConnectClient Client;
        private byte[] Key;
        private static Random Rnd = new Random();
        private static Encoding KeyEncoding = Encoding.GetEncoding(1252);
        private CryptoProvider cryptoHash = new CryptoProvider();
        private Service Worker;
        private DateTime ServiceCheckTime;

        public void Dispose()
        {
            Client.Dispose();
        }

        private void SetKey()
        {
            var rnd = new Random();
            var k = new byte[Rnd.Next(400, 600)];
            for (int i = 0; i < k.Length; i++)
            {
                k[i] = (byte)(Rnd.Next(0, 128) + rnd.Next(0, 128));
            }
            var k2 = KeyEncoding.GetBytes("089~`tgjРР·dfgорЫГ9♫7ПМпfghjp147&$#hf%#h^^gxчмиА▀ЫЮББЮю,><2en]√");

            int oldLen = k.Length;
            Array.Resize(ref k, k.Length + k2.Length);
            Array.Copy(k2, 0, k, oldLen, k2.Length);

            Key = cryptoHash.GetHash(k);
        }

        public void Do(ConnectClient client)
        {
            Client = client;

            Loger.Log("Server ReceiveBytes1");

            ///установка условно защищенного соединения
            //Строго первый пакет: Передаем серверу КОткр
            var rc = Client.ReceiveBytes();
            var crypto = new CryptoProvider();
            if (SessionClient.UseCryptoKeys)
            {
                crypto.OpenKey = Encoding.UTF8.GetString(rc);
            }

            //Строго первый ответ: Передаем клиенту КОткр(Сессия)
            SetKey();
            Loger.Log("Server SendMessage1");
            if (SessionClient.UseCryptoKeys)
                Client.SendMessage(crypto.Encrypt(Key));
            else
                Client.SendMessage(Key);

            //if (LogMessage != null) LogMessage("session Connected");

            Worker = new Service();

            ///рабочий цикл
            while (true)
            {
                //Loger.Log("Server Loop1");
                var rec = Client.ReceiveBytes();

                if (Worker.Player != null) Worker.Player.Public.LastOnlineTime = DateTime.UtcNow;

                //отдельно обрабатываем пинг
                if (rec.Length == 1)
                {
                    if (rec[0] == 0x00)
                    {
                        Client.SendMessage(new byte[1] { 0x00 });
                    }
                    //отдельно обрабатываем запрос на обновление (ответ 0 - нет ничего, 1 - что-то есть) 
                    else if (rec[0] == 0x01)
                    {
                        var exists = ServiceCheck();
                        Client.SendMessage(new byte[1] { exists ? (byte)0x01 : (byte)0x00 });
                    }
                    continue;
                }

                //Loger.Log("Server Loop2");
                var rec2 = CryptoProvider.SymmetricDecrypt(rec, Key);
                //Loger.Log("Server " + Loger.Bytes(rec2));
                //Loger.Log("Server Loop3");
                var recObj = (ModelContainer)GZip.UnzipObjByte(rec2); //Deserialize
                //Loger.Log("Server Loop4");

                var sendObj = Service(recObj);

                //Loger.Log("Server Loop5");
                var ob = GZip.ZipObjByte(sendObj); //Serialize
                //Loger.Log("Server Loop6");
                var send = CryptoProvider.SymmetricEncrypt(ob, Key);
                //Loger.Log("Server Loop7");
                Client.SendMessage(send);
            }
        }

        /// <summary>
        /// Есть ли изменения. Сейчас используется только для чата
        /// </summary>
        /// <returns></returns>
        private bool ServiceCheck()
        {
            if (ServiceCheckTime == DateTime.MinValue)
            {
                ServiceCheckTime = DateTime.UtcNow;
                return true;
            }
            //На данный момен только проверка чата
            var res = Worker.CheckChat(ServiceCheckTime);
            ServiceCheckTime = DateTime.UtcNow;
            return res;
        }

        private ModelContainer Service(ModelContainer recObj)
        {
            var send = new ModelContainer();
            switch (recObj.TypePacket)
            {
                case 1:
                    send.TypePacket = 2;
                    Loger.Log("Server "+(Worker.Player == null ? "     " : Worker.Player.Public.Login.PadRight(5)) +" Registration");
                    send.Packet = Worker.Registration((ModelLogin)recObj.Packet);
                    break;
                case 3:
                    send.TypePacket = 4;
                    Loger.Log("Server " + (Worker.Player == null ? "     " : Worker.Player.Public.Login.PadRight(5)) + " Login");
                    send.Packet = Worker.Login((ModelLogin)recObj.Packet);
                    break;
                case 5:
                    send.TypePacket = 6;
                    Loger.Log("Server " + (Worker.Player == null ? "     " : Worker.Player.Public.Login.PadRight(5)) + " Info");
                    send.Packet = Worker.GetInfo((ModelInt)recObj.Packet);
                    break;
                case 7:
                    send.TypePacket = 8;
                    Loger.Log("Server " + (Worker.Player == null ? "     " : Worker.Player.Public.Login.PadRight(5)) + " CreatingWorld");
                    send.Packet = Worker.CreatingWorld((ModelCreateWorld)recObj.Packet);
                    break;
                case 11:
                    send.TypePacket = 12;
                    Loger.Log("Server " + (Worker.Player == null ? "     " : Worker.Player.Public.Login.PadRight(5)) + " PlayInfo");
                    send.Packet = Worker.PlayInfo((ModelPlayToServer)recObj.Packet);
                    break;
                case 15:
                    send.TypePacket = 16;
                    Loger.Log("Server " + (Worker.Player == null ? "     " : Worker.Player.Public.Login.PadRight(5)) + " SendThings");
                    send.Packet = Worker.SendThings((ModelMailTrade)recObj.Packet);
                    break;
                case 17:
                    send.TypePacket = 18;
                    send.Packet = Worker.UpdateChat((ModelUpdateTime)recObj.Packet);
                    break;
                case 19:
                    send.TypePacket = 20;
                    Loger.Log("Server " + (Worker.Player == null ? "     " : Worker.Player.Public.Login.PadRight(5)) + " PostingChat");
                    send.Packet = Worker.PostingChat((ModelPostingChat)recObj.Packet);
                    break;
                case 21:
                    send.TypePacket = 22;
                    Loger.Log("Server " + (Worker.Player == null ? "     " : Worker.Player.Public.Login.PadRight(5)) + " ExchengeEdit");
                    send.Packet = Worker.ExchengeEdit((OrderTrade)recObj.Packet);
                    break;
                case 23:
                    send.TypePacket = 24;
                    Loger.Log("Server " + (Worker.Player == null ? "     " : Worker.Player.Public.Login.PadRight(5)) + " ExchengeBuy");
                    send.Packet = Worker.ExchengeBuy((ModelOrderBuy)recObj.Packet);
                    break;
                case 25:
                    send.TypePacket = 26;
                    Loger.Log("Server " + (Worker.Player == null ? "     " : Worker.Player.Public.Login.PadRight(5)) + " ExchengeLoad");
                    send.Packet = Worker.ExchengeLoad();
                    break;
                case 27:
                    send.TypePacket = 28;
                    Loger.Log("Server " + (Worker.Player == null ? "     " : Worker.Player.Public.Login.PadRight(5)) + " AttackInitiator");
                    send.Packet = Worker.AttackOnlineInitiator((AttackInitiatorToSrv)recObj.Packet);
                    break;
                case 29:
                    send.TypePacket = 30;
                    Loger.Log("Server " + (Worker.Player == null ? "     " : Worker.Player.Public.Login.PadRight(5)) + " AttackHost");
                    send.Packet = Worker.AttackOnlineHost((AttackHostToSrv)recObj.Packet);
                    break;
                default:
                    Loger.Log("Server " + (Worker.Player == null ? "     " : Worker.Player.Public.Login.PadRight(5)) + " Error0");
                    send.TypePacket = 0;
                    break;
            }
            return send;
        }
    }
}
