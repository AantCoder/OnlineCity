using Harmony;
using HugsLib.Utils;
using OCUnion;
using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using Transfer;
using UnityEngine;
using Verse;

namespace RimWorldOnlineCity
{
    public class MainMenu
    {
        public static void OnMainMenuNetClick()
        {
            Find.WindowStack.Add(new Dialog_LoginForm());
        }

        public static float DrawOptionListing(Rect rect, List<ListableOption> optList)
        {
            if (optList.Count > 0 && optList[0].GetType() == typeof(ListableOption))
            {
                if (Current.ProgramState == ProgramState.Entry)
                {
                    var item = new ListableOption("OCity_LAN_btn".Translate(), delegate
                        {
                            OnMainMenuNetClick();
                        }, null);
                    optList.Insert(0, item);
                }
            }
            return OptionListingUtility.DrawOptionListing(rect, optList);
        }
    }

    //Пункт в главном меню
    [HarmonyPatch(typeof(OptionListingUtility))]
    [HarmonyPatch("DrawOptionListing")]
    [HarmonyPatch(new[] { typeof(Rect), typeof(List<ListableOption>)})]
    internal class MainMenuDrawer_DoMainMenuControls_Patch
    {
        public static bool Inited = false;

        //Инициализация игры
        [HarmonyPrefix]
        public static void Prefix(Rect rect, List<ListableOption> optList)
        {

            //File.WriteAllText(Loger.PathLog + @"optList.txt", DevelopTest.TextObj(optList), Encoding.UTF8);
            if (optList.Count > 0 && optList[0].GetType() == typeof(ListableOption))
            {
                if (Current.ProgramState == ProgramState.Entry)
                {
                    var item = new ListableOption("OCity_LAN_btn".Translate(), delegate
                    {
                        MainMenu.OnMainMenuNetClick();
                    }, null);
                    optList.Insert(0, item);
                }
            }

            if (Inited) return;
            Inited = true;
            SessionClientController.Init();
        }
    }
   
}
