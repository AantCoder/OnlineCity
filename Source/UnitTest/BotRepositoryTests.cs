using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OC.DiscordBotServer;
using OC.DiscordBotServer.Models;
using OC.DiscordBotServer.Repositories;

namespace UnitTest
{
    [DeploymentItem("EntityFramework.SqlServer.dll")] // Потом атрибут
    [TestClass]
    public class BotRepositoryTests
    {
        private readonly SqlLiteDataContext _sqlLiteDataContext;

        private Chanel2Server testObjServer = new OC.DiscordBotServer.Models.Chanel2Server()
        {
            Chanel2ServerId =1,
            IP=1,
            LastOnlineTime = DateTime.Now,
            LinkCreator=1,
            Port =1
        };

        public BotRepositoryTests()
        {
            //_sqlLiteDataContext = new SqlLiteDataContext();
        }

        [TestMethod]
        public void Chanel2ServerRepositoryTest()
        {
            var repo = new Chanel2ServerRepository(_sqlLiteDataContext);
            repo.AddNewServer(testObjServer);
        }
    }
}
