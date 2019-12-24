using OCUnion;
using ServerOnlineCity.Model;
using ServerOnlineCity.Services;
using System;
using System.Linq;
using Transfer;

namespace ServerOnlineCity
{
    public class Service
    {
        public PlayerServer Player;

        public bool CheckChat(DateTime time)
        {
            if (Player == null) return false;
            return Player.Chats.Any(ct => ct.Posts.Any(p => p.Time > time));
        }

        internal ModelContainer GetPackage(ModelContainer inputPackage)
        {           
            if (ServerManager.ServiceDictionary.TryGetValue((byte)inputPackage.TypePacket, out IGenerateResponseContainer generateResponseContainer))
            {
                Loger.Log("Server " + (Player == null ? "     " : Player.Public.Login.PadRight(5)) + generateResponseContainer.GetType().Name);
                return generateResponseContainer.GenerateModelContainer(inputPackage, ref Player);
            }

            Loger.Log("Server " + (Player == null ? "     " : Player.Public.Login.PadRight(5)) + $" Reponse for type {inputPackage.TypePacket} not found");

            return new ModelContainer() { TypePacket = 0 };
        }
    }
}
