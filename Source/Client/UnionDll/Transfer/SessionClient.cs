using OCUnion;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Transfer;
using Util;
using Model;
using OCUnion.Transfer.Model;
using OCUnion.Transfer;
using System.Threading.Tasks;
using System.Threading;

namespace Transfer
{
    public class SessionClient
    {
        public const int DefaultPort = 19019; // :) https://www.random.org/integers/?num=1&min=5001&max=49151&col=5&base=10&format=html&rnd=new
        public const bool UseCryptoKeys = false;
        private Object LockObj = new Object();

        public Action<int, string, ModelStatus> OnPostingChatAfter;
        public Func<int, string, ModelStatus> OnPostingChatBefore;

        #region

        /// <summary>
        /// Поддерживаем статус открытого соединения пока пытаемся переподключиться, чтобы не разблокировать уязвимости обычной игры
        /// </summary>
        public static bool IsRelogin = false;
        public bool IsLogined
        {
            get { return IsLogined_ || IsRelogin; }
            private set { IsLogined_ = value; }
        }
        private volatile bool IsLogined_ = false;
        public static DateTime LoginTime;

        public ConnectClient Client;
        private byte[] Key;
        public int ErrorCode;
        public string ErrorMessage;

        public void Disconnect()
        {
            try
            {
                IsLogined = false;
                if (Client != null) Client.Dispose();
            }
            catch
            {
            }

            Client = null;
        }

        public void Connect(string addr, int port)
        {
            ErrorMessage = null;

            try
            {
                IsLogined = false;
                if (Client != null) Client.Dispose();
            }
            catch
            { }

            try
            {
                //Loger.Log("Client Connect1");
                // Generate open-close keys  KClose-KOpen
                //Генерим рандомную пару КЗакр-КОткр
                var crypto = new CryptoProvider();
                if (UseCryptoKeys) crypto.GenerateKeys();

                //Loger.Log("Client Connect2");
                Client = new ConnectClient(addr, port);

                //Loger.Log("Client Connect3");//Строго первый пакет: Передаем серверу КОткр
                if (UseCryptoKeys)
                    Client.SendMessage(Encoding.UTF8.GetBytes(crypto.OpenKey));
                else
                    Client.SendMessage(new byte[1] { 0x00 });

                //Loger.Log("Client Connect4");
                //Строго первый ответ: Передаем клиенту КОткр(Сессия)
                var rc = Client.ReceiveBytes();
                if (UseCryptoKeys)
                    Key = crypto.Decrypt(rc);
                else
                    Key = rc;

                //Loger.Log("Client Connect5");
                //Обмен по протоколу ниже: Передаем серверу Сессия(Логин - Пароль или запрос на создание)

                //Запускаем таймер фоново поддерживающий открытое соединение при простое
                ConnectSaver.AddClient(Client, (cl) =>
                {
                    lock (LockObj)
                    {
                        cl.SendMessage(new byte[1] { 0x00 });
                        cl.ReceiveBytes();
                    }
                });
            }
            catch (Exception e)
            {
                ErrorCode = -1;
                ErrorMessage = e.Message
                    + (e.InnerException == null ? "" : " -> " + e.InnerException.Message);
                ExceptionUtil.ExceptionLog(e, "Client");
                throw;
            }
        }

        /// <summary>
        /// Пинг
        /// </summary>
        /// <returns></returns>
        public bool ServicePing()
        {
            try
            {
                lock (LockObj)
                {
                    ErrorCode = 0;
                    ErrorMessage = null;
                    Client.SendMessage(new byte[1] { 0x00 });

                    var rec = Client.ReceiveBytes();

                    return rec.Length == 1 && rec[0] == 0x00;
                }
            }
            catch (Exception e)
            {
                ErrorCode = -1;
                ErrorMessage = e.Message
                    + (e.InnerException == null ? "" : " -> " + e.InnerException.Message);
                ExceptionUtil.ExceptionLog(e, "Client ServicePing ");
                return false;
            }
        }

        /// <summary>
        /// Проверка есть ли новое на сервере, используется только для чата 
        /// </summary>
        /// <returns></returns>
        public bool? ServiceCheck()
        {
            try
            {
                lock (LockObj)
                {
                    ErrorCode = 0;
                    ErrorMessage = null;
                    Client.SendMessage(new byte[1] { 0x01 });

                    var rec = Client.ReceiveBytes();

                    return rec.Length == 1 && rec[0] == 0x01;
                }
            }
            catch (Exception e)
            {
                ErrorCode = -1;
                ErrorMessage = e.Message
                    + (e.InnerException == null ? "" : " -> " + e.InnerException.Message);
                ExceptionUtil.ExceptionLog(e, "Client ServiceCheck ");
                return null;
            }
        }

        /// <summary>
        /// Передаем и принимаем объект ModelContainer
        /// </summary>
        private ModelContainer Trans(ModelContainer sendObj)
        {
            if (Client == null)
                throw new ApplicationException("Session is not connected.");

            lock (LockObj)
            {
                ErrorCode = 0;
                ErrorMessage = null;

                var time1 = DateTime.UtcNow;
                
                var ob = GZip.ZipObjByte(sendObj); //Serialize
                var send = CryptoProvider.SymmetricEncrypt(ob, Key);
                
                if (send.Length > 1024 * 512) Loger.Log($"Client Network toS {send.Length} unzip {GZip.LastSizeObj} ");
                var time2 = DateTime.UtcNow;
                
                Client.SendMessage(send);
                
                var time3 = DateTime.UtcNow;
                
                var rec = Client.ReceiveBytes();
                
                var time4 = DateTime.UtcNow;
                
                var rec2 = CryptoProvider.SymmetricDecrypt(rec, Key);
                
                var time5 = DateTime.UtcNow;
                
                var res = (ModelContainer)GZip.UnzipObjByte(rec2); //Deserialize
                
                var time6 = DateTime.UtcNow;
                if (rec.Length > 1024 * 512) Loger.Log($"Client Network fromS {rec.Length} unzip {GZip.LastSizeObj} ");

                if ((time5 - time1).TotalMilliseconds > 900)
                {
                    Loger.Log($"Client Network timeSerialize {(time2 - time1).TotalMilliseconds}" +
                        $" timeSend {(time3 - time2).TotalMilliseconds}" +
                        $" timeReceive {(time4 - time3).TotalMilliseconds}" +
                        $" timeDecrypt {(time5 - time4).TotalMilliseconds}" +
                        $" timeDeserialize {(time6 - time5).TotalMilliseconds}");
                }

                return res;
            }
        }

        /// <summary>
        /// Передаем объект с указанием номера типа.
        /// </summary>
        protected T TransObject<T>(object objOut, int typeOut, int typeIn)
            where T : class
        {
            //Loger.Log("Client T2");
            try
            {
                var pack = new ModelContainer()
                {
                    TypePacket = typeOut,
                    Packet = objOut
                };
                var res = Trans(pack);
                var stat = res.Packet as T;
                if (res.TypePacket != typeIn
                    || stat == null)
                    throw new ApplicationException($"Unknow server error TransObject({typeOut} -> {typeIn}) responce: {res.TypePacket} "
                        + (res.Packet == null ? "null" : res.Packet.GetType().Name));
                return stat;
            }
            catch (Exception e)
            {
                ErrorCode = -1;
                ErrorMessage = e.Message
                    + (e.InnerException == null ? "" : " -> " + e.InnerException.Message);
                ExceptionUtil.ExceptionLog(e, "Client");
                throw;
            }
        }

        public T TransObject2<T>(object objOut, PackageType typeOut, PackageType typeIn)
            where T : class
        {
            return TransObject<T>(objOut, (int)typeOut, (int)typeIn);
        }

        public Task<R> TransAsync<T, R>(T objOut) where T : ISendable where R : class, ISendable, new()
        {
            var inCode = new R().PackageType;
            return Task.Factory.StartNew(() => TransObject2<R>(objOut, objOut.PackageType, inCode), new CancellationTokenSource().Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
        }

        /// <summary>
        /// Передаем объект с указанием номера типа.
        /// Ответ должен придти как ModelStatus со статусов == 0 и указанным номером типа.
        /// </summary>
        private bool TransStatus(object objOut, int typeOut, int typeIn)
        {
            var stat = TransObject<ModelStatus>(objOut, typeOut, typeIn);

            if (stat != null && stat.Status != 0)
            {
                ErrorCode = stat.Status;
                ErrorMessage = stat.Message;
                return false;
            }
            return stat != null;
        }
        private async Task TransStatusAsync(object objOut, PackageType typeOut, PackageType typeIn)
        {
            var stat = await Task.Factory.StartNew(() => TransObject<ModelStatus>(objOut, (int)typeOut, (int)typeIn), new CancellationTokenSource().Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
            if (stat == null)
                throw new ApplicationException("Server did not send a response.");
            if (stat.Status != 0)
                throw new ApplicationException(stat.Message);
        }
        #endregion

        public async Task Registration(string login, string pass, string email)
        {
            var packet = new ModelLogin() { Login = login, Pass = pass, Email = email, Version = MainHelper.VersionNum };
            await TransStatusAsync(packet, PackageType.Request1Register, PackageType.Response2Register);

            IsLogined = true;
            LoginTime = DateTime.UtcNow;
        }

        public async Task Login(string login, string pass, string email)
        {
            var packet = new ModelLogin() { Login = login, Pass = pass, Email = email, Version = MainHelper.VersionNum };
            await TransStatusAsync(packet, PackageType.Request3Login, PackageType.Response4Login);
            
            IsLogined = true;
            LoginTime = DateTime.UtcNow;
        }

        public async Task Reconnect(string login, string key, string email)
        {
            var packet = new ModelLogin() { Login = login, KeyReconnect = key, Email = email };
            await TransStatusAsync(packet, PackageType.Request3Login, PackageType.Response4Login);

            IsLogined = true;
        }

        public ModelInfo GetInfo(ServerInfoType serverInfoType)
        {
            Loger.Log("Client GetInfo " + serverInfoType.ToString());
            var packet = new ModelInt() { Value = (int)serverInfoType };
            var stat = TransObject<ModelInfo>(packet, (int)PackageType.Request5UserInfo, (int)PackageType.Response6UserInfo);
            return stat;
        }

        public ModelPlayToClient PlayInfo(ModelPlayToServer info)
        {
            //Loger.Log("Client PlayInfo "/* + info.TypeInfo.ToString()*/);
            var stat = TransObject<ModelPlayToClient>(info, (int)PackageType.Request11, (int)PackageType.Response12);
            return stat;
        }

        public ModelUpdateChat UpdateChat(ModelUpdateTime modelUpdate)
        {
            Loger.Log("Client UpdateChat " + modelUpdate.Time.ToGoodUtcString());
            var packet = modelUpdate;
            var stat = TransObject<ModelUpdateChat>(packet, (int)PackageType.Request17, (int)PackageType.Response18);
    
            return stat;
        }

        public ModelStatus PostingChat(int chatId, string msg, bool raw = false)
        {
            Loger.Log("Client PostingChat " + chatId.ToString() + ", " + msg);

            if (!raw && OnPostingChatBefore != null)
            {
                var cancel = OnPostingChatBefore(chatId, msg);
                if (cancel != null) return cancel;
            }

            var packet = new ModelPostingChat() { IdChat = chatId, Message = msg };
            var stat = TransObject<ModelStatus>(packet, (int)PackageType.Request19PostingChat, (int)PackageType.Response20PostingChat);

            ErrorCode = stat?.Status ?? 0;
            ErrorMessage = stat?.Message;

            if (!raw && OnPostingChatAfter != null) OnPostingChatAfter(chatId, msg, stat);

            return stat;
        }

        public Player GetPlayerByToken(Guid guidToken)
        {
            var stat = TransObject2<Player>(guidToken, PackageType.RequestPlayerByToken, PackageType.ResponsePlayerByToken);

            return stat;
        }

        //WIP World Object
        public ModelGameServerInfo GetGameServerInfo()
        {
            Loger.Log("Client Get WorldObject From Server");
            var packet = new ModelInt() { Value = 1 };
            var stat = TransObject<ModelGameServerInfo>(packet, (int)PackageType.Request43WObjectUpdate, (int)PackageType.Response44WObjectUpdate);
            return stat;
        }

        public List<string> AnyLoad(List<long> hashs)
        {
            var packet = new ModelAnyLoad() { Hashs = hashs };
            var stat = TransObject<ModelAnyLoad>(packet, (int)PackageType.Request45AnyLoad, (int)PackageType.Response46AnyLoad);
            return (stat as ModelAnyLoad)?.Datas;
        }

        public ModelFileSharing FileSharingDownload(FileSharingCategory category, string name)
        {
            var packet = new ModelFileSharing() { Category = category, Name = name };
            var stat = TransObject<ModelFileSharing>(packet, (int)PackageType.Request49FileSharing, (int)PackageType.Response50FileSharing);
            return stat;
        }

        /// <summary>
        /// Обновляет данные в fileSharing, если по указанному Name изменился Hash. Иначе возвращает fileSharing.
        /// Поле Data в fileSharing игнорируется.
        /// </summary>
        /// <param name="fileSharing"></param>
        /// <returns></returns>
        public ModelFileSharing FileSharingDownload(ModelFileSharing fileSharing)
        {
            var packet = new ModelFileSharing() { Category = fileSharing.Category, Name = fileSharing.Name, Hash = fileSharing.Hash };
            var stat = TransObject<ModelFileSharing>(packet, (int)PackageType.Request49FileSharing, (int)PackageType.Response50FileSharing);
            return (stat?.Data == null ? fileSharing : stat);
        }

        public ModelFileSharing FileSharingUpload(FileSharingCategory category, string name, byte[] data)
        {
            var packet = new ModelFileSharing() { Category = category, Name = name, Data = data };
            var stat = TransObject<ModelFileSharing>(packet, (int)PackageType.Request49FileSharing, (int)PackageType.Response50FileSharing);
            return stat;
        }

    }
}