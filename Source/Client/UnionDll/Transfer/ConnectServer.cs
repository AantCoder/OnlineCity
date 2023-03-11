using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace Transfer
{
    public class ConnectServer
    {
        public Action<ConnectClient> ConnectionAccepted;
        private int CheckDelay = 100;
        private bool Listening;

        public void Start(string hostname, int port)
        {
            IPAddress ipAddress;
            try
            {
                if (string.IsNullOrEmpty(hostname) || hostname == "*")
                {
                    ipAddress = IPAddress.Any;
                }
                else
                {
                    ipAddress = Dns.GetHostAddresses(hostname)[0];
                }
            }
            catch (Exception ex)
            {
                throw new ArgumentException("Failed to get the IP address of the server from its name", "hostname", ex);
            }

            TcpListener srv = new TcpListener(ipAddress, port);
            try
            {
                srv.Start();
                Listening = false;
                while (true)
                {
                    Thread.Sleep(CheckDelay);
                    while (srv.Pending())
                    {
                        if (Listening) break;
                        ConnectionAccepted(new ConnectClient(srv.AcceptTcpClient()));
                    }
                    if (Listening) break;
                }
            }
            finally
            {
                srv.Stop();
            }
        }

        public void Stop()
        {
            Listening = true;
        }
        
    }
}
