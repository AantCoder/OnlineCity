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
using System.Collections.Concurrent;

namespace RimWorldOnlineCity
{
    public class ModBaseData : ModBase
    {
        //public static WorldDataStore GameData = null; //не используется

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
            //var obj = UtilityWorldObjectManager.GetUtilityWorldObject<WorldDataStore>();
            //после загрузки мира
            //GameData = obj;
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
        public SettingHandle<string> LastCash;
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

            LastCash = Settings.GetHandle<string>("ocLastCash"
               , "OCity_StorageTest_LastCash".Translate()
               , null
               , "");
            LastCash.NeverVisible = true;
            LastCash.Unsaved = false;

        }

        public static bool RunMainThreadSync(Action act, int waitSecond = 10, bool softTimeout = false)
        {
            if (GlobalData.MainThreadNum == Thread.CurrentThread.ManagedThreadId)
                act();
            else
            {
                if (softTimeout && GlobalData.MainThread.Count > 2)
                {
                    Loger.Log($"Client RunMainThread CancelRun currentReady={GlobalData.ActionNumReady} count={GlobalData.MainThread.Count} LastRunDebug={GlobalData.LastRunDebug.Ticks}", Loger.LogLevel.DEBUG);
                    return false;
                }
                //Loger.Log($"Client RunMainThreadSync begin");
                var num = RunMainThread(act);
                int i = 0;
                while (GlobalData.ActionNumReady < num && i++ < 20) Thread.Sleep(1);

                if (GlobalData.ActionNumReady < num)
                {
                    var end = DateTime.UtcNow.AddSeconds(waitSecond);
                    while (GlobalData.ActionNumReady < num && DateTime.UtcNow < end) Thread.Sleep(10);

                    if (GlobalData.ActionNumReady < num)
                    {
                        Loger.Log($"Client RunMainThread Timeout Exception num={num} currentReady={GlobalData.ActionNumReady} count={GlobalData.MainThread.Count} LastRunDebug={GlobalData.LastRunDebug.Ticks}", Loger.LogLevel.DEBUG);
                        if (!softTimeout) throw new ApplicationException("Client RunMainThread Timeout");
                        return false;
                    }
                }

                //Loger.Log($"Client RunMainThreadSync end i={i}");
            }
            return true;
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
        public long ActionNumReady = 0;

        private ConcurrentQueue<QueueActionModel> MainThread = new ConcurrentQueue<QueueActionModel>();

        private object MainThreadLock = new object();

        public int MainThreadNum = int.MinValue;

        public DateTime LastRunDebug;

        public override void Update()
        {
            LastRunDebug = DateTime.UtcNow;
            if (MainThreadNum == int.MinValue)
            {
                MainThreadNum = Thread.CurrentThread.ManagedThreadId;
            }

            lock (MainThreadLock)
            {
                while (!MainThread.IsEmpty)
                {
                    if (!MainThread.TryDequeue(out var qa)) continue;
                    try
                    {
                        qa.Act();
                    }
                    catch(Exception ext)
                    {
                        Loger.Log("Client RunMainThread Exception " + ext.ToString(), Loger.LogLevel.ERROR);
                    }
                    ActionNumReady = qa.Num;
                }
            }
        }
    }
}
