using System;
using OCUnion;
using ServerOnlineCity.Model;
using Transfer;

namespace ServerOnlineCity.Services
{
    internal class ServerInformation : IGenerateResponseContainer
    {
        public byte RequestTypePackage => 5;

        public byte ResponseTypePackage => 6;

        public ModelContainer GenerateModelContainer(ModelContainer request, ref PlayerServer player)
        {
            var result = new ModelContainer() { TypePacket = ResponseTypePackage };
            result.Packet = GetInfo((ModelInt)request.Packet, ref player);
            return result;
        }

        public ModelInfo GetInfo(ModelInt packet, ref PlayerServer player)
        {
            if (player == null) return null;

            lock (player)
            {
                var saveData = player.SaveDataPacket;
                if (packet.Value == 1)
                {
                    //полная информация fullInfo = true
                    var data = Repository.GetData;
                    var result = new ModelInfo()
                    {
                        My = player.Public,
                        IsAdmin = player.IsAdmin,
                        VersionInfo = MainHelper.VersionInfo,
                        VersionNum = MainHelper.VersionNum,
                        Seed = data.WorldSeed ?? "",
                        MapSize = data.WorldMapSize,
                        PlanetCoverage = data.WorldPlanetCoverage,
                        Difficulty = data.WorldDifficulty,
                        NeedCreateWorld = saveData == null,
                        ServerTime = DateTime.UtcNow,
                    };

                    return result;
                }
                else if (packet.Value == 3)
                {
                    //передача файла игры, для загрузки WorldLoad();
                    var result = new ModelInfo();
                    result.SaveFileData = saveData;
                    return result;
                }
                else
                {
                    //packet.Value =??? магия какая-то, двойка, наверное ))                    
                    //краткая (зарезервированно, пока не используется) fullInfo = false
                    var result = new ModelInfo();
                    return result;
                }
            }
        }
    }
}