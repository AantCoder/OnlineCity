using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using RimWorld.BaseGen;
using RimWorld.Planet;
using HugsLib;
using HugsLib.Utils;
using Verse;
using HugsLib.Quickstart;
using HugsLib.Settings;

namespace RimWorldOnlineCity
{
    public class StorageData : ModBase
    {
        public static WorldDataStore GameData = null;

        public static StorageData GlobalData = null;

        public StorageData()
            :base()
        {
            GlobalData = this;
        }

        public override string ModIdentifier
        {
            get { return "OnlineCity"; }
        }

        public override void WorldLoaded()
        {
            var obj = UtilityWorldObjectManager.GetUtilityWorldObject<WorldDataStore>();
            //после загрузки мира
            GameData = obj;
            GrassFix.SetMapTickOnPlaced(GameData.GrassFixData ?? "");
        }
        
        public class WorldDataStore : UtilityWorldObject
        {
            public string GrassFixData;

            public override void PostAdd()
            {
                base.PostAdd();
                //инициализируется первый раз в игре
                GrassFixData = "";
            }

            public override void ExposeData()
            {
                base.ExposeData();
                //в файл сохранения игры
                Scribe_Values.Look(ref GrassFixData, "ocGrassFixData", "");
            }
        }
        
        public SettingHandle<bool> GrassFixOn;
        public SettingHandle<string> LastIP;

        public override void DefsLoaded()
        {
            GrassFixOn = Settings.GetHandle<bool>("ocGrassFixOn"
                , "OCity_StorageTest_CreateGrass".Translate()
                , "OCity_StorageTest_CreateGrassDescrip".Translate()
                , false);
            LastIP = Settings.GetHandle<string>("ocLastIP"
                , "OCity_StorageTest_LastIP".Translate()
                , null
                , "");
            LastIP.NeverVisible = true;
            LastIP.Unsaved = false;

        }
    }
}
