using System;
using System.Linq;
using OCUnion;
using OCUnion.Transfer.Model;
using ServerOnlineCity.Model;
using Transfer;
using Transfer.ModelMails;

namespace ServerOnlineCity.Services
{
    internal sealed class AnyUpload : IGenerateResponseContainer
    {
        public int RequestTypePackage => (int)PackageType.Request45AnyLoad;

        public int ResponseTypePackage => (int)PackageType.Response46AnyLoad;

        public ModelContainer GenerateModelContainer(ModelContainer request, ServiceContext context)
        {
            if (context.Player == null) return null;

            var hashs = ((ModelAnyLoad)request.Packet).Hashs;
            var datas = hashs.Select(hash =>
            {
                Repository.GetData.UploadService.TryGetValue(hash, out string data);
                return data;
            }).ToList();

            var result = new ModelContainer() { TypePacket = ResponseTypePackage, Packet = new ModelAnyLoad() { Hashs = hashs, Datas = datas } };
            return result;
        }
    }
}
