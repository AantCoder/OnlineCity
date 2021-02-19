using OCUnion.Transfer.Model;
using ServerOnlineCity.Model;
using Transfer;

namespace ServerOnlineCity.Services
{
    internal sealed class UpdateWorldOnline : IGenerateResponseContainer
    {
        public int RequestTypePackage => (int)PackageType.Request43WObjectUpdate;

        public int ResponseTypePackage => (int)PackageType.Response44WObjectUpdate;

        public ModelContainer GenerateModelContainer(ModelContainer request, ServiceContext context)
        {
            var result = new ModelContainer() { TypePacket = ResponseTypePackage };
            result.Packet = GetWorldObjectUpdate((ModelInt)request.Packet, context);
            return result;
        }

        private ModelGameServerInfo GetWorldObjectUpdate(ModelInt packet, ServiceContext context)
        {
            var data = Repository.GetData;
            var toClientGServerInfo = new ModelGameServerInfo();
            if (packet != null)
            {
                toClientGServerInfo.WObjectOnlineList = data.WorldObjectOnlineList;
                toClientGServerInfo.FactionOnlineList = data.FactionOnlineList;
            }

            return toClientGServerInfo;
        }
    }
}
