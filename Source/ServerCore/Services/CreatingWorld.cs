using System.Collections.Generic;
using Model;
using ServerOnlineCity.Model;
using Transfer;

namespace ServerOnlineCity.Services
{
    internal sealed class CreatingWorld : IGenerateResponseContainer
    {
        public int RequestTypePackage => 7;

        public int ResponseTypePackage => 8;

        public ModelContainer GenerateModelContainer(ModelContainer request, ref PlayerServer player)
        {
            if (player == null) return null;
            var result = new ModelContainer() { TypePacket = ResponseTypePackage };
            result.Packet = creatingWorld((ModelCreateWorld)request.Packet, ref player);
            return result;
        }

        public ModelStatus creatingWorld(ModelCreateWorld packet, ref PlayerServer player)
        {
            lock (player)
            {
                if (!player.IsAdmin)
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
                data.WorldDifficulty = packet.Difficulty;
                data.WorldMapSize = packet.MapSize;
                data.WorldPlanetCoverage = packet.PlanetCoverage;
                data.WorldObjects = packet.WObjects ?? new List<WorldObjectEntry>();
                data.WorldObjectsDeleted = new List<WorldObjectEntry>();
                data.Orders = new List<OrderTrade>();
                Repository.Get.ChangeData = true;
            }

            return new ModelStatus()
            {
                Status = 0,
                Message = null
            };
        }
    }
}