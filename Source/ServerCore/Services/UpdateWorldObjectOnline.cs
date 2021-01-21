using System.Linq;
using Newtonsoft.Json;
using OCUnion;
using ServerOnlineCity.Model;
using Transfer;

namespace ServerOnlineCity.Services
{
    internal sealed class UpdateWorldObjectOnline : IGenerateResponseContainer
    {
        public int RequestTypePackage => 31;

        public int ResponseTypePackage => 32;

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

            //var json = JsonConvert.SerializeObject(toClientWObject, Formatting.Indented);
            //Loger.Log("GetWorldObjectUpdate >> " + json);
            return toClientWObject;
        }
    }
}
