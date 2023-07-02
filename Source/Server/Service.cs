using Newtonsoft.Json;
using OCUnion;
using ServerOnlineCity.Model;
using ServerOnlineCity.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Transfer;
//using System.Text.Json;

namespace ServerOnlineCity
{
    public class Service
    {
        public static IReadOnlyDictionary<int, IGenerateResponseContainer> ServiceDictionary { get; private set; }

        public ServiceContext Context { get; private set; }
        private static ServiceAPI API { get; set; }

        static Service()
        {
            DependencyInjection();
            API = new ServiceAPI();
        }

        public Service(ServiceContext context)
        {
            Context = context;
        }

        private static void DependencyInjection()
        {
            //may better way use a native .Net Core DI
            var d = new Dictionary<int, IGenerateResponseContainer>();
            foreach (var type in Assembly.GetAssembly(typeof(Service)).GetTypes())
            {
                if (!type.IsClass)
                {
                    continue;
                }

                if (type.GetInterfaces().Any(x => x == typeof(IGenerateResponseContainer)))
                {
                    var t = (IGenerateResponseContainer)Activator.CreateInstance(type);
                    d[t.RequestTypePackage] = t;
                }
            }

            ServiceDictionary = d;
        }

        public bool CheckChat(DateTime time)
        {
            if (Context.Player == null)
            {
                return false;
            }

            // Для ускорения работы, в момент отправки сообщения пользователю, сохраняем последний отправленный индекс
            // или Если пользователя кикнули с канала, тогда по дате изменения канала
            return Context.Player.Chats.Any(ct => ct.Value.Value + 1 < ct.Key.Posts.Count || ct.Key.LastChanged > ct.Value.Time);
        }

        internal ModelContainer GetPackage(ModelContainer inputPackage)
        {
            if (ServiceDictionary.TryGetValue(inputPackage.TypePacket, out IGenerateResponseContainer generateResponseContainer))
            {
                var name = generateResponseContainer.GetType().Name;
                Loger.Log("Server " + (Context.Player == null ? "     " : Context.Player.Public.Login.PadRight(5))
                    + " " + name);
                return generateResponseContainer.GenerateModelContainer(inputPackage, Context);
            }

            Loger.Log("Server " + (Context.Player == null ? "     " : Context.Player.Public.Login.PadRight(5)) + $" Response for type {inputPackage.TypePacket} not found");

            return new ModelContainer() { TypePacket = 0 };
        }

        public static object GetPackageJson(string inputPackage, Dictionary<string, byte[]> data)
        {
            var package = JsonConvert.DeserializeObject<APIRequest>(inputPackage);
            if (data != null && data.Count > 0) package.Data = data.Values.First();
            var ret = API.GetPackage(package);
            if (ret is APIResponseRawData) return (ret as APIResponseRawData).Data;
            return JsonConvert.SerializeObject(ret);

            //var package = JsonSerializer.Deserialize<APIRequest>(inputPackage);
            //var ret = API.GetPackage(package);
            //return JsonSerializer.Serialize(ret);
        }
    }
}
