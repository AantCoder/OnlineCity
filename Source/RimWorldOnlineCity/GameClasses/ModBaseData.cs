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
    public class ModBaseData : ModBase
    {
        public static WorldDataStore GameData = null;

        public static ModBaseData GlobalData = null;

        public ModBaseData()
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
        }
        
        public class WorldDataStore : UtilityWorldObject
        {

            public override void PostAdd()
            {
                base.PostAdd();
                //инициализируется первый раз в игре
            }

            public override void ExposeData()
            {
                base.ExposeData();
                //в файл сохранения игры
                //Scribe_Values.Look(ref GrassFixData, "ocGrassFixData", "");
            }
        }
        
        public SettingHandle<string> LastIP;
        public SettingHandle<string> LastLoginName;
        public override void DefsLoaded()
        {
            LastIP = Settings.GetHandle<string>("ocLastIP"
                , "OCity_StorageTest_LastIP".Translate()
                , null
                , "");
            LastIP.NeverVisible = true;
            LastIP.Unsaved = false;
            LastLoginName = Settings.GetHandle<string>("ocLastLoginName"
               , "OCity_StorageTest_LoginName".Translate()
               , null
               , "");
            LastLoginName.NeverVisible = true;
            LastLoginName.Unsaved = false;

        }

        public void RunMainThread(Action act)
        {
            lock (MainThreadLock)
            {
                MainThread.Enqueue(act);
            }
        }

        private Queue<Action> MainThread = new Queue<Action>();

        private object MainThreadLock = new object();

        public override void Update()
        {
            lock (MainThreadLock)
            {
                while (MainThread.Count > 0)
                {
                    MainThread.Dequeue()();
                }
            }
        }
    }
}
