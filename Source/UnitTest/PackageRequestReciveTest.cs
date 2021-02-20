using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading;
using Transfer;
using Util;

namespace OC.UnitTest
{
    [TestClass]
    public class PackageRequestReciveTest
    {
        private readonly SessionClient _sessionClient;
        const string userName = "222";

        public PackageRequestReciveTest()
        {
            Thread.Sleep(5);
            _sessionClient = new SessionClient();
            var res = _sessionClient.Connect("127.0.0.1");
            Assert.IsTrue(res);
            var pass = new CryptoProvider().GetHash(userName);
            res = _sessionClient.Login(userName, pass, null);
            Assert.IsTrue(res);
        }
    }
}
