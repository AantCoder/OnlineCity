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
using UnityEngine;
using System.Reflection;
//using RimWorldOnlineCity.MapRenderer;

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
            /*
            //тестирование скриншотов
            Log.Message("DevelopTest Run");
            Loger.Log("");
            Loger.Log("DevelopTest Run ");
            var path = Path.Combine(Loger.PathLog, "MapSnapshot");
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            MapRendererMod.settings.path = path;
            //var t = GameObject.Find("GameRoot").AddComponent<RenderMap>();
            //var t = GameObject.Find("GameRoot").AddComponentExt("RenderMap");
            var t = new RenderMap();
            Loger.Log("DevelopTest w2 " + t.GetType().FullName);
            RenderMap renderMap = t as RenderMap;
            renderMap.Initialize();
            renderMap.Render();
            Loger.Log("DevelopTest End");
            return true;

            //тестирование биржы
            Log.Message("DevelopTest Run");
            Loger.Log("");
            Loger.Log("DevelopTest Run");            
            var formm = new Dialog_Exchenge(Find.Maps[0]);
            Find.WindowStack.Add(formm);
            return true;
            // */
            /*
            Log.Message("DevelopTest Run: " + UpdateWorldController.GetTestText());
            
            var hostPlace = UpdateWorldController.GetWOByServerId(7) as MapParent;
            Log.Message("DevelopTest Run: " + (hostPlace?.Label ?? "null"));
            return true;
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
            return false;
            var lll = ScenarioLister.ScenariosInCategory(ScenarioCategory.FromDef);

            File.WriteAllText(Loger.PathLog + @"ScenarioLister.txt", TextObj(lll), Encoding.UTF8);
            return true;
            */
            /*
            try
            {
                var pawns = Find.WorldPawns.AllPawnsAlive.ToList();

                //Fedon,Huntsman,Ally,Lilith,Tater,Jesse,Kentucky
                //Log.Message(string.Join(",", pawns.Select(p => p.NameStringShort).ToArray()));

                var pawn = pawns.Where(p => p.Name.ToStringShort == "Huntsman").FirstOrDefault();
                File.WriteAllText(Loger.PathLog + @"Huntsman.txt", TextObj(pawn), Encoding.UTF8);

                var msg = "";
                var map = Find.Maps[0];

                //var pawnsMy = map.mapPawns.AllPawnsSpawned.First();
                var mapPawnsA = new Pawn[map.mapPawns.AllPawnsSpawned.Count];
                map.mapPawns.AllPawnsSpawned.CopyTo(mapPawnsA);
                var pawnsMy = mapPawnsA.First();

                Thing thinXZ;
                var cell = GameUtils.GetTradeCell(map);

                /*
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
                * /
                //File.WriteAllText(Loger.PathLog + @"map.txt", TextObj(map, false), Encoding.UTF8);

                Loger.Log("DevelopTest t1");
                List<Thing> listThing = CaravanFormingUtility.AllReachableColonyItems(Find.Maps[0]);
                Loger.Log("DevelopTest t2");
                Dialog_TradeOnline form = null;
                form = new Dialog_TradeOnline(listThing, "OCity_DevTest_Test".Translate(), 3, () =>
                {
                    Loger.Log("DevelopTest t3");
                    var select = form.GetSelect();
                    Thing thin = null;
                    var thins = select.Select(p =>
                     {
                         return ThingEntry.CreateEntry(thin = p.Key, p.Value);
                     }).ToList();
                    var outText = TextObj(thins, true);
                    File.WriteAllText(Loger.PathLog + @"Car.txt", outText, Encoding.UTF8);

                    Loger.Log("DevelopTest t4");
                    var caravan = Find.WorldObjects.Caravans[0];
                    foreach (var t in select)
                    {
                        //t.Key

                        //ThingEntry the = new ThingEntry(t.Key, t.Value);
                        ///*
                        //thin = the.CreateThing();
                        //var p = CaravanInventoryUtility.FindPawnToMoveInventoryTo(thin, caravan.PawnsListForReading, null);
                        //p.inventory.innerContainer.TryAdd(thin, true);
                        //* /
                        //thin = the.CreateThing();
                        GenDrop.TryDropSpawn(thin, cell, map, ThingPlaceMode.Near, out thinXZ, null);
                    }
                    Loger.Log("DevelopTest t5");
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
            }* /
        });
        Find.WindowStack.Add(form);
        return true;
        */
            /*
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
            * /
        }
        catch(Exception e)
        {
            Log.Error(e.ToString());
        }
        */
            return true;
        }
    }

    public static class ExtensionMethod
    {
        public static Component AddComponentExt(this GameObject obj, string scriptName)
        {
            Component cmpnt = null;


            for (int i = 0; i < 10; i++)
            {
                //If call is null, make another call
                cmpnt = _AddComponentExt(obj, scriptName, i);

                //Exit if we are successful
                if (cmpnt != null)
                {
                    break;
                }
            }


            //If still null then let user know an exception
            if (cmpnt == null)
            {
                Debug.LogError("Failed to Add Component");
                return null;
            }
            return cmpnt;
        }

        private static Component _AddComponentExt(GameObject obj, string className, int trials)
        {
            //Any script created by user(you)
            const string userMadeScript = "Assembly-CSharp, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null";
            //Any script/component that comes with Unity such as "Rigidbody"
            const string builtInScript = "UnityEngine, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null";

            //Any script/component that comes with Unity such as "Image"
            const string builtInScriptUI = "UnityEngine.UI, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null";

            //Any script/component that comes with Unity such as "Networking"
            const string builtInScriptNetwork = "UnityEngine.Networking, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null";

            //Any script/component that comes with Unity such as "AnalyticsTracker"
            const string builtInScriptAnalytics = "UnityEngine.Analytics, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null";

            //Any script/component that comes with Unity such as "AnalyticsTracker"
            const string builtInScriptHoloLens = "UnityEngine.HoloLens, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null";

            Assembly asm = null;

            try
            {
                //Decide if to get user script or built-in component
                switch (trials)
                {
                    case 0:

                        asm = Assembly.Load(userMadeScript);
                        break;

                    case 1:
                        //Get UnityEngine.Component Typical component format
                        className = "UnityEngine." + className;
                        asm = Assembly.Load(builtInScript);
                        break;
                    case 2:
                        //Get UnityEngine.Component UI format
                        className = "UnityEngine.UI." + className;
                        asm = Assembly.Load(builtInScriptUI);
                        break;

                    case 3:
                        //Get UnityEngine.Component Video format
                        className = "UnityEngine.Video." + className;
                        asm = Assembly.Load(builtInScript);
                        break;

                    case 4:
                        //Get UnityEngine.Component Networking format
                        className = "UnityEngine.Networking." + className;
                        asm = Assembly.Load(builtInScriptNetwork);
                        break;
                    case 5:
                        //Get UnityEngine.Component Analytics format
                        className = "UnityEngine.Analytics." + className;
                        asm = Assembly.Load(builtInScriptAnalytics);
                        break;

                    case 6:
                        //Get UnityEngine.Component EventSystems format
                        className = "UnityEngine.EventSystems." + className;
                        asm = Assembly.Load(builtInScriptUI);
                        break;

                    case 7:
                        //Get UnityEngine.Component Audio format
                        className = "UnityEngine.Audio." + className;
                        asm = Assembly.Load(builtInScriptHoloLens);
                        break;

                    case 8:
                        //Get UnityEngine.Component SpatialMapping format
                        className = "UnityEngine.VR.WSA." + className;
                        asm = Assembly.Load(builtInScriptHoloLens);
                        break;

                    case 9:
                        //Get UnityEngine.Component AI format
                        className = "UnityEngine.AI." + className;
                        asm = Assembly.Load(builtInScript);
                        break;
                }
            }
            catch (Exception e)
            {
                //Debug.Log("Failed to Load Assembly" + e.Message);
            }

            //Return if Assembly is null
            if (asm == null)
            {
                return null;
            }

            //Get type then return if it is null
            Type type = asm.GetType(className);
            if (type == null)
                return null;

            //Finally Add component since nothing is null
            Component cmpnt = obj.AddComponent(type);
            return cmpnt;
        }
    }
}
