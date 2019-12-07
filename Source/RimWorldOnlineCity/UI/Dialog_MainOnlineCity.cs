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
    [StaticConstructorOnStartup]
    public class Dialog_MainOnlineCity : Window
    {
        public string ScenarioToGen;
        private static Texture2D IconAddTex;
        private static Texture2D IconDelTex;
        private static Texture2D IconSubMenuTex;

        private int TabIndex = 0;
        private DateTime DataLastChatsTime; //время полученного пакета данных чата от сервера, которое выведено в интерфейс
        private DateTime DataLastChatsTimeUpdateTime; //когда последний раз обновлялась информация в интерфейсе (перерисовывать раз в 5 сек)
        private ListBox<string> lbCannals;
        private int lbCannalsLastSelectedIndex;
        private float lbCannalsHeight = 0;
        private ListBox<ListBoxPlayerItem> lbPlayers;

        private TextBox ChatBox = new TextBox();
        //private string ChatText = "";
        //private Vector2 ChatScrollPosition;
        private bool ChatScrollToDown = false;
        private DateTime ChatLastPostTime;
        private string ChatInputText = "";

        private string InfoTabTitle = "OCity_Dialog_HelloLAN".Translate();
        private TextBox InfoBox = new TextBox()
        {
            Text = "OCity_InfoTabText".Translate()
        };
        //private Vector2 InfoScrollPosition;

        private long UpdateLogHash;
        private string lbCannalsGoToChat; //как только появиться этот чат перейти к нему

        private bool NeedFockus = true;

        public static string AboutGeneralText = MainHelper.VersionInfo + " "
            + "OCity_AboutTabText".Translate() + Environment.NewLine + Environment.NewLine
            + "OCity_AboutGeneralText".Translate();
        private static TextBox AboutBox = new TextBox()
        {
            Text = AboutGeneralText
        };

        public static readonly Texture2D IconForums;

        public class ListBoxPlayerItem
        {
            public string Login;
            public string Text;
            public string Tooltip;
            public bool InChat;
            public bool GroupTitle;
            public override string ToString()
            {
                return Text;
            }
        }

        public override Vector2 InitialSize
        {
            get { return LastInitialSize; }
        }

        static Dialog_MainOnlineCity IsShow = null;
        static Vector2 LastInitialSize = new Vector2(700f, 650f);
        static Vector2 LastInitialPos = new Vector2(0f, 0f);

        static Dialog_MainOnlineCity()
        {
            IconAddTex = ContentFinder<Texture2D>.Get("OCAdd");
            IconDelTex = ContentFinder<Texture2D>.Get("OCDel");
            IconSubMenuTex = ContentFinder<Texture2D>.Get("OCSubMenu");
            IconForums = ContentFinder<Texture2D>.Get("UI/HeroArt/WebIcons/Forums", true);
        }

        public Dialog_MainOnlineCity()
        {
            closeOnCancel = true;
            closeOnAccept = false;
            doCloseButton = false;
            doCloseX = true;
            resizeable = true;
            draggable = true;
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
                var loginRect = new Rect(inRect.width - 180f, -2f, 180f, 50f);
                Widgets.Label(loginRect, SessionClientController.Data.LastServerConnectFail
                    ? "OCity_Dialog_Connecting".Translate()
                    : "OCity_Dialog_Login".Translate() + SessionClientController.My.Login
                        + " " + (int)SessionClientController.Data.Ping.TotalMilliseconds + "ms");
            }
            catch (Exception e)
            {
                Loger.Log("Dialog_MainOnlineCity Exception: " + e.Message + Environment.NewLine + e.ToString());
            }
        }

        private float DoTab0LastHeight = 0;
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
                if (SessionClientController.Data.ChatNotReadPost > 0) SessionClientController.Data.ChatNotReadPost = 0;

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
                    lbCannals.OnClick += (index, text) => DataLastChatsTime = DateTime.MinValue; /*StatusTemp = text;*/
                    lbCannals.SelectedIndex = 0;
                }

                if (lbPlayers == null)
                {
                    //первый запуск
                    lbPlayers = new ListBox<ListBoxPlayerItem>();
                    /*
                    lbPlayers.Area = new Rect(inRect.x
                        , inRect.y + iconWidthSpase + lbCannalsHeight + 22f
                        , 100f
                        , inRect.height - (iconWidthSpase + lbCannalsHeight + 22f));
                    */
                    lbPlayers.OnClick += (index, item) =>
                    {
                        //убираем выделение
                        lbPlayers.SelectedIndex = -1;
                        //вызываем контекстное меню
                        PlayerItemMenu(item);
                    };

                    lbPlayers.Tooltip = (item) => item.Tooltip;
                }
                if (DoTab0LastHeight != inRect.height)
                {
                    DoTab0LastHeight = inRect.height;
                    lbPlayers.Area = new Rect(inRect.x
                        , inRect.y + iconWidthSpase + lbCannalsHeight + 22f
                        , 100f
                        , inRect.height - (iconWidthSpase + lbCannalsHeight + 22f));
                }

                if (DataLastChatsTime != SessionClientController.Data.ChatsTime
                    || DataLastChatsTimeUpdateTime < DateTime.UtcNow.AddSeconds(-5))
                {
                    DataLastChatsTimeUpdateTime = DateTime.UtcNow;
                    //пишем в лог
                    var updateLogHash = SessionClientController.Data.Chats.Count * 1000000
                        + SessionClientController.Data.Chats.Sum(c => c.Posts.Count);
                    if (updateLogHash != UpdateLogHash)
                    {
                        UpdateLogHash = updateLogHash;
                        Loger.Log("Client UpdateChats chats="
                            + SessionClientController.Data.Chats.Count.ToString()
                            + " players=" + SessionClientController.Data.Players.Count.ToString());
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

                    //Заполняем список игроков по группами {
                    lbPlayers.DataSource = new List<ListBoxPlayerItem>();
                    var allreadyLogin = new List<string>();
                    Func<string, string, ListBoxPlayerItem> addPl = (login, text) =>
                    {
                        allreadyLogin.Add(login);
                        var n = new ListBoxPlayerItem()
                        {
                            Login = login,
                            Text = text,
                            Tooltip = login
                        };
                        lbPlayers.DataSource.Add(n);
                        return n;
                    };
                    Action<string> addTit = (text) =>
                    {
                        if (lbPlayers.DataSource.Count > 0) addPl(null, " ").GroupTitle = true;
                        addPl(null, " <i>– " + text + " –</i> ").GroupTitle = true;
                    };
                    Func<string, bool> isOnline = (login) => login == SessionClientController.My.Login
                        || SessionClientController.Data.Players.ContainsKey(login) && SessionClientController.Data.Players[login].Online;
                    Func<bool, string, string> frameOnline = (online, txt) =>
                        online
                        ? "<b>" + txt + "</b>"
                        : "<color=#888888ff>" + txt + "</color>";

                    if (lbCannals.SelectedIndex > 0 && SessionClientController.Data.Chats.Count > lbCannals.SelectedIndex)
                    {
                        var selectCannal = SessionClientController.Data.Chats[lbCannals.SelectedIndex];

                        // в чате создатель
                        addTit("в чате".NeedTranslate());
                        var n = addPl(selectCannal.OwnerLogin
                            , frameOnline(isOnline(selectCannal.OwnerLogin), "★ " + selectCannal.OwnerLogin));
                        n.Tooltip += "OCity_Dialog_ChennelOwn".Translate();
                        n.InChat = true;

                        // в чате 
                        var offlinePartyLogin = new List<string>();
                        for (int i = 0; i < selectCannal.PartyLogin.Count; i++)
                        {
                            var lo = selectCannal.PartyLogin[i];
                            if (lo != "system" && lo != selectCannal.OwnerLogin)
                            {
                                if (isOnline(lo))
                                {
                                    n = addPl(lo, frameOnline(true, lo));
                                    n.Tooltip += "OCity_Dialog_ChennelUser".Translate();
                                    n.InChat = true;
                                }
                                else
                                    offlinePartyLogin.Add(lo);
                            }
                        }

                        // в чате оффлайн
                        //addTit("оффлайн".NeedTranslate());
                        for (int i = 0; i < offlinePartyLogin.Count; i++)
                        {
                            var lo = offlinePartyLogin[i];
                            n = addPl(lo, frameOnline(false, lo));
                            n.Tooltip += "OCity_Dialog_ChennelUser".Translate();
                            n.InChat = true;
                        }
                    }

                    var other = SessionClientController.Data.Chats[0].PartyLogin
                        .Where(p => p != "" && p != "system" && !allreadyLogin.Any(al => al == p))
                        .ToList();
                    if (other.Count > 0)
                    {
                        // игроки
                        addTit("игроки".NeedTranslate());
                        var offlinePartyLogin = new List<string>();
                        for (int i = 0; i < other.Count; i++)
                        {
                            var lo = other[i];
                            if (isOnline(lo))
                            {
                                var n = addPl(lo, frameOnline(true, lo));
                                //n.Tooltip += "OCity_Dialog_ChennelUser".Translate();
                            }
                            else
                                offlinePartyLogin.Add(lo);
                        }

                        // игроки оффлайн
                        //addTit("оффлайн".NeedTranslate());
                        for (int i = 0; i < offlinePartyLogin.Count; i++)
                        {
                            var lo = offlinePartyLogin[i];
                            var n = addPl(lo, frameOnline(false, lo));
                            //n.Tooltip += "OCity_Dialog_ChennelUser".Translate();
                        }

                    }
                    DataLastChatsTime = SessionClientController.Data.ChatsTime;
                    lbCannalsLastSelectedIndex = -1; //сброс для обновления содержимого окна
                }
                // }

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
                        var selectCannal = SessionClientController.Data.Chats[lbCannals.SelectedIndex];
                        if (selectCannal.Posts != null || selectCannal.Posts.Count > 0)
                        {
                            var chatLastPostTime = selectCannal.Posts.Max(p => p.Time);
                            if (ChatLastPostTime != chatLastPostTime)
                            {
                                ChatLastPostTime = chatLastPostTime;
                                Func<ChatPost, string> getPost = (cp) => "[" + cp.Time.ToGoodUtcString("dd HH:mm ") + cp.OwnerLogin + "]: " + cp.Message;

                                var totalLength = 0;
                                ChatBox.Text = selectCannal.Posts
                                    .Reverse<ChatPost>()
                                    .Where(i => (totalLength += i.Message.Length) < 5000)
                                    .Aggregate("", (r, i) => getPost(i) + (r == "" ? "" : Environment.NewLine + r));
                                ChatScrollToDown = true;
                            }
                            else
                                ChatBox.Text = "";
                        }
                        else
                            ChatBox.Text = "";
                    }
                    else
                        ChatBox.Text = "";
                }

                if (lbCannals.SelectedIndex >= 0 && SessionClientController.Data.Chats.Count > lbCannals.SelectedIndex)
                {
                    var selectCannal = SessionClientController.Data.Chats[lbCannals.SelectedIndex];
                    var chatAreaOuter = new Rect(inRect.x + 110f, inRect.y, inRect.width - 110f, inRect.height - 30f);
                    ChatBox.Drow(chatAreaOuter, ChatScrollToDown);
                    ChatScrollToDown = false;

                    var rrect = new Rect(inRect.x + inRect.width - 25f, inRect.y + inRect.height - 25f, 25f, 25f);
                    Text.Font = GameFont.Medium;
                    Text.Anchor = TextAnchor.MiddleCenter;
                    Widgets.Label(rrect, "▶");
                    Text.Font = GameFont.Small;
                    Text.Anchor = TextAnchor.MiddleLeft;
                    bool rrcklick = Widgets.ButtonInvisible(rrect);

                    if (ChatInputText != "")
                    {
                        if (Mouse.IsOver(rrect))
                        {
                            Widgets.DrawHighlight(rrect);
                        }

                        var ev = Event.current;
                        if (ev.isKey && ev.type == EventType.keyDown && ev.keyCode == KeyCode.Return
                            || rrcklick)
                        {
                            SoundDefOf.RadioButtonClicked.PlayOneShotOnCamera();
                            SessionClientController.Command((connect) =>
                            {
                                connect.PostingChat(selectCannal.Id, ChatInputText);
                            });
                            ChatInputText = "";
                        }
                    }

                    GUI.SetNextControlName("StartTextField");
                    ChatInputText = GUI.TextField(new Rect(inRect.x + 110f, inRect.y + inRect.height - 25f, inRect.width - 110f - 30f, 25f)
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
                if (form.ResultOK && !string.IsNullOrEmpty(form.InputText) && form.InputText.Replace(" ", "") != "")
                {
                    var mainCannal = SessionClientController.Data.Chats[0];
                    SessionClientController.Command((connect) =>
                    {
                        connect.PostingChat(mainCannal.Id, "/createChat '" + form.InputText.Replace("'", "''") + "'");
                    });
                    //to do Сделать старт крутяшки до обновления чата
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
                    //to do Сделать старт крутяшки до обновления чата
                }
            };
            Find.WindowStack.Add(form);
        }

        private void CannalRename()
        {
            var selectCannal = SessionClientController.Data.Chats[lbCannals.SelectedIndex];
            var form = new Dialog_Input("OCity_Dialog_ChennelRenLabel".Translate(), "OCity_Dialog_ChennelNewName".Translate() + selectCannal.Name, "");
            form.PostCloseAction = () =>
            {
                if (form.ResultOK && form.InputText != null)
                {
                    SessionClientController.Command((connect) =>
                    {
                        connect.PostingChat(selectCannal.Id, "/renameChat '" + form.InputText.Replace("'", "''") + "'");
                    });
                    //to do Сделать старт крутяшки до обновления чата
                }
            };
            Find.WindowStack.Add(form);
        }

        private void PlayerItemMenu(ListBoxPlayerItem item)
        {
            if (item.GroupTitle) return;

            var listMenu = new List<FloatMenuOption>();
            var myLogin = SessionClientController.My.Login;
            ///Личное сообщение
            if (item.Login != myLogin)
            {
                listMenu.Add(new FloatMenuOption("OCity_Dialog_PrivateMessage".Translate(), () =>
                {
                    var privateChat = String.Compare(myLogin, item.Login) < 0
                        ? myLogin + " · " + item.Login
                        : item.Login + " · " + myLogin;
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
                    InfoBox.Text = pl.GetTextInfo();
                    TabIndex = 1;
                }));

            if (listMenu.Count == 0) return;
            var menu = new FloatMenu(listMenu);
            Find.WindowStack.Add(menu);
        }

        public void DoTab1Contents(Rect inRect)
        {
            if (string.IsNullOrEmpty(InfoTabTitle) && string.IsNullOrEmpty(InfoBox.Text))
            {
                var pl = SessionClientController.Data.Players[SessionClientController.My.Login];
                InfoTabTitle = "OCity_Dialog_ChennelPlayerInfoTitle".Translate() + SessionClientController.My.Login;
                InfoBox.Text = pl.GetTextInfo();
            }

            Text.Font = GameFont.Medium;
            Widgets.Label(inRect, InfoTabTitle);

            Text.Font = GameFont.Small;
            var chatAreaOuter = new Rect(inRect.x + 50f, inRect.y + 40f, inRect.width - 50f, inRect.height - 30f - 40f);
            InfoBox.Drow(chatAreaOuter);
        }

        public void DoTab2Contents(Rect inRect)
        {
            Text.Font = GameFont.Medium;
            Widgets.Label(inRect, "OCity_Dialog_AboutMode".Translate());

            Text.Font = GameFont.Small;
            var chatAreaOuter = new Rect(inRect.x + 150f, inRect.y + 40f, inRect.width - 150f, inRect.height - 30f - 40f);
            AboutBox.Drow(chatAreaOuter);

            var rect2 = new Rect(inRect.x, inRect.y + inRect.height / 2f, 150f, inRect.height - 30f - 40f);
            Text.Font = GameFont.Small;
            List<ListableOption> list2 = new List<ListableOption>();
            ListableOption item2 = new ListableOption_WebLink("OCity_Dialog_AutorPage".Translate(), "https://steamcommunity.com/sharedfiles/filedetails/?id=1908437382", IconForums);
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
                            if (connect.PostingChat(mainCannal.Id, "/killmyallplease"))
                            {
                                SessionClientController.Disconnected("OCity_Dialog_DeletedData".Translate());
                            }
                        });
                    }
                };
                Find.WindowStack.Add(form);
            }, IconDelTex);
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
