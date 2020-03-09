using Model;
using OCUnion;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace RimWorldOnlineCity.UI
{
    public class PanelChat : DialogControlBase
    {
        private DateTime DataLastChatsTime; //время полученного пакета данных чата от сервера, которое выведено в интерфейс
        private DateTime DataLastChatsTimeUpdateTime; //когда последний раз обновлялась информация в интерфейсе (перерисовывать раз в 5 сек)
        private ListBox<string> lbCannals;
        private int lbCannalsLastSelectedIndex;
        private bool NeedUpdateChat;
        private float lbCannalsHeight = 0;
        private ListBox<ListBoxPlayerItem> lbPlayers;
        private TextBox ChatBox = new TextBox();

        private float PanelLastHeight = 0;
        private long UpdateLogHash;
        private string lbCannalsGoToChat; //как только появиться этот чат перейти к нему
        private bool NeedFockus = true;
        //private string ChatText = "";
        //private Vector2 ChatScrollPosition;
        private bool ChatScrollToDown = false;
        private DateTime ChatLastPostTime;
        private string ChatInputText = "";

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

        public void Drow(Rect inRect)
        {
            var iconWidth = 25f;
            var iconWidthSpase = 30f;

            /// -----------------------------------------------------------------------------------------
            /// Список каналов
            ///
            if (SessionClientController.Data.Chats != null)
            {
                lock (SessionClientController.Data.Chats)
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
                        lbPlayers.OnClick += (index, item) =>
                        {
                        //убираем выделение
                        lbPlayers.SelectedIndex = -1;
                        //вызываем контекстное меню
                        PlayerItemMenu(item);
                        };

                        lbPlayers.Tooltip = (item) => item.Tooltip;
                    }

                    if (PanelLastHeight != inRect.height)
                    {
                        PanelLastHeight = inRect.height;
                        lbPlayers.Area = new Rect(inRect.x
                            , inRect.y + iconWidthSpase + lbCannalsHeight + 22f
                            , 100f
                            , inRect.height - (iconWidthSpase + lbCannalsHeight + 22f));
                    }

                    if (NeedUpdateChat)
                    {
                        lbCannalsLastSelectedIndex = -1;
                        NeedUpdateChat = false;
                    }

                    var nowUpdateChat = DataLastChatsTime != SessionClientController.Data.ChatsTime.Time;
                    if (nowUpdateChat)
                    {
                        Loger.Log("Client UpdateChats nowUpdateChat");
                        DataLastChatsTime = SessionClientController.Data.ChatsTime.Time;
                        lbCannalsLastSelectedIndex = -1; //сброс для обновления содержимого окна
                        NeedUpdateChat = true;
                    }

                    if (nowUpdateChat
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
                            addTit("OCity_Dialog_Exchenge_Chat".Translate());
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
                            addTit("OCity_Dialog_Exchenge_Gamers".Translate());
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
                    }

                    lbCannals.Drow();
                    lbPlayers.Drow();

                    var iconRect = new Rect(inRect.x, inRect.y, iconWidth, iconWidth);
                    TooltipHandler.TipRegion(iconRect, "OCity_Dialog_ChennelCreate".Translate());
                    if (Widgets.ButtonImage(iconRect, GeneralTexture.IconAddTex))
                    {
                        CannalAdd();
                    }

                    if (lbCannals.SelectedIndex > 0 && SessionClientController.Data.Chats.Count > lbCannals.SelectedIndex)
                    {
                        //Если что-то выделено, и это не общий чат (строка 0)
                        iconRect.x += iconWidthSpase;
                        TooltipHandler.TipRegion(iconRect, "OCity_Dialog_ChennelClose".Translate());
                        if (Widgets.ButtonImage(iconRect, GeneralTexture.IconDelTex))
                        {
                            CannalDelete();
                        }
                    }

                    if (lbCannals.SelectedIndex >= 0 && SessionClientController.Data.Chats.Count > lbCannals.SelectedIndex)
                    {
                        iconRect.x += iconWidthSpase;
                        TooltipHandler.TipRegion(iconRect, "OCity_Dialog_OthersFunctions".Translate());
                        if (Widgets.ButtonImage(iconRect, GeneralTexture.IconSubMenuTex))
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
                            //if (selectCannal.Posts != null || selectCannal.Posts.Count > 0)
                            //{
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
                            // else
                            //   ChatBox.Text = "";
                            //}
                            //  else
                            // ChatBox.Text = "";
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
                            if (ev.isKey && ev.type == EventType.KeyDown && ev.keyCode == KeyCode.Return
                                || rrcklick)
                            {
                                //SoundDefOf.RadioButtonClicked.PlayOneShotOnCamera();
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
                    Dialog_MainOnlineCity.ShowInfo("OCity_Dialog_ChennelPlayerInfoTitle".Translate() + item.Login, pl.GetTextInfo());
                }));

            if (listMenu.Count == 0) return;
            var menu = new FloatMenu(listMenu);
            Find.WindowStack.Add(menu);
        }

    }
}
