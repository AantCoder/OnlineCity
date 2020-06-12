using OCUnion;
using OCUnion.Common;
using OCUnion.Transfer.Model;
using System.IO;
using Transfer;
using Verse;

namespace RimWorldOnlineCity.Services
{
    public sealed class SetPlayerInfo : IOnlineCityClientService<bool>
    {
        public PackageType RequestTypePackage => PackageType.Request41SetPlayerInfo;

        public PackageType ResponseTypePackage => PackageType.Response42SetPlayerInfo;

        private readonly Transfer.SessionClient _sessionClient;

        public SetPlayerInfo(Transfer.SessionClient sessionClient)
        {
            _sessionClient = sessionClient;
        }

        public bool GenerateRequestAndDoJob(object context)
        {
            var state = _sessionClient.TransObject2<ModelStatus>((ModelPlayerInfo)context, RequestTypePackage, ResponseTypePackage);

            return state.Status == 0;
        }
    }
}
