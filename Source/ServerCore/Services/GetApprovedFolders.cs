using OCUnion.Transfer.Model;
using ServerCore.Model;
using ServerOnlineCity.Model;
using Transfer;

namespace ServerOnlineCity.Services
{
    internal sealed class GetApprovedFolders : IGenerateResponseContainer
    {
        public int RequestTypePackage => (int)PackageType.Request37GetApproveFolders;

        public int ResponseTypePackage => (int)PackageType.Response38GetApproveFolders;

        public ModelContainer GenerateModelContainer(ModelContainer request, ServiceContext context)
        {
            if (context.Player == null) return null;
            var result = new ModelContainer() { TypePacket = ResponseTypePackage };
            result.Packet = checkFiles((ModelInt)request.Packet, context);
            return result;
        }

        private ModelModsFiles checkFiles(ModelInt packet, ServiceContext context)
        {
            return ServerManager.ServerSettings.AppovedFolderAndConfig;
        }
    }
}
