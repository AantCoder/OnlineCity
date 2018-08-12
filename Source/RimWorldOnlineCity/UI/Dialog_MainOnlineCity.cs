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
using HugsLib.Utils;
using OCUnion;
using Model;

namespace RimWorldOnlineCity
{
    public class Dialog_MainOnlineCity : Window
    {
        public string ScenarioToGen;
        private static Texture2D IconAddTex;
        private static Texture2D IconDelTex;
        private static Texture2D IconSubMenuTex;

        private int TabIndex = 0;
        private DateTime DataLastChatsTime;
        private ListBox<string> lbCannals;
        private int lbCannalsLastSelectedIndex;
        private float lbCannalsHeight = 0;
        private ListBox<ListBoxPlayerItem> lbPlayers;
        private string ChatText = "";
        private Vector2 ChatScrollPosition;
        private Vector2 InfoScrollPosition;
        private Vector2 AboutScrollPosition;
        private string ChatInputText = "";
        private long UpdateLogHash;
        private string lbCannalsGoToChat; //как только появиться этот чат перейти к нему

        //private string StatusTemp;
        private Vector2 scrollPosition;
        private bool NeedFockus = true;

        private string InfoTabText = "OCity_InfoTabText".Translate();

        private string InfoTabTitle = "OCity_Dialog_HelloLAN".Translate();

        private string AboutTabText = MainHelper.VersionInfo + "OCity_AboutTabText".Translate();
        public static readonly Texture2D IconForums = ContentFinder<Texture2D>.Get("UI/HeroArt/WebIcons/Forums", true);

        public class ListBoxPlayerItem
        {
            public string Login;
            public string Text;
            public string Tooltip;
            public bool InChat;
            public override string ToString()
            {
                return Text;
            }
        }

        public override Vector2 InitialSize
        {
            get { return new Vector2(600f, 500f); }
        }

        public Dialog_MainOnlineCity()
        {
            closeOnEscapeKey = true;
            doCloseButton = false;
            doCloseX = true;
            resizeable = true;
            draggable = true;
        }

        public override void PreOpen()
        {
            base.PreOpen();
            //EnsureSettingsHaveValidFiles(ClientController.Settings);
            windowRect.Set(0, 0, windowRect.width, windowRect.height);
            IconAddTex = ContentFinder<Texture2D>.Get("OCAdd");
            IconDelTex = ContentFinder<Texture2D>.Get("OCDel");
            IconSubMenuTex = ContentFinder<Texture2D>.Get("OCSubMenu");
        }

        public override void PostClose()
        {
            //ClientController.SaveSettings();
        }
        
        private bool DevTest = false;
        public override void DoWindowContents(Rect inRect)
        {
            if (!DevTest && new DevelopTest().Run())
            {
                DevTest = true;
                Close();
            }
            if (DevTest) return;

            if (!SessionClient.Get.IsLogined)
            {
                Close();
                Find.WindowStack.Add(new Dialog_LoginForm());
                return;
            }

            //Rect r1 = new Rect(inRect.x - 5f, inRect.y, 180f, 40f); // inRect.width, inRect.height);
            //Widgets.DrawBoxSolid(r1, new Color(0, 1, 1));

            var screenRect = new Rect(inRect.x, inRect.y + 31f, 400f, 0);
            var tabRect = new Rect(inRect.x, inRect.y + 31f, inRect.width, inRect.height - 31f);
            
            List<TabRecord> list = new List<TabRecord>();
            list.Add(new TabRecord("OCity_Dialog_ListChat".Translate(), () => { TabIndex = 0; }, TabIndex == 0));
            list.Add(new TabRecord("OCity_Dialog_ListInfo".Translate(), () => { TabIndex = 1; }, TabIndex == 1));
            list.Add(new TabRecord("OCity_Dialog_ListAbout".Translate(), () => { TabIndex = 2; }, TabIndex == 2));
            TabDrawer.DrawTabs(screenRect, list);
            if (TabIndex == 0) DoTab0Contents(tabRect);
            else if (TabIndex == 1) DoTab1Contents(tabRect);
            else if (TabIndex == 2) DoTab2Contents(tabRect);

            Text.Font = GameFont.Small;
            var loginRect = new Rect(inRect.width - 120f, -2f, 120f, 50f);
            Widgets.Label(loginRect, SessionClientController.Data.LastServerConnectFail
                ? "OCity_Dialog_Connecting".Translate()
                : "OCity_Dialog_Login".Translate() + SessionClientController.My.Login);
        }

        public void DoTab0Contents(Rect inRect)
        {
            var iconWidth = 25f;
            var iconWidthSpase = 30f;

            //StatusTemp = Text.CalcSize("VersionLabel").y.ToString();
            //Widgets.Label(new Rect(inRect.width - 100f, inRect.height - 30f, inRect.width, inRect.height), StatusTemp + " " + lbCannalsLastSelectedIndex.ToString());

            /// -----------------------------------------------------------------------------------------
            /// Список каналов
            /// 
            if (SessionClientController.Data.Chats != null)
            {
                //Loger.Log("Client " + SessionClientController.Data.Chats.Count);
                if (lbCannalsHeight == 0)
                {
                    var textHeight = new DialogControlBase().TextHeight;
                    lbCannalsHeight = (float)Math.Round((decimal)(inRect.height / 2f / textHeight)) * textHeight;
                }
                Widgets.Label(new Rect(inRect.x, inRect.y + iconWidthSpase + lbCannalsHeight, 100f, 22f), "OCity_Dialog_Players".Translate());

                if (lbCannals == null)
                {
                    //первый запуск
                    lbCannals = new ListBox<string>();
                    lbCannals.Area = new Rect(inRect.x
                        , inRect.y + iconWidthSpase
                        , 100f
                        , lbCannalsHeight);
                    //lbCannals.OnClick += (index, text) => StatusTemp = text;
                    lbCannals.SelectedIndex = 0;
                }
                
                if (lbPlayers == null)
                {
                    //первый запуск
                    lbPlayers = new ListBox<ListBoxPlayerItem>();
                    lbPlayers.Area = new Rect(inRect.x
                        , inRect.y + iconWidthSpase + lbCannalsHeight + 22f
                        , 100f
                        , inRect.height - (iconWidthSpase + lbCannalsHeight + 22f));
                    lbPlayers.OnClick += (index, item) =>
                    {
                        //убираем выделение
                        lbPlayers.SelectedIndex = -1;
                        //вызываем контекстное меню
                        PlayerItemMenu(item);
                    };

                    lbPlayers.Tooltip = (item) => item.Tooltip;
                }
                if (DataLastChatsTime != SessionClientController.Data.ChatsTime)
                {
                    //пишем в лог
                    var updateLogHash = SessionClientController.Data.Chats.Count * 1000000
                        + SessionClientController.Data.Chats.Sum(c => c.Posts.Count);
                    if (updateLogHash != UpdateLogHash)
                    {
                        UpdateLogHash = updateLogHash;
                        Loger.Log("Client UpdateChats count="
                            + SessionClientController.Data.Chats.Count.ToString()
                            + " posts=" + (updateLogHash % 1000000).ToString()
                            + " - " + SessionClientController.Data.ChatsTime.ToString(Loger.Culture));
                    }
                    //устанавливаем данные
                    lbCannals.DataSource = SessionClientController.Data.Chats
                        //.OrderBy(c => (c.OwnerMaker ? "2" : "1") + c.Name) нелья просто отсортировать, т.к. потом находим по индексу
                        .Select(c => c.Name)
                        .ToList();
                    if (lbCannalsGoToChat != null)
                    {
                        var lbCannalsGoToChatIndex = lbCannals.DataSource.IndexOf(lbCannalsGoToChat);
                        if (lbCannalsGoToChatIndex >= 0)
                        {
                            lbCannals.SelectedIndex = lbCannalsGoToChatIndex;
                            lbCannalsGoToChat = null;
                        }
                    }

                    var listPlayersInChat = new List<string>();
                    if (lbCannals.SelectedIndex >= 0 && SessionClientController.Data.Chats.Count > lbCannals.SelectedIndex)
                    {
                        var selectCannal = SessionClientController.Data.Chats[lbCannals.SelectedIndex];
                        listPlayersInChat = selectCannal.PartyLogin;
                        lbPlayers.DataSource = listPlayersInChat
                            .Where(p => p != "system")
                            .OrderBy(p => (selectCannal.OwnerLogin == p ? "1" : "2") + p)
                            .Select(p => new ListBoxPlayerItem()
                            {
                                Login = p,
                                Text = (selectCannal.OwnerLogin == p ? "<b>★ " + p + "</b>" : "<b>" + p + "</b>"),
                                Tooltip = p 
                                    + (selectCannal.OwnerLogin == p ? "OCity_Dialog_ChennelOwn".Translate() : "OCity_Dialog_ChennelUser".Translate()),
                                InChat = true
                            })
                            .ToList();
                    }
                    else
                    {
                        lbPlayers.DataSource = new List<ListBoxPlayerItem>();
                    }
                    var mainCannal = SessionClientController.Data.Chats[0];
                    lbPlayers.DataSource.AddRange(mainCannal.PartyLogin
                        .Where(p => !listPlayersInChat.Any(sp => sp == p))
                        .OrderBy(p => p)
                        .Select(p => new ListBoxPlayerItem()
                        {
                            Login = p,
                            Text = "<color=#888888ff>" + p + "</color>",
                            Tooltip = p,
                            InChat = false
                        })
                        );
                    
                    DataLastChatsTime = SessionClientController.Data.ChatsTime;
                    lbCannalsLastSelectedIndex = -1; //сброс для обновления содержимого окна
                }
                lbCannals.Drow();
                lbPlayers.Drow();

                var iconRect = new Rect(inRect.x, inRect.y, iconWidth, iconWidth);
                TooltipHandler.TipRegion(iconRect, "OCity_Dialog_ChennelCreate".Translate());
                if (Widgets.ButtonImage(iconRect, IconAddTex))
                {
                    CannalAdd();
                }

                if (lbCannals.SelectedIndex > 0 && SessionClientController.Data.Chats.Count > lbCannals.SelectedIndex)
                {
                    //Если что-то выделено, и это не общий чат (строка 0)
                    iconRect.x += iconWidthSpase;
                    TooltipHandler.TipRegion(iconRect, "OCity_Dialog_ChennelClose".Translate());
                    if (Widgets.ButtonImage(iconRect, IconDelTex))
                    {
                        CannalDelete();
                    }
                }

                if (lbCannals.SelectedIndex >= 0 && SessionClientController.Data.Chats.Count > lbCannals.SelectedIndex)
                {
                    iconRect.x += iconWidthSpase;
                    TooltipHandler.TipRegion(iconRect, "OCity_Dialog_OthersFunctions".Translate());
                    if (Widgets.ButtonImage(iconRect, IconSubMenuTex))
                    {
                        CannalsMenuShow();
                    }
                }

                /// -----------------------------------------------------------------------------------------
                /// Чат
                ///
                if (lbCannalsLastSelectedIndex != lbCannals.SelectedIndex)
                {
                    lbCannalsLastSelectedIndex = lbCannals.SelectedIndex;
                    if (lbCannals.SelectedIndex >= 0 && SessionClientController.Data.Chats.Count > lbCannals.SelectedIndex)
                    {
                        Func<ChatPost, string> getPost = (cp) => "[" + cp.OwnerLogin + "]: " + cp.Message;

                        var selectCannal = SessionClientController.Data.Chats[lbCannals.SelectedIndex];
                        ChatText = selectCannal.Posts
                            .Aggregate("", (r, i) => (r == "" ? "" : r + Environment.NewLine) + getPost(i));
                    }
                    else
                        ChatText = "";
                }

                if (lbCannals.SelectedIndex >= 0 && SessionClientController.Data.Chats.Count > lbCannals.SelectedIndex)
                {
                    var selectCannal = SessionClientController.Data.Chats[lbCannals.SelectedIndex];

                    //var chatTextSize = Text.CalcSize(ChatText);
                    var chatAreaOuter = new Rect(inRect.x + 110f, inRect.y, inRect.width - 110f, inRect.height - 30f);
                    var chatAreaInner = new Rect(0, 0
                        , /*inRect.width - (inRect.x + 110f) */ chatAreaOuter.width - ListBox<string>.WidthScrollLine
                        , 0/*chatTextSize.y*/);
                    chatAreaInner.height = Text.CalcHeight(ChatText, chatAreaInner.width);

                    ChatScrollPosition = GUI.BeginScrollView(chatAreaOuter, ChatScrollPosition, chatAreaInner);
                    GUILayout.BeginArea(chatAreaInner);
                    GUILayout.TextField(ChatText, "Label");
                    GUILayout.EndArea();
                    GUI.EndScrollView();

                    ///

                    if (ChatInputText != "")
                    {
                        var ev = Event.current;
                        if (ev.isKey && ev.type == EventType.keyDown && ev.keyCode == KeyCode.Return)
                        {
                            SessionClientController.Command((connect) =>
                            {
                                connect.PostingChat(selectCannal.Id, ChatInputText);
                            });
                            ChatInputText = "";
                        }
                    }

                    GUI.SetNextControlName("StartTextField");
                    ChatInputText = GUI.TextField(new Rect(inRect.x + 110f, inRect.y + inRect.height - 25f, inRect.width - 110f, 25f)
                        , ChatInputText, 10000);

                    if (NeedFockus)
                    {
                        NeedFockus = false;
                        GUI.FocusControl("StartTextField");
                    }
                }
            }
        }

        private void CannalsMenuShow()
        {
            var listMenu = new List<FloatMenuOption>();
            var myLogin = SessionClientController.My.Login;

            listMenu.Add(new FloatMenuOption("OCity_Dialog_ChennelCreate2".Translate(), CannalAdd));

            if (lbCannals.SelectedIndex > 0)
                listMenu.Add(new FloatMenuOption("OCity_Dialog_ChennelLeave".Translate(), CannalDelete));

            if (lbCannals.SelectedIndex > 0)
                listMenu.Add(new FloatMenuOption("OCity_Dialog_ChennelRen".Translate(), CannalRename));

            if (listMenu.Count == 0) return;
            var menu = new FloatMenu(listMenu);
            Find.WindowStack.Add(menu);
        }

        private void CannalAdd()
        {
            var form = new Dialog_Input("OCity_Dialog_ChennelCreating".Translate(), "OCity_Dialog_ChennelCreateName".Translate(), "");
            form.PostCloseAction = () =>
            {
                if (form.ResultOK && form.InputText != null)
                {
                    var mainCannal = SessionClientController.Data.Chats[0];
                    SessionClientController.Command((connect) =>
                    {
                        connect.PostingChat(mainCannal.Id, "/createChat '" + form.InputText.Replace("'", "''") + "'");
                    });
                    //todo Сделать старт крутяшки до обновления чата
                }
            };
            Find.WindowStack.Add(form);
        }

        private void CannalDelete()
        {
            var form = new Dialog_Input("OCity_Dialog_ChennelQuit".Translate(), "OCity_Dialog_ChennelQuitCheck".Translate());
            form.PostCloseAction = () =>
            {
                if (form.ResultOK)
                {
                    var selectCannal = SessionClientController.Data.Chats[lbCannals.SelectedIndex];
                    SessionClientController.Command((connect) =>
                    {
                        connect.PostingChat(selectCannal.Id, "/exitChat");
                    });
                    //todo Сделать старт крутяшки до обновления чата
                }
            };
            Find.WindowStack.Add(form);
        }

        private void CannalRename()
        {
            var selectCannal = SessionClientController.Data.Chats[lbCannals.SelectedIndex];
            var form = new Dialog_Input("OCity_Dialog_ChennelRen".Translate(), "OCity_Dialog_ChennelNewName".Translate() + selectCannal.Name, "");
            form.PostCloseAction = () =>
            {
                if (form.ResultOK && form.InputText != null)
                {
                    SessionClientController.Command((connect) =>
                    {
                        connect.PostingChat(selectCannal.Id, "/renameChat '" + form.InputText.Replace("'", "''") + "'");
                    });
                    //todo Сделать старт крутяшки до обновления чата
                }
            };
            Find.WindowStack.Add(form);
        }

        private void PlayerItemMenu(ListBoxPlayerItem item)
        {
            var listMenu = new List<FloatMenuOption>();
            var myLogin = SessionClientController.My.Login;
            ///Личное сообщение
            if (item.Login != myLogin)
            {
                listMenu.Add(new FloatMenuOption("OCity_Dialog_PrivateMessage".Translate(), () =>
                {
                    var privateChat = String.Compare(myLogin, item.Login) < 0
                        ? myLogin + " · " + item.Login
                        : myLogin + " · " + item.Login;
                    var index = lbCannals.DataSource.IndexOf(privateChat);
                    if (index >= 0)
                    {
                        lbCannals.SelectedIndex = index;
                        return;
                    }
                    //создаем канал
                    var mainCannal = SessionClientController.Data.Chats[0];
                    SessionClientController.Command((connect) =>
                    {
                        connect.PostingChat(mainCannal.Id, "/createChat '" + privateChat.Replace("'", "''") + "' '" + item.Login.Replace("'", "''") + "'");
                    });

                    lbCannalsGoToChat = privateChat;
                }));
            }

            ///Добавить участника
            if (lbCannals.SelectedIndex > 0 && SessionClientController.Data.Chats.Count > lbCannals.SelectedIndex)
            {
                var selectCannal = SessionClientController.Data.Chats[lbCannals.SelectedIndex];

                if (!item.InChat)
                {
                    listMenu.Add(new FloatMenuOption("OCity_Dialog_ChennelAddUser".Translate(), () =>
                    {
                        SessionClientController.Command((connect) =>
                        {
                            connect.PostingChat(selectCannal.Id, "/addPlayer '" + item.Login.Replace("'", "''") + "'");
                        });
                    }));
                }
            }

            if (SessionClientController.Data.Players.ContainsKey(item.Login))
                listMenu.Add(new FloatMenuOption("OCity_Dialog_ChennelPlayerInfo".Translate(), () =>
                {
                    var pl = SessionClientController.Data.Players[item.Login];
                    InfoTabTitle = "OCity_Dialog_ChennelPlayerInfoTitle".Translate() + item.Login;
                    InfoTabText = pl.GetTextInfo();
                    TabIndex = 1;
                }));

            if (listMenu.Count == 0) return;
            var menu = new FloatMenu(listMenu);
            Find.WindowStack.Add(menu);
        }

        public void DoTab1Contents(Rect inRect)
        {
            Text.Font = GameFont.Medium;
            Widgets.Label(inRect, InfoTabTitle);
            
            Text.Font = GameFont.Small;
            //var chatTextSize = Text.CalcSize(InfoTabText);
            var chatAreaOuter = new Rect(inRect.x + 50f, inRect.y + 40f, inRect.width - 50f, inRect.height - 30f - 40f);
            var chatAreaInner = new Rect(0, 0
                , /*inRect.width - (inRect.x + 50f)*/ chatAreaOuter.width - ListBox<string>.WidthScrollLine
                , 0/*chatTextSize.y*/);
            chatAreaInner.height = Text.CalcHeight(InfoTabText, chatAreaInner.width);

            InfoScrollPosition = GUI.BeginScrollView(chatAreaOuter, InfoScrollPosition, chatAreaInner);
            GUILayout.BeginArea(chatAreaInner);
            GUILayout.TextField(InfoTabText, "Label");
            GUILayout.EndArea();
            GUI.EndScrollView();


            /*
            Widgets.Label(inRect, "Вкладка 2");

            scrollPosition = GUI.BeginScrollView(new Rect(10, 300, 100, 100), scrollPosition, new Rect(0, 0, 220, 200));
            GUI.Button(new Rect(0, 0, 100, 20), "Top-left");
            GUI.Button(new Rect(120, 0, 100, 20), "Top-right");
            GUI.Button(new Rect(0, 180, 100, 20), "Bottom-left");
            GUI.Button(new Rect(120, 180, 100, 20), "Bottom-right");
            GUI.EndScrollView();
            */
        }

        public void DoTab2Contents(Rect inRect)
        {
            Text.Font = GameFont.Medium;
            Widgets.Label(inRect, "OCity_Dialog_AboutMode".Translate());

            Text.Font = GameFont.Small;
            //var chatTextSize = Text.CalcSize(InfoTabText);
            var chatAreaOuter = new Rect(inRect.x + 150f, inRect.y + 40f, inRect.width - 150f, inRect.height - 30f - 40f);
            var chatAreaInner = new Rect(0, 0
                , /*inRect.width - (inRect.x + 50f)*/ chatAreaOuter.width - ListBox<string>.WidthScrollLine
                , 0/*chatTextSize.y*/);
            chatAreaInner.height = Text.CalcHeight(AboutTabText, chatAreaInner.width);

            InfoScrollPosition = GUI.BeginScrollView(chatAreaOuter, InfoScrollPosition, chatAreaInner);
            GUILayout.BeginArea(chatAreaInner);
            GUILayout.TextField(AboutTabText, "Label");
            GUILayout.EndArea();
            GUI.EndScrollView();

            var rect2 = new Rect(inRect.x, inRect.y + inRect.height / 2f, 150f, inRect.height - 30f - 40f);
            Text.Font = GameFont.Small;
            List<ListableOption> list2 = new List<ListableOption>();
            ListableOption item2 = new ListableOption_WebLink("OCity_Dialog_AutorPage".Translate(), "https://vk.com/rimworldonline", IconForums);
            list2.Add(item2);
            float num = OptionListingUtility.DrawOptionListing(rect2, list2);

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
                            if (connect.PostingChat(mainCannal.Id, "/killmyallpleace"))
                            {
                                SessionClientController.Disconnected("OCity_Dialog_DeletedData".Translate());
                            }
                        });
                    }
                };
                Find.WindowStack.Add(form);
            }, IconDelTex);
            list2.Add(item2);
            num = OptionListingUtility.DrawOptionListing(rect2, list2);
            
            /*
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
            */

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
