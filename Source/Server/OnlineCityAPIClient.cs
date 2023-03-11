using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace ServerOnlineCity
{
    public class OnlineCityAPIClient
    {
        public string Addr { get; set; }
        public int Port { get; set; }
        public string LastError { get; set; }

        private TcpClient Client;
        private NetworkStream ClientStream;
        private readonly Encoding MessageEncoding = Encoding.UTF8;
        private const int DefaultTimeout = 1000;

        public OnlineCityAPIClient(string addr, int port)
        {
            Addr = addr;
            Port = port;
        }

        public void RequestStatus(Action<APIResponseStatus> response) =>
            Request(new APIRequest() { Q = "s", }, response);

        public void RequestPlayer(string login, Action<APIResponsePlayers> response) =>
            Request(new APIRequest() { Q = "p", Login = login, }, response);

        public void RequestAllPlayers(Action<APIResponsePlayers> response) =>
            Request(new APIRequest() { Q = "a", }, response);

        public void Request<TResponse>(APIRequest request, Action<TResponse> response)
            where TResponse : APIResponse
        {
            try
            {
                var package = JsonSerializer.Serialize(request);
                Request(package, res =>
                {
                    TResponse tres = null;
                    try
                    {
                        if (string.IsNullOrEmpty(res))
                        {
                            if (LastError == null) LastError = "Empty response";
                        }
                        else
                        if (res.Contains("\"Error\":"))
                        {
                            var err = JsonSerializer.Deserialize<APIResponseError>(res);
                            if (LastError == null) LastError = err.Error;
                        }
                        else
                            tres = JsonSerializer.Deserialize<TResponse>(res);
                    }
                    catch (Exception ex)
                    {
                        if (LastError == null) LastError = ex.Message;
                    }
                    response(tres);
                });
            }
            catch (Exception ex)
            {
                LastError = ex.Message;
            }
        }

        public void Request(string send, Action<string> response)
        {
            LastError = null;
            try
            {
                Client = new TcpClient(Addr, Port);
                Client.SendTimeout = DefaultTimeout;
                Client.ReceiveTimeout = DefaultTimeout;
                ClientStream = Client.GetStream();

                var sendHTTP = "POST / HTTP/1.1\r\n"
                    + "Content-Type: application/json; charset=utf-8\r\n\r\n"
                    + send;

                var sendButes = MessageEncoding.GetBytes(sendHTTP);
                SendAllByte(sendButes);

                ReceiveAllByte(responseRaw =>
                {
                    string responseStr = null;
                    try
                    {
                        var responseHTTP = MessageEncoding.GetString(responseRaw);
                        ClientStream.Close();
                        Client.Close();
                        var ii = responseHTTP.IndexOf("\r\n\r\n");
                        if (ii > 0 && responseHTTP.Length - ii > 5)
                        {
                            responseStr = responseHTTP.Substring(ii + 4).Trim();
                        }
                    }
                    catch (Exception ex)
                    {
                        LastError = ex.Message;
                    }
                    response(responseStr);
                });
            }
            catch (Exception ex)
            {
                LastError = ex.Message;
            }
        }

        private void ReceiveAllByte(Action<byte[]> action)
        {
            try
            {
                byte[] receiveBuffer = new byte[1024 * 256];
                ClientStream.BeginRead(receiveBuffer, 0, receiveBuffer.Length
                    , (IAsyncResult ar) =>
                    {
                        try
                        {
                            var numberOfBytesRead = ClientStream.EndRead(ar);
                            if (numberOfBytesRead <= 0)
                            {
                                action(new byte[0]);
                                return;
                            }
                            byte[] receive = new byte[numberOfBytesRead];
                            Buffer.BlockCopy(receiveBuffer, 0, receive, 0, numberOfBytesRead);
                            try
                            {
                                action(receive);
                            }
                            catch { }
                        }
                        catch
                        {
                            action(new byte[0]);
                        }
                    }
                    , null);
            }
            catch
            {
                action(new byte[0]);
            }
        }

        private void SendAllByte(byte[] message)
        {
            ClientStream.Write(message, 0, message.Length);
        }
    }
}
