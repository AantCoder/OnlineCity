using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OC.DiscordBotServer;
using OC.DiscordBotServer.Models;
using OC.DiscordBotServer.Repositories;
using Transfer;

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

        public BotRepositoryTests()
        {
            _sessionClient = new SessionClient();
            var res = _sessionClient.Connect("127.0.0.1");
            Assert.IsTrue(res);
            res = _sessionClient.Login("q", "qqq");
            Assert.IsTrue(res);
        }



        [TestMethod]
        public void Chanel2ServerRepositoryTest()
        {
            var t = _sessionClient.IsLogined;
            //var repo = new Chanel2ServerRepository(_sqlLiteDataContext);
            // repo.AddNewServer(testObjServer);
        }
    }
}
