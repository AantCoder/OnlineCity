using System;
using System.Linq;
using OCUnion;
using OCUnion.Transfer.Model;
using ServerOnlineCity.Model;
using Transfer;
using Transfer.ModelMails;

namespace ServerOnlineCity.Services
{
    internal sealed class FileSharing : IGenerateResponseContainer
    {
        public int RequestTypePackage => (int)PackageType.Request49FileSharing;

        public int ResponseTypePackage => (int)PackageType.Response50FileSharing;

        public ModelContainer GenerateModelContainer(ModelContainer request, ServiceContext context)
        {
            if (context.Player == null) return null;

            var fileSharing = (ModelFileSharing)request.Packet;

            if (fileSharing.Data != null)
            {
                //запрос на запись файла на сервер
                if (!Repository.GetFileSharing.SaveFileSharing(context.Player, fileSharing))
                {
                    fileSharing.Hash = null; //признак ошибки или отказа
                }
                fileSharing.Data = null;
            }
            else
            {
                //запрос на чтение с сервера
                Repository.GetFileSharing.LoadFileSharing(context.Player, fileSharing);
            }

            var result = new ModelContainer() 
            { 
                TypePacket = ResponseTypePackage, 
                Packet = fileSharing
            };
            return result;
        }
    }
}
