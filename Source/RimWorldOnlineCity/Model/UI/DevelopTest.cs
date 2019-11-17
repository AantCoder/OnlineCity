using OCUnion;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Verse;
using Verse.AI;
using RimWorld;
using RimWorld.Planet;
using Model;
using RimWorldOnlineCity.UI;
using HugsLib.Utils;

namespace RimWorldOnlineCity
{

    public class DevelopTest
    {
        private static HashSet<string> ExcludeTypes = new HashSet<string>()
        {
            "Faction", //фракция
            "Filth", //какой-то набор вещей с карты
            "ThoughtHandler", 
            "Pawn_WorkSettings", //расписание

            "WorldObjectDef",
            "ThingDef",
            "PawnKindDef",
            "NeedDef",
            "HediffDef",
            "ThinkTreeDef",
            "MentalStateDef",
            "SoundDef",
            //"JobDef",
            "SkillDef",
            "HairDef",
            "TraitDef",
            "TimeAssignmentDef",
            "FactionDef",
            "ThingCategoryDef",
            "SpecialThingFilterDef",
            "PrisonerInteractionModeDef",
            "PawnGroupKindDef",
            "RaidStrategyDef",
            //"RulePackDef",
            "WorkGiverDef",
        };
            
        public static string TextObj(object o, bool detalic = false)
        {
            return detalic 
                ? Restricted.ToStringRestricted(o, ExcludeTypes)
                : Restricted.ToStringRestrictedShort(o, ExcludeTypes);
        }

        private static int tttt;

        public bool Run()
        {
            return false;
            Log.Message("DevelopTest Run: " + UpdateWorldController.GetTestText());
            
            var hostPlace = UpdateWorldController.GetWOByServerId(7) as MapParent;
            Log.Message("DevelopTest Run: " + (hostPlace?.Label ?? "null"));
            return true;
            /*
            var mmap = Find.Maps[0];
            var ppawn = mmap.mapPawns.AllPawnsSpawned.FirstOrDefault(i => i.thingIDNumber == 589);
            var tpawn = mmap.mapPawns.AllPawnsSpawned.FirstOrDefault(i => i.thingIDNumber == 586);

            var pxml = ThingEntry.CreateEntry(ppawn, 1);
            File.WriteAllText(Loger.PathLog + "RunTest" + (++tttt).ToString() + ".xml", pxml.Data);

            var place = Find.WorldObjects.Settlements
                .FirstOrDefault(f => f.Faction == Faction.OfPlayer);
            var map_ = ((Settlement)place).Map;
            var cell_ = GameUtils.GetTradeCell(map_);

            map_.terrainGrid.SetTerrain(cell_
                , (tttt++) % 2 == 1 
                ? DefDatabase<TerrainDef>.GetNamed("TileGranite")
                : TerrainDefOf.WaterDeep);

            return true;
            */
            /////////////////////////////////////////////////////
            /*
            GameUtils.ShowDialodOKCancel("asdasda",
                () => Log.Message("Test OK"),
                () => Log.Message("Test Cancel")
                );
            return true;
            */
            /*
            var formm = new Dialog_Exchenge(Find.Maps[0]);
            Find.WindowStack.Add(formm);
            return true;
            // */
            return false;
            /*
            var lll = ScenarioLister.ScenariosInCategory(ScenarioCategory.FromDef);

            File.WriteAllText(Loger.PathLog + @"ScenarioLister.txt", TextObj(lll), Encoding.UTF8);
            return true;
            */
            try
            {
                Log.Message("DevelopTest Run");
                Loger.Log("");
                Loger.Log("DevelopTest Run");

                var pawns = Find.WorldPawns.AllPawnsAlive.ToList();

                //Fedon,Huntsman,Ally,Lilith,Tater,Jesse,Kentucky
                //Log.Message(string.Join(",", pawns.Select(p => p.NameStringShort).ToArray()));

                var pawn = pawns.Where(p => p.Name.ToStringShort == "Huntsman").FirstOrDefault();
                File.WriteAllText(Loger.PathLog + @"Huntsman.txt", TextObj(pawn), Encoding.UTF8);

                var msg = "";
                var map = Find.Maps[0];
                var pawnsMy = map.mapPawns.AllPawnsSpawned.First();
                Thing thinXZ;
                var cell = GameUtils.GetTradeCell(map);

                var gx = new GameXMLUtils();
                //var testPawn = Scribe.saver.DebugOutputFor(pawns[0]);
                var testPawn = gx.ToXml(pawnsMy);
                File.WriteAllText(@"c:\World\testPawn.xml", testPawn);

                gx.StartFromXml(@"c:\World\test.xml");
                var thin0 = gx.Test<Thing>();
                //PawnComponentsUtility.CreateInitialComponents((Pawn)thin0);
                gx.Finish();

                thin0.ThingID += "555";
                if (thin0 is Pawn)
                {
                    var refugee = (Pawn)thin0;
                    GenSpawn.Spawn(refugee, cell, map);
                    //refugee.SetFaction(Faction.OfPlayer, null);
                    refugee.PostMapInit();  //?
                    //map.mapPawns.UpdateRegistryForPawn((Pawn)thin0);
                }
                else
                    GenDrop.TryDropSpawn(thin0, cell, map, ThingPlaceMode.Near, out thinXZ, null);


                //MapComponentUtility.FinalizeInit(map); //????
                return true;

                File.WriteAllText(Loger.PathLog + @"map.txt", TextObj(map, false), Encoding.UTF8);
                
                List<Thing> listThing = CaravanFormingUtility.AllReachableColonyItems(Find.Maps[0]);
                Dialog_TradeOnline form = null;
                form = new Dialog_TradeOnline(listThing, "OCity_DevTest_Test".Translate(), 3, () =>
                {
                    var select = form.GetSelect();
                    Thing thin = null;
                    var thins = select.Select(p =>
                     {
                         return ThingEntry.CreateEntry(thin = p.Key, p.Value);
                     }).ToList();
                    var outText = TextObj(thins, true);
                    File.WriteAllText(Loger.PathLog + @"Car.txt", outText, Encoding.UTF8);
                    
                    var caravan = Find.WorldObjects.Caravans[0];
                    foreach (var t in select)
                    {
                        //t.Key

                        //ThingEntry the = new ThingEntry(t.Key, t.Value);
                        ///*
                        //thin = the.CreateThing();
                        //var p = CaravanInventoryUtility.FindPawnToMoveInventoryTo(thin, caravan.PawnsListForReading, null);
                        //p.inventory.innerContainer.TryAdd(thin, true);
                        //*/
                        //thin = the.CreateThing();
                        GenDrop.TryDropSpawn(thin, cell, map, ThingPlaceMode.Near, out thinXZ, null);
                    }
                    /*
                    File.WriteAllText(Loger.PathLog + @"ThingIn.txt", TextObj(thin, true), Encoding.UTF8);
                    //if (thin.Spawned) thin.DeSpawn();
                    ThingEntry the = new ThingEntry(thin, 1);
                    thin = the.CreateThing();
                    GenDrop.TryDropSpawn(thin, GameUtils.GetTradeCell(map), map, ThingPlaceMode.Near, out thinXZ, null);
                    File.WriteAllText(Loger.PathLog + @"ThingXZ.txt", TextObj(thinXZ, true), Encoding.UTF8);
                    File.WriteAllText(Loger.PathLog + @"ThingOut.txt", TextObj(thin, true), Encoding.UTF8);
                    */
                    /*
                    if (thin != null)
                    {
                        File.WriteAllText(Loger.PathLog + @"ThingIn.txt", TextObj(thin, true), Encoding.UTF8);
                        ThingEntry the = new ThingEntry(thin, 1);
                        File.WriteAllText(Loger.PathLog + @"ThingEntry.txt", TextObj(the, true), Encoding.UTF8);
                        File.WriteAllText(Loger.PathLog + @"ThingOut.txt", TextObj(the.CreateThing(), true), Encoding.UTF8);
                    }*/
                });
                Find.WindowStack.Add(form);
                return true;
                
                pawn = pawns.Where(p => p.Name.ToStringShort == "Jesse").FirstOrDefault();

                //msg += Find.Maps.Count.ToString() + Environment.NewLine;


                var pawnText = TextObj(pawn, true);
                File.WriteAllText(Loger.PathLog + @"Car.txt", pawnText, Encoding.UTF8);

                int directionTile = CaravanExitMapUtility.RandomBestExitTileFrom(Find.Maps[0]);
                    //Find.Maps[0].Tile;

                //var destroyedFactionBase = (CaravanOnline)WorldObjectMaker.MakeWorldObject(ModDefOf.CaravanOnline);
                var destroyedFactionBase = (CaravanOnline)WorldObjectMaker.MakeWorldObject(ModDefOf.BaseOnline);
                destroyedFactionBase.Tile = directionTile;
                destroyedFactionBase.OnlineWObject = new Model.WorldObjectEntry() { LoginOwner = "OCity_DevTest_NameTestPlayer".Translate() };
                destroyedFactionBase.SetFaction(Faction.OfPlayer);
                Find.WorldObjects.Add(destroyedFactionBase);

                var cars = Find.WorldObjects.AllWorldObjects.Where(o => o is Caravan).ToList();
                var seeText = TextObj(cars);
                File.WriteAllText(Loger.PathLog + @"See.txt", seeText, Encoding.UTF8);

                Loger.Log(msg);
                
            }
            catch(Exception e)
            {
                Log.Error(e.ToString());
            }
            return true;
        }
    }
}
