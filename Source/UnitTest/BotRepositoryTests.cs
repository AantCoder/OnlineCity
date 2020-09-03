using System;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OCUnion.Transfer;
using OCUnion.Transfer.Types;
using Transfer;
using Util;

namespace OC.UnitTest
{
    // Important Start Server before
    // Важно: сначала надо запустить сервер ;-)
    // Если есть идеи как запустить консольку сервера и тесты, будет здорово
    //[DeploymentItem("EntityFramework.SqlServer.dll")] // Потом атрибут

    // before must be two users:111 (admin)
    // 222 Usaly user
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
            var f1 = _sessionClient.GetInfo(ServerInfoType.Full);
            Assert.IsNotNull(f1);
            var f2 = _sessionClient.GetInfo(ServerInfoType.Short);
            Assert.IsNotNull(f2);
            var f3 = _sessionClient.GetInfo(ServerInfoType.FullWithDescription);
            Assert.IsNotNull(f3);
            //Assert.IsFalse(string.IsNullOrEmpty(f3.Description));
        }

        [TestMethod]
        public void GetTokenFromGame()
        {
            checkGetToken("/Discord", 0);
            checkGetToken("/Discord servertoken", 1);
        }

        [TestMethod]
        public void CheckGrants()
        {
            var res2 = _sessionClient.PostingChat(1, "/grants type Nobody");
            Assert.IsTrue(res2.Status == (int)ChatCmdResult.UserNotFound);
            var res3 = _sessionClient.PostingChat(1, "/grants add 222 2");
            Assert.IsTrue(res3.Status == 0);
            var res4 = _sessionClient.PostingChat(1, "/grants revoke 222 2");
            Assert.IsTrue(res4.Status == 0);
            var res5 = _sessionClient.PostingChat(1, "/grants type 222");
            Assert.IsTrue(res5.Status == 0);
        }

        private void checkGetToken(string msg, int index)
        {
            var res = _sessionClient.PostingChat(1, msg);
            Assert.IsTrue(res.Status == 0);
            var ic = new ModelUpdateTime();
            var dc = _sessionClient.UpdateChat(ic);
            Assert.IsNotNull(dc);
            Assert.IsTrue(dc.Chats[0].OwnerLogin == userName);

            var m = dc.Chats[0].Posts[index].Message;
            var boolres = Guid.TryParse(m, out Guid guid);
            Assert.IsTrue(boolres);
        }
    }
}