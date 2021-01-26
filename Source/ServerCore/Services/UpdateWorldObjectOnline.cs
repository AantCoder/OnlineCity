﻿using System.Linq;
using OCUnion.Transfer.Model;
using ServerOnlineCity.Model;
using Transfer;

namespace ServerOnlineCity.Services
{
    internal sealed class UpdateWorldObjectOnline : IGenerateResponseContainer
    {
        public int RequestTypePackage => (int)PackageType.Request43WObjectUpdate;

        public int ResponseTypePackage => (int)PackageType.Response44WObjectUpdate;

        public ModelContainer GenerateModelContainer(ModelContainer request, ServiceContext context)
        {
            var result = new ModelContainer() { TypePacket = ResponseTypePackage };
            result.Packet = GetWorldObjectUpdate((ModelInt)request.Packet, context);
            return result;
        }

        private ModelWorldObjectOnline GetWorldObjectUpdate(ModelInt packet, ServiceContext context)
        {
            var data = Repository.GetData;
            var toClientWObject = new ModelWorldObjectOnline();
            if (packet != null)
            {
               toClientWObject.WObjectOnlineList = data.WorldObjectOnlineList;
            }

            return toClientWObject;
        }
    }
}