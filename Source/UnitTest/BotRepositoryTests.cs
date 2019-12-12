using System;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OC.DiscordBotServer;
using OC.DiscordBotServer.Models;
using OC.DiscordBotServer.Repositories;
using Transfer;
using Util;

namespace UnitTest
{
    // Important Start Server before
    // Важно: сначала надо запустить сервер ;-)
    // Если есть идеи как запустить консольку сервера и тесты, будет здорово
    //[DeploymentItem("EntityFramework.SqlServer.dll")] // Потом атрибут
    [TestClass]
    public class BotRepositoryTests
    {
        private readonly SessionClient _sessionClient;
        const string userName = "111";

        public BotRepositoryTests()
        {
            Thread.Sleep(5);
            _sessionClient = new SessionClient();
            var res = _sessionClient.Connect("127.0.0.1");
            Assert.IsTrue(res);
            var pass = new CryptoProvider().GetHash(userName);
            res = _sessionClient.Login(userName, pass);
            Assert.IsTrue(res);
        }

        [TestMethod]
        public void Chanel2ServerRepositoryTest()
        {
            var t = _sessionClient.IsLogined;

            var f = _sessionClient.GetInfo(false);
            f = _sessionClient.GetInfo(true);

        }

        [TestMethod]
        public void GetTokenFromGame()
        {
            checkGetToken("/Discord", 0);
            checkGetToken("/Discord servertoken", 1);
        }

        private void checkGetToken(string msg, int index)
        {
            var res = _sessionClient.PostingChat(1, msg);
            Assert.IsTrue(res);
            var dc = _sessionClient.UpdateChat(DateTime.UtcNow.AddHours(-1));
            Assert.IsNotNull(dc);
            Assert.IsTrue(dc.Chats[0].OwnerLogin == userName);

            var m = dc.Chats[0].Posts[index].Message;
            var boolres = Guid.TryParse(m, out Guid guid);
            Assert.IsTrue(boolres);
        }
    }
}