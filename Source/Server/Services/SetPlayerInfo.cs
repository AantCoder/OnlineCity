using OCUnion.Transfer.Model;
using ServerCore.Model;
using ServerOnlineCity.Model;
using System;
using Transfer;

namespace ServerOnlineCity.Services
{

    internal sealed class SetPlayerInfo : IGenerateResponseContainer
    {
        public int RequestTypePackage => (int)PackageType.Request41SetPlayerInfo;

        public int ResponseTypePackage => (int)PackageType.Response42SetPlayerInfo;

        public ModelContainer GenerateModelContainer(ModelContainer request, ServiceContext context)
        {
            if (context.Player == null) return null;
            var result = new ModelContainer() { TypePacket = ResponseTypePackage };
            result.Packet = setPlayerInfo((ModelPlayerInfo)request.Packet, context);
            return result;
        }

        private ModelStatus setPlayerInfo(ModelPlayerInfo packet, ServiceContext context)
        {
            lock (context.Player)
            {
                var data = Repository.GetData;

                if (context.Player.Public.EnablePVP != packet.EnablePVP)
                {
                    if (context.Player.TimeChangeEnablePVP >= DateTime.UtcNow)
                    {
                        return new ModelStatus()
                        {
                            Status = 1,
                            Message = "You can’t change the status of PVP yet",
                        };
                    }
                    context.Player.TimeChangeEnablePVP = DateTime.UtcNow.AddHours(24);
                    context.Player.Public.EnablePVP = packet.EnablePVP;
                }
                context.Player.Public.AboutMyText = packet.AboutMyTextBox;
                context.Player.Public.EMail = packet.EMail;
                context.Player.Public.DiscordUserName = packet.DiscordUserName;
                context.Player.SettingDelaySaveGame = packet.DelaySaveGame;

            }
            return new ModelStatus();
        }
    }
}
