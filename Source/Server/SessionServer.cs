using Model;
using OCUnion;
using ServerOnlineCity.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using Transfer;
using Util;

namespace ServerOnlineCity
{
    public class SessionServer : IDisposable
    {
        public bool IsAPI = false;
        public bool IsActive = true;
        private ConnectClient Client;
        private byte[] Key;
        private static Random Rnd = new Random();
        private static Encoding KeyEncoding = Encoding.GetEncoding(1252);
        private static Encoding JsonEncoding = Encoding.UTF8;
        private CryptoProvider cryptoHash = new CryptoProvider();
        private Service Worker;
        private DateTime ServiceCheckTime;


        public void Dispose()
        {
            IsActive = false;
            Client.Dispose();
        }

        public string GetNameWhoConnect()
        {
            try
            {
                return Worker?.Context?.Player?.Public?.Login ?? "";
            }
            catch
            {
                return "";
            }
        }
        public ServiceContext GetContext()
        {
            try
            {
                return Worker?.Context;
            }
            catch
            {
                return null;
            }
        }

        private void SetKey()
        {
            var rnd = new Random();
            var k = new byte[Rnd.Next(400, 600)];
            for (int i = 0; i < k.Length; i++)
            {
                k[i] = (byte)(Rnd.Next(0, 128) + rnd.Next(0, 128));
            }
            var k2 = KeyEncoding.GetBytes("g08Р·dfgиА▀ЫЮЁрЫГxч<2en]*♫7ПМпёf#hю,>√^p147&$`tgjРмБ^g9~hjЮf%#h");

            int oldLen = k.Length;
            Array.Resize(ref k, k.Length + k2.Length);
            Array.Copy(k2, 0, k, oldLen, k2.Length);

            Key = cryptoHash.GetHash(k);
        }

        public void DoServiceJson(ConnectClient c)
        {
            var receiveReady = false;
            c.ReceiveAllByte((client, requestRaw) =>
            {
                try
                {
                    Dictionary<string, byte[]> data = new Dictionary<string, byte[]>();
                    var request = JsonEncoding.GetString(requestRaw);
                    var ii = request.IndexOf("\r\n\r\n");
                    var iiBoundary = request.IndexOf("boundary=");
                    string isBoundary = null;
                    if (iiBoundary > 0 && iiBoundary < ii)
                    {
                        iiBoundary += "boundary=".Length;
                        isBoundary = "\r\n--" + request.Substring(iiBoundary, request.IndexOf("\r\n", iiBoundary) - iiBoundary);
                    }
                    if (isBoundary != null)
                    {
                        var requestRawCode = Encoding.ASCII.GetString(requestRaw);
                        var rawCodeIndex = 0;
                        var listBoundary = request.Split(new string[] { isBoundary }, StringSplitOptions.RemoveEmptyEntries);
                        request = null;
                        for (var i = 1; i < listBoundary.Length; i++)
                        {
                            rawCodeIndex = requestRawCode.IndexOf(isBoundary, rawCodeIndex);
                            /* Content-Disposition: form-data; name="avatar"; filename="XGQKemif8eU.jpg"
                             * Content-Type: image/jpeg
                             * 
                             * ����
                             */
                            var item = listBoundary[i];
                            var itemi = item.IndexOf("name=\"");
                            if (itemi > 0)
                            {
                                itemi += "name=\"".Length;
                                var name = item.Substring(itemi, item.IndexOf("\"", itemi) - itemi);

                                rawCodeIndex = requestRawCode.IndexOf("\r\n\r\n", rawCodeIndex) + 4;
                                if (rawCodeIndex >= 4)
                                {
                                    var rawCodeIndexNext = requestRawCode.IndexOf(isBoundary, rawCodeIndex);
                                    if (rawCodeIndexNext < 0) rawCodeIndexNext = requestRaw.Length;
                                    var content = new byte[rawCodeIndexNext - rawCodeIndex];
                                    Array.Copy(requestRaw, rawCodeIndex, content, 0, rawCodeIndexNext - rawCodeIndex);
                                    //File.WriteAllBytes("e:\\test.png", content);
                                    if (request != null) request += ", ";
                                    if (content.Length > 1000)
                                    {
                                        data[name] = content;
                                    }
                                    else
                                    {
                                        request += Environment.NewLine + $"\"{name}\":\"{JsonEncoding.GetString(content).Replace("\"", "'")}\"";
                                    }
                                }
                            }
                        }
                        request = "{" + request + Environment.NewLine + "}";
                    }
                    else
                    {
                        //обрезаем и игнорируем весь заголовок
                        if (ii > 0 && request.Length - ii > 5)
                        {
                            request = request.Substring(ii + 4).Trim();
                        }
                        else
                            request = null;
                    }
                    if (request != null)
                    {
                        byte[] sendButes;
                        var send = Service.GetPackageJson(request, data);

                        if (send is byte[]) 
                        {
                            var body = (byte[])send;
                            var sendHTTP = "HTTP/1.0 200 OK\r\n"
                                //+ "Accept-Ranges: bytes\r\n"
                                //+ "Content-Length: " + body.Length + "\r\n"
                                //+ "Content-Type: image/png\r\n"
                                + "Connection: close\r\n\r\n";
                            var header = JsonEncoding.GetBytes(sendHTTP);
                            sendButes = new byte[header.Length + body.Length];
                            Array.Copy(header, 0, sendButes, 0, header.Length);
                            Array.Copy(body, 0, sendButes, header.Length, body.Length);
                        }
                        else
                        { 
                            var sendHTTP = "HTTP/1.0 200 OK\r\n"
                                + "Content-Type: application/json; charset=utf-8\r\n"
                                + "Connection: close\r\n\r\n"
                                + send.ToString();
                            sendButes = JsonEncoding.GetBytes(sendHTTP);
                        }
                        client.SendAllByte(sendButes);
                        //чтобы уменьшить спам отсекаем все запросы на статус 
                        if (!request.ToLower().Replace(" ", "").Replace("\"q\"", "q").Contains("q:\"s\""))
                            Loger.Log("DoServiceJson Request: " + request + Environment.NewLine + (send is byte[] ? sendButes.Length.ToString() : send.ToString()), Loger.LogLevel.INFO);
                    }
                }
                catch (Exception ext)
                {
                    Loger.Log("DoServiceJson Exception: " + ext.ToString(), Loger.LogLevel.ERROR);
                }
                receiveReady = true;
            }, 1024 * 1024 * 2);
            var timeOut = DateTime.UtcNow.AddSeconds(2);
            while (!receiveReady && timeOut > DateTime.UtcNow)
                Thread.Sleep(1);
        }

        public void Do(ConnectClient client, Action<Action<SessionServer>> allSessionAction)
        {
            try
            {
                Client = client;

                //Loger.Log("Server ReceiveBytes0");
                //Считываем 4 байта, если они POST то переходим в режим запроса json, иначе пердаем эти байты  дальше как часть основного протокола
                var firstByte = Client.ReceiveFourByte();
                if (firstByte?.Length == 4 
                    && firstByte[0] == 80
                    && firstByte[1] == 79
                    && firstByte[2] == 83
                    && firstByte[3] == 84) // 'POST'
                {
                    IsAPI = true;
                    DoServiceJson(client);
                    return;
                }

                Loger.Log("Server ReceiveBytes1");

                ///установка условно защищенного соединения
                //Строго первый пакет: Передаем серверу КОткр
                var rc = Client.ReceiveBytes(firstByte);
                var crypto = new CryptoProvider();
                if (SessionClient.UseCryptoKeys) crypto.OpenKey = Encoding.UTF8.GetString(rc);

                //Строго первый ответ: Передаем клиенту КОткр(Сессия)
                SetKey();
                Loger.Log("Server SendMessage1");
                if (SessionClient.UseCryptoKeys)
                    Client.SendMessage(crypto.Encrypt(Key));
                else
                    Client.SendMessage(Key);

                var context = new ServiceContext();
                context.AddrIP = ((IPEndPoint)client.Client.Client.RemoteEndPoint).Address.ToString();
                context.AllSessionAction = allSessionAction;
                Worker = new Service(context);

                ///рабочий цикл
                while (IsActive)
                {
                    var rec = Client.ReceiveBytes();
                    if (!IsActive) break;

                    if (context.Player != null)
                    {
                        lock (context.Player)
                        {
                            context.Player.Public.LastOnlineTime = DateTime.UtcNow;
                        }
                    }

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

                    var time1 = DateTime.UtcNow;

                    var rec2 = CryptoProvider.SymmetricDecrypt(rec, Key);
                    var recObj = (ModelContainer)GZip.UnzipObjByte(rec2); //Deserialize

                    if (rec.Length > 1024 * 512) Loger.Log($"Server Network fromC {rec.Length} unzip {GZip.LastSizeObj} ");
                    var time2 = DateTime.UtcNow;

                    ModelContainer sendObj;
                    try
                    {
                        sendObj = Worker.GetPackage(recObj);
                        if (!IsActive) break;
                    }
                    catch (Exception ext)
                    {
                        Loger.Log("Exception GetPackage: " + ext.ToString(), Loger.LogLevel.ERROR);
                        sendObj = null;
                    }
                    if (sendObj == null)
                    {
                        sendObj = new ModelContainer()
                        {
                            TypePacket = 0
                        };
                    }

                    var time3 = DateTime.UtcNow;

                    var ob = GZip.ZipObjByte(sendObj); //Serialize
                    var send = CryptoProvider.SymmetricEncrypt(ob, Key);

                    if (send.Length > 1024 * 512) Loger.Log($"Server Network toC {send.Length} unzip {GZip.LastSizeObj} ");
                    var time4 = DateTime.UtcNow;

                    Client.SendMessage(send);

                    var time5 = DateTime.UtcNow;

                    if ((time5 - time1).TotalMilliseconds > 900)
                    {
                        Loger.Log($"Server Network timeDeserialize {(time2 - time1).TotalMilliseconds}" +
                            $" timeWorker {(time3 - time2).TotalMilliseconds}" +
                            $" timeSerialize {(time4 - time3).TotalMilliseconds}" +
                            $" timeSend {(time5 - time4).TotalMilliseconds}");
                    }

                    if (context.Player != null)
                    {
                        lock (context.Player)
                        {
                            context.Player.Public.LastOnlineTime = DateTime.UtcNow;
                            if (context.Player.ExitReason != OCUnion.Transfer.DisconnectReason.AllGood)
                            {
                                //context.Player.ExitReason = OCUnion.Transfer.DisconnectReason.AllGood;
                                Loger.Log("Disconnect  . . ." + context.Player.ExitReason.ToString());
                                break;
                            }
                        }
                    }
                }
            }
            finally
            {
                IsActive = false;
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
    }
}