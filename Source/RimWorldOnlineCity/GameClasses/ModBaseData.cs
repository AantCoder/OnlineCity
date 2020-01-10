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
using System.Threading;
using OCUnion;

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

        public static void RunMainThreadSync(Action act)
        {
            /*
            if (GlobalData.MainThreadNum == Thread.CurrentThread.ManagedThreadId)
                act();
            else
            */
            {
                var num = RunMainThread(act);
                int i = 0;
                while (GlobalData.ActionNumReady < num && i++ < 20) Thread.Sleep(1);

                if (GlobalData.ActionNumReady < num)
                {
                    var end = DateTime.UtcNow.AddSeconds(10);
                    while (GlobalData.ActionNumReady < num && DateTime.UtcNow < end) Thread.Sleep(10);

                    if (GlobalData.ActionNumReady < num)
                    {
                        Loger.Log("Client RunMainThread Timeout Exception");
                        throw new ApplicationException("Client RunMainThread Timeout");
                    }
                }

                Loger.Log($"Client RunMainThreadSync end i={i}");
            }
        }
        
        public static long RunMainThread(Action act)
        {
            lock (GlobalData.MainThreadLock)
            {
                var qa = new QueueActionModel()
                {
                    Num = ++GlobalData.ActionNumNext,
                    Act = act
                };
                GlobalData.MainThread.Enqueue(qa);
                return qa.Num;
            }
        }

        private class QueueActionModel
        {
            public Action Act;
            public long Num;
        }

        private long ActionNumNext = 0;
        private long ActionNumReady = 0;

        private Queue<QueueActionModel> MainThread = new Queue<QueueActionModel>();

        private object MainThreadLock = new object();

        public int MainThreadNum = int.MinValue;

        public override void Update()
        {
            if (MainThreadNum == int.MinValue)
            {
                MainThreadNum = Thread.CurrentThread.ManagedThreadId;
            }

            lock (MainThreadLock)
            {
                while (MainThread.Count > 0)
                {
                    var qa = MainThread.Dequeue();
                    try
                    {
                        qa.Act();
                    }
                    catch(Exception ext)
                    {
                        Loger.Log("Client RunMainThread Exception " + ext.ToString());
                    }
                    ActionNumReady = qa.Num;
                }
            }
        }
    }
}
