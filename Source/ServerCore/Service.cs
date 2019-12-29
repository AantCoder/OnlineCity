using OCUnion;
using ServerOnlineCity.Model;
using ServerOnlineCity.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Transfer;

namespace ServerOnlineCity
{
    public class Service
    {
        public static IReadOnlyDictionary<int, IGenerateResponseContainer> ServiceDictionary { get; private set; }

        public PlayerServer Player;

        static Service()
        {
            DependencyInjection();
        }

        private static void DependencyInjection()
        {
            //may better way use a native .Net Core DI
            var d = new Dictionary<int, IGenerateResponseContainer>();
            foreach (var type in Assembly.GetEntryAssembly().GetTypes())
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
            if (Player == null)
            {
                return false;
            }

            return Player.Chats.Any(ct => ct.Posts.Any(p => p.Time > time));
        }

        internal ModelContainer GetPackage(ModelContainer inputPackage)
        {
            if (ServiceDictionary.TryGetValue(inputPackage.TypePacket, out IGenerateResponseContainer generateResponseContainer))
            {
                Loger.Log("Server " + (Player == null ? "     " : Player.Public.Login.PadRight(5)) + generateResponseContainer.GetType().Name);
                return generateResponseContainer.GenerateModelContainer(inputPackage, ref Player);
            }

            Loger.Log("Server " + (Player == null ? "     " : Player.Public.Login.PadRight(5)) + $" Response for type {inputPackage.TypePacket} not found");

            return new ModelContainer() { TypePacket = 0 };
        }
    }
}
