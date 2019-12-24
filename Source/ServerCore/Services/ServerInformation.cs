using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using System.Text;
using OCUnion;
using ServerCore.Model;
using ServerOnlineCity.Model;
using Transfer;

namespace ServerOnlineCity.Services
{
    internal sealed class ServerInformation : IGenerateResponseContainer
    {
        public int RequestTypePackage => 5;

        public int ResponseTypePackage => 6;

        public ModelContainer GenerateModelContainer(ModelContainer request, ref PlayerServer player)
        {
            if (player == null) return null;
            var result = new ModelContainer() { TypePacket = ResponseTypePackage };
            result.Packet = GetInfo((ModelInt)request.Packet, ref player);
            return result;
        }

        public ModelInfo GetInfo(ModelInt packet, ref PlayerServer player)
        {
            lock (player)
            {
                switch (packet.Value)
                {
                    case 1:
                        {
                            var result = GetModelInfo(player);
                            return result;
                        }
                    case 3:
                        {
                            //передача файла игры, для загрузки WorldLoad();
                            var result = new ModelInfo();
                            result.SaveFileData = player.SaveDataPacket;
                            return result;
                        }
                    case 4:
                        {
                            var result = GetModelInfo(player);
                            //result.Description = "";
                            var displayAttributes = new List<Tuple<int, string>>();

                            foreach (var prop in typeof(ServerSettings).GetFields())
                            {
                                var attribute = prop.GetCustomAttributes(typeof(DisplayAttribute)).FirstOrDefault();
                                if (attribute is null || !prop.IsPublic)
                                {
                                    continue;
                                }

                                var dispAtr = (DisplayAttribute)attribute;
                                var strvalue = string.IsNullOrEmpty(dispAtr.GetDescription()) ? prop.Name : dispAtr.GetDescription();
                                strvalue = strvalue + "=" + prop.GetValue(ServerManager.ServerSettings).ToString();
                                var order = dispAtr.GetOrder().HasValue ? dispAtr.GetOrder().Value : 0;
                                displayAttributes.Add(Tuple.Create(order, strvalue));
                            }

                            var sb = new StringBuilder();
                            var sorte = new List<string>(displayAttributes.OrderBy(x => x.Item1).Select(y => y.Item2)).AsReadOnly();
                            foreach (var prop in sorte)
                            {
                                sb.AppendLine(prop);
                            }

                            //result.Description = sb.ToString();
                            return result;
                        }
                    default:
                        {
                            // краткая (зарезервированно, пока не используется) fullInfo = false
                            var result = new ModelInfo();
                            return result;
                        }
                }
            }
        }

        private ModelInfo GetModelInfo(PlayerServer player)
        {
            var data = Repository.GetData;
            var result =
            new ModelInfo()
            {
                My = player.Public,
                IsAdmin = player.IsAdmin,
                VersionInfo = MainHelper.VersionInfo,
                VersionNum = MainHelper.VersionNum,
                Seed = data.WorldSeed ?? "",
                MapSize = data.WorldMapSize,
                PlanetCoverage = data.WorldPlanetCoverage,
                Difficulty = data.WorldDifficulty,
                NeedCreateWorld = player.SaveDataPacket == null,
                ServerTime = DateTime.UtcNow,
            };

            return result;
        }
    }
}