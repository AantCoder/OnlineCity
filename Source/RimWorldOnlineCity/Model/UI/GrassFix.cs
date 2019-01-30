using Harmony;
using OCUnion;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace RimWorldOnlineCity
{
    /*
    [HarmonyPatch(typeof(Map))]
    [HarmonyPatch("MapPreTick")]
    public class GrassFix
    {
        private static Dictionary<int, int> MapTickOnPlaced = new Dictionary<int, int>();

        private static string GetMapTickOnPlaced()
        {
            return MapTickOnPlaced
                .Aggregate("", (r, i) => r + i.Key.ToString() + "," + i.Value.ToString() + "|");
        }

        public static void SetMapTickOnPlaced(string value)
        {
            //Loger.Log("setGrass " + value);
            MapTickOnPlaced = value.Split(new string[] { "|" }, StringSplitOptions.RemoveEmptyEntries)
                .Select(p => p.Split(','))
                .ToDictionary(p => int.Parse(p[0]), p => int.Parse(p[1]));
        }

        [HarmonyPrefix]
        public static void Prefix(Map __instance)
        {
            if (StorageData.GlobalData == null || !StorageData.GlobalData.GrassFixOn) return;

            var tick = Find.TickManager.TicksGame;

            //раз в день
            if (tick % 60000 == 59)
            {
                var map = __instance;
                if (map.fertilityGrid == null || map.mapTemperature == null || map.thingGrid == null || map.Tile == 0) return;

                //Loger.Log("map " + map.Tile + " " + GetMapTickOnPlaced());

                //это должна быть весна
                float latitude = (map == null) ? 0f : Find.WorldGrid.LongLatOf(map.Tile).y;
                float longitude = (map == null) ? 0f : Find.WorldGrid.LongLatOf(map.Tile).x;
                Season season = GenDate.Season((long)Find.TickManager.TicksAbs, latitude, longitude);
                if (season != Season.Spring) return;
                
                //в этом году ещё не запускали (сбрасывается при загрузке. Поэтому весной спамит каждый раз после загрузки)
                if (!MapTickOnPlaced.ContainsKey(map.Tile))
                {
                    MapTickOnPlaced.Add(map.Tile, -3600000);
                    StorageData.GameData.GrassFixData = GetMapTickOnPlaced();
                    //Loger.Log("getGrass " + StorageData.GameData.GrassFixData);
                }
                if (MapTickOnPlaced[map.Tile] + 3600000 / 2 > tick) return;

                if (map.mapTemperature.OutdoorTemp > 5f)
                {
                    MapTickOnPlaced[map.Tile] = tick;
                    StorageData.GameData.GrassFixData = GetMapTickOnPlaced();
                    //Loger.Log("getGrass " + StorageData.GameData.GrassFixData);
                    int goodPlaceNum = 0;
                    int placed = 0;
                    foreach (var pos in map.AllCells)
                    {
                        if (map.fertilityGrid.FertilityAt(pos) > 0f)
                        {
                            var ths = map.thingGrid.ThingsAt(pos);
                            if (map.thingGrid.ThingsAt(pos).Any(t => t.def == ThingDefOf.Plant_Grass)) goodPlaceNum = 0;
                            else if (map.thingGrid.ThingsAt(pos).Count() == 0)
                            {
                                goodPlaceNum++;
                                if (goodPlaceNum > 10)
                                {
                                    placed++;
                                    goodPlaceNum = 0;
                                    Plant plant = (Plant)ThingMaker.MakeThing(ThingDefOf.Plant_Grass, null);
                                    plant.Growth = 0.07f; //Rand.Range(0.07f, 1f);
                                    plant.Age = 0;
                                    GenSpawn.Spawn(plant, pos, map);
                                }
                            }
                            // GenStep_CavePlants
                        }
                    }
                    var msg = "OCity_GrassFix_Plant".Translate() + placed.ToString() + "OCity_GrassFix_Grass".Translate();
                    Loger.Log(msg);

                    Messages.Message(msg, MessageTypeDefOf.NeutralEvent);
                }
            }
        }
    }
    */
}
