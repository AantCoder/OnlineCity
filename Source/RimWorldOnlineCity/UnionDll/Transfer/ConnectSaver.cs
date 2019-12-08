using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace Transfer
{
    /// <summary>
    /// Таймер фоново поддерживающий открытое соединение при простое
    /// </summary>
    public static class ConnectSaver
    {
        private static Dictionary<ConnectClient, Action<ConnectClient>> Clients = new Dictionary<ConnectClient, Action<ConnectClient>>();

        private static Thread Worker;

        public static void AddClient(ConnectClient client, Action<ConnectClient> ping)
        {
            Clients.Add(client, ping);
            StartWorker();
        }

        private static void StartWorker()
        {
            if (Worker != null) return;
            Worker = new Thread(WorkerDo);
            Worker.IsBackground = true;
            Worker.Start();
        }

        private static void WorkerDo()
        {
            while (Clients.Count > 0)
            {
                Thread.Sleep(60000);
                var now = DateTime.UtcNow.AddMinutes(5);

                lock (Clients)    // for resolving System.InvalidOperationException: 'Коллекция была изменена; невозможно выполнить операцию перечисления.'              
                {
                    foreach (var client in Clients.Keys)
                    {
                        if (!client.Client.Connected)
                        {
                            Clients.Remove(client);
                            continue;
                        }
                        if (now > client.LastSend)
                        {
                            //запуск пинга через 5-6 мин после последнего обращения (в т.ч. пинга)
                            Clients[client](client);
                        }
                    }
                }
            }

            Worker = null;
        }
    }
}
