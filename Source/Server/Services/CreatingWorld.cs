using System.Collections.Generic;
using Model;
using OCUnion.Transfer.Model;
using ServerOnlineCity.Model;
using Transfer;

namespace ServerOnlineCity.Services
{
    internal sealed class CreatingWorld : IGenerateResponseContainer
    {
        public int RequestTypePackage => (int)PackageType.Request7CreateWorld;

        public int ResponseTypePackage => (int)PackageType.Response8WorldCreated;

        public ModelContainer GenerateModelContainer(ModelContainer request, ServiceContext context)
        {
            if (context.Player == null) return null;
            var result = new ModelContainer() { TypePacket = ResponseTypePackage };
            result.Packet = creatingWorld((ModelCreateWorld)request.Packet, context);
            return result;
        }

        public ModelStatus creatingWorld(ModelCreateWorld packet, ServiceContext context)
        {
            lock (context.Player)
            {
                if (!context.Player.IsAdmin)
                {
                    return new ModelStatus()
                    {
                        Status = 1,
                        Message = "Access is denied"
                    };
                }

                if (!string.IsNullOrEmpty(Repository.GetData.WorldSeed))
                {
                    return new ModelStatus()
                    {
                        Status = 2,
                        Message = "The world is already created"
                    };
                }

                var data = Repository.GetData;
                data.WorldSeed = packet.Seed;
                data.WorldScenarioName = packet.ScenarioName;
                data.WorldDifficulty = packet.Difficulty;
                data.WorldStoryteller = packet.Storyteller;
                data.WorldMapSize = packet.MapSize;
                data.WorldPlanetCoverage = packet.PlanetCoverage;
                data.WorldObjects = packet.WObjects ?? new List<WorldObjectEntry>();
                data.WorldObjectsDeleted = new List<WorldObjectEntry>();
                data.Orders = new List<TradeOrder>();
                data.OrdersPlaceServerIdByTile = new Dictionary<int, long>();
                Repository.Get.ChangeData = true;

                ServerManager.SaveSettings(data.WorldStoryteller, data.WorldDifficulty);
            }

            return new ModelStatus()
            {
                Status = 0,
                Message = null
            };
        }
    }
}