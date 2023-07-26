using System;
using System.Collections.Generic;
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

            object result;
            if (request.Packet is List<ModelFileSharing>)
            {
                //only check
                var list = (List<ModelFileSharing>)request.Packet;
                var res = new List<ModelFileSharing>();
                foreach (var fileSharing in list)
                {
                    res.Add(GetFileSharing(fileSharing, context.Player, true));
                }
                result = res;
            }
            else
            {
                //full function
                var fileSharing = (ModelFileSharing)request.Packet;

                fileSharing = GetFileSharing(fileSharing, context.Player, false);

                result = fileSharing;
            }
            return new ModelContainer()
            {
                TypePacket = ResponseTypePackage,
                Packet = result,
            };
        }

        private ModelFileSharing GetFileSharing(ModelFileSharing fileSharing, PlayerServer player, bool onlyCheck)
        {
            if (!onlyCheck && fileSharing.Data != null)
            {
                //запрос на запись файла на сервер
                if (!Repository.GetFileSharing.SaveFileSharing(player, fileSharing))
                {
                    fileSharing.Hash = null; //признак ошибки или отказа
                }
                fileSharing.Data = null;
            }
            else
            {
                //запрос на чтение с сервера
                Repository.GetFileSharing.LoadFileSharing(player, fileSharing);
                if (onlyCheck) fileSharing.Data = null; //получаем сами данные, а потом сбрасываем чтобы не ломать систему кэширования
            }
            return fileSharing;
        }
    }
}
