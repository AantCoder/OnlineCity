using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;
using Transfer;
using RimWorldOnlineCity.UI;
using OCUnion;
using Model;

namespace RimWorldOnlineCity
{
    public class Dialog_MainOnlineCity : Window
    {
        public string ScenarioToGen;

        private int TabIndex = 0;

        private PanelChat panelChat;
        private PanelProfilePlayer panelProfilePlayer;


        public static string AboutGeneralText = MainHelper.VersionInfo + " "
            + "OCity_AboutTabText".Translate() + Environment.NewLine + Environment.NewLine
            + "OCity_AboutGeneralText".Translate();
        private static TextBox AboutBox = new TextBox()
        {
            Text = AboutGeneralText
        };



        public override Vector2 InitialSize
        {
            get { return LastInitialSize; }
        }

        static Dialog_MainOnlineCity IsShow = null;
        static Vector2 LastInitialSize = new Vector2(700f, 650f);
        static Vector2 LastInitialPos = new Vector2(0f, 0f);


        public Dialog_MainOnlineCity()
        {
            closeOnCancel = true;
            closeOnAccept = false;
            doCloseButton = false;
            doCloseX = true;
            resizeable = true;
            draggable = true;
            panelChat = new PanelChat();
            panelProfilePlayer = new PanelProfilePlayer();

            ChatController.MainPanelChat = panelChat;
        }

        static public void ShowHide()
        {
            if (IsShow == null)
                Find.WindowStack.Add(IsShow = new Dialog_MainOnlineCity());
            else
            {
                IsShow.Close();
                IsShow = null;
            }
        }
        static public void ShowChat()
        {
            if (IsShow == null) ShowHide();

            IsShow.TabIndex = 0;
        }

        public override void PreOpen()
        {
            base.PreOpen();
            //EnsureSettingsHaveValidFiles(ClientController.Settings);
            windowRect.Set(LastInitialPos.x, LastInitialPos.y, windowRect.width, windowRect.height);
        }

        public override void PostClose()
        {
            IsShow = null;
            //ClientController.SaveSettings();
        }

        private bool DevTest = false;
        public override void DoWindowContents(Rect inRect)
        {
            try
            {
                LastInitialSize = new Vector2(windowRect.width, windowRect.height);
                LastInitialPos = new Vector2(windowRect.x, windowRect.y);

                if (MainHelper.DebugMode)
                {
                    if (!DevTest && new DevelopTest().Run())
                    {
                        DevTest = true;
                        Close();
                    }
                    if (DevTest) return;
                }

                if (!SessionClient.Get.IsLogined)
                {
                    Close();
                    SessionClientController.Disconnected(null);
                    //Find.WindowStack.Add(new Dialog_LoginForm());
                    return;
                }

                //Rect r1 = new Rect(inRect.x - 5f, inRect.y, 180f, 40f); // inRect.width, inRect.height);
                //Widgets.DrawBoxSolid(r1, new Color(0, 1, 1));

                var screenRect = new Rect(inRect.x, inRect.y + 31f, 400f, 0);
                var tabRect = new Rect(inRect.x, inRect.y + 31f, inRect.width, inRect.height - 31f);

                List<TabRecord> list = new List<TabRecord>();
                list.Add(new TabRecord("OCity_Dialog_ListChat".Translate(), () => { TabIndex = 0; }, TabIndex == 0));
                //list.Add(new TabRecord("OCity_Dialog_ListInfo".Translate(), () => { TabIndex = 1; }, TabIndex == 1));
                list.Add(new TabRecord("OCity_Dialog_Settings".Translate(), () => { TabIndex = 2; }, TabIndex == 2));
                list.Add(new TabRecord("OCity_Dialog_ListAbout".Translate(), () => { TabIndex = 3; }, TabIndex == 3));
                TabDrawer.DrawTabs(screenRect, list);
                if (TabIndex == 0) DoTab0Contents(tabRect);
                //else if (TabIndex == 1) DoTab1Contents(tabRect);
                else if (TabIndex == 2) DoTab2Contents(tabRect);
                else if (TabIndex == 3) DoTab3Contents(tabRect);

                Text.Font = GameFont.Small;
                var loginRect = new Rect(inRect.width - 180f, -2f, 180f, 50f);

                Widgets.Label(loginRect, SessionClientController.Data.LastServerConnectFail
                    ? "OCity_Dialog_Connecting".Translate()
                    : "OCity_Dialog_Login".Translate() + SessionClientController.My.Login
                        + new TaggedString(" ") + (int)SessionClientController.Data.Ping.TotalMilliseconds + new TaggedString("ms"));
            }
            catch (Exception e)
            {
                Loger.Log("Dialog_MainOnlineCity Exception: " + e.Message + Environment.NewLine + e.ToString(), Loger.LogLevel.ERROR);
            }
        }

        public void DoTab0Contents(Rect inRect)
        {
            panelChat.Drow(inRect);
        }

        public void DoTab1Contents(Rect inRect)
        {
        }

        public void DoTab2Contents(Rect inRect)
        {
            panelProfilePlayer.Drow(inRect);
        }

        public void DoTab3Contents(Rect inRect)
        {
            Text.Font = GameFont.Medium;
            Widgets.Label(inRect, "OCity_Dialog_AboutMode".Translate());

            Text.Font = GameFont.Small;
            var chatAreaOuter = new Rect(inRect.x + 150f, inRect.y + 40f, inRect.width - 150f, inRect.height - 30f - 40f);
            AboutBox.Drow(chatAreaOuter);

            var rect2 = new Rect(inRect.x, inRect.y + inRect.height / 2f, 150f, inRect.height - 30f - 40f);
            Text.Font = GameFont.Small;
            List<ListableOption> list2 = new List<ListableOption>();
            ListableOption item2 = new ListableOption_WebLink("OCity_Dialog_AutorPage".Translate(), "https://steamcommunity.com/sharedfiles/filedetails/?id=1908437382", GeneralTexture.IconForums);
            list2.Add(item2);

            rect2 = new Rect(inRect.x, inRect.y + 30f, 150f, 40f);
            Text.Font = GameFont.Small;
            list2 = new List<ListableOption>();
            item2 = new ListableOption_WebLink("OCity_Dialog_Regame".Translate(), () =>
            {
                var form = new Dialog_Input("OCity_Dialog_DeleteData".Translate(), "OCity_Dialog_DeleteDataCheck".Translate());
                form.PostCloseAction = () =>
                {
                    if (form.ResultOK)
                    {
                        var mainCannal = SessionClientController.Data.Chats[0];
                        SessionClientController.Command((connect) =>
                        {
                            var res = connect.PostingChat(mainCannal.Id, "/killmyallplease");
                            if (res != null && res.Status == 0)
                            {
                                SessionClientController.Disconnected("OCity_Dialog_DeletedData".Translate());
                            }
                        });
                    }
                };
                Find.WindowStack.Add(form);
            }, GeneralTexture.IconDelTex);
            list2.Add(item2);

            float num = OptionListingUtility.DrawOptionListing(rect2, list2);
            GUI.BeginGroup(rect2);
            if (Current.ProgramState == ProgramState.Entry && Widgets.ButtonImage(new Rect(0f, num + 10f, 64f, 32f), LanguageDatabase.activeLanguage.icon))
            {
                List<FloatMenuOption> list3 = new List<FloatMenuOption>();
                foreach (LoadedLanguage current in LanguageDatabase.AllLoadedLanguages)
                {
                    LoadedLanguage localLang = current;
                    list3.Add(new FloatMenuOption(localLang.FriendlyNameNative, delegate
                    {
                        LanguageDatabase.SelectLanguage(localLang);
                        Prefs.Save();
                    }, MenuOptionPriority.Default, null, null, 0f, null, null));
                }
                Find.WindowStack.Add(new FloatMenu(list3));
            }
            GUI.EndGroup();

            /*
            var rectCannals = new Rect(inRect.x, inRect.y, 100f, (float)Math.Round((decimal)(inRect.height / 2f * 10f)) / 10f);
            Widgets.DrawBoxSolid(inRect, new Color(0.2f, 0.2f, 0));
            Widgets.DrawBoxSolid(rectCannals, new Color(0.4f, 0.4f, 0));


            Widgets.DrawBoxSolid(new Rect(inRect.x + 110f, inRect.y, inRect.width - 110f, inRect.height - 40f)
                , new Color(0.4f, 0, 0));

            Widgets.DrawBoxSolid(new Rect(inRect.x + 110f, inRect.y + inRect.height - 35f, inRect.width - 110f, 25f)
                , new Color(0, 0, 0.4f));

            Widgets.Label(inRect, "Вкладка 3");
            */
        }
        

    }
}
