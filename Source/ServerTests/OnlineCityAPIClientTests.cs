using Microsoft.VisualStudio.TestTools.UnitTesting;
using ServerOnlineCity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ServerOnlineCity.Tests
{
    [TestClass()]
    public class OnlineCityAPIClientTests
    {
        [TestMethod()]
        public void RequestTest()
        {
            var api = new OnlineCityAPIClient("127.0.0.1", 19019);
            //var api = new OnlineCityAPIClient("62.133.174.133", 19024);
            string response = null;
            api.Request("{Q:\"s\"}", (res) => { response = res; });
            Thread.Sleep(5000);

            api.Request("{Q:\"p\",Login:\"TheCrazeMan\"}", (res) => { response = res; });
            Thread.Sleep(5000);

            api.RequestStatus((res) => { response = $"Online {res.OnlineCount}/{res.PlayerCount}"; });
            Thread.Sleep(5000);

            api.RequestPlayer("TheCrazeMan", (res) => { response = res.Players[0].Login; });
            Thread.Sleep(5000);

            api.RequestAllPlayers((res) => { response = res.Players[0].Login; });
            Thread.Sleep(5000);
            //Assert.Fail();
        }
    }
}