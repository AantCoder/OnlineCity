using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;
using OCUnion;
using HugsLib;
using RimWorldOnlineCity.UI;
using System.Threading;

namespace RimWorldOnlineCity
{
    public class Dialog_LoginForm : Window
    {
        private string InputName = "";
        private string InputAddr = "";
        private string InputLogin = "";
        private string InputPassword = "";
        private bool SavePassword = false;
        private bool NeedFockus = true;

        private DateTime Refresh;
        private int RefreshSeconds = 15;
        private bool NeedApprove = false;

        public override Vector2 InitialSize
        {
            get { return new Vector2(530f, 400f); }
        }

        public Dialog_LoginForm(bool needApprove = false)
        {
            InputAddr = ModBaseData.GlobalData?.LastIP?.Value ?? "";
            InputLogin = ModBaseData.GlobalData?.LastLoginName?.Value ?? "";
            if (!string.IsNullOrEmpty(InputAddr) && !string.IsNullOrEmpty(InputLogin))
                InputPassword = ModBaseData.GlobalData?.LastPassword?.Value ?? "";
            else InputPassword = "";
            SavePassword = !string.IsNullOrEmpty(InputPassword);
            if (string.IsNullOrEmpty(InputAddr))
            {
                InputAddr = MainHelper.DefaultIP ?? "";
            }
            InputName = MainHelper.ServerList.FirstOrDefault(p => p.Value == InputAddr).Key ?? "";
            //Loger.Log("login/beg " + StorageData.GlobalData.LastIP.Value);
            closeOnCancel = false;
            closeOnAccept = false;
            doCloseButton = false;
            doCloseX = true;
            resizeable = false;
            draggable = true;
            if (needApprove) SetNeedWaitApprove();
        }

        public override void PreOpen()
        {
            base.PreOpen();
        }

        public override void PostClose()
        {
        }

        //PanelText text = null;
        public override void DoWindowContents(Rect inRect)
        {
            /*
            if (text == null)
            {
                text = new PanelText()
                {
                    PrintText = @"OOOOOOOOOO
OOOOOOOOOO
<img ColonyOffExpanding7> OOOOOOOOOO OOOOOOOOOO<btn name=test arg=qwe312> OOOOOOOOOO<img name=ColonyOn height=50> OOOOOOOOOO </btn>OOOOOOOOOO"
                };
                text.Btns.Add("test", new TagBtn()
                {         
                    Name = "test",
                    HighlightIsOver = true,
                    Tooltip = "Test tooltip",
                    ActionClick = (s) => Find.WindowStack.Add(new Dialog_Input("Test title", "Test text: " + s, true))
                });
            }
            text.Drow(inRect);
            return;
            */

            const float categoryPadding = 10f;
            const float categoryInset = 30f;
            const float radioLabelInset = 40f;
            const float mainListingSpacing = 6f;
            const float subListingSpacing = 6f;
            const float subListingLabelWidth = 100f;
            const float subListingRowHeight = 30f;
            const float checkboxListingWidth = 280f;
            const float listingColumnSpacing = 17f;

            var btnSize = new Vector2(140f, 40f);
            var buttonYStart = inRect.height - btnSize.y;

            //кнопки
            var ev = Event.current;
            if (Widgets.ButtonText(new Rect(inRect.width - btnSize.x * 3, buttonYStart, btnSize.x, btnSize.y), "OCity_LoginForm_BtnEnter".Translate())
                || ev.isKey && ev.type == EventType.KeyDown && ev.keyCode == KeyCode.Return
                || NeedApprove && (DateTime.UtcNow - Refresh).TotalSeconds > RefreshSeconds)
            {
                Refresh = DateTime.UtcNow;
                var msgError = SessionClientController.Login(InputAddr, InputLogin, InputPassword
                    , (bool needApprove) =>
                    {
                        SessionClientController.LoginInNewServerIP = ModBaseData.GlobalData?.LastIP?.Value != InputAddr;
                        if (ModBaseData.GlobalData?.LastIP != null)
                        {
                            ModBaseData.GlobalData.LastIP.Value = InputAddr;
                            ModBaseData.GlobalData.LastLoginName.Value = InputLogin;
                            ModBaseData.GlobalData.LastPassword.Value = SavePassword ? InputPassword : "";
                            HugsLibController.SettingsManager.SaveChanges();
                        }

                        if (needApprove) SetNeedWaitApprove();
                        else NeedApprove = false;

                        return true;
                    });
                if (msgError == null)
                {
                    //Loger.Log("login " + StorageData.GlobalData.LastIP.Value);
                    Close();
                }
                if (string.IsNullOrEmpty(msgError))
                {
                    NeedApprove = false;
                }
                else
                {
                    //был вывод сообщения об ошибке (не подтверждения регистрации)
                    if (SavePassword)
                    {
                        SavePassword = false;
                        ModBaseData.GlobalData.LastPassword.Value = "";
                        HugsLibController.SettingsManager.SaveChanges();
                    }
                }
            }

            if (Widgets.ButtonText(new Rect(inRect.width - btnSize.x * 2, buttonYStart, btnSize.x, btnSize.y), "OCity_LoginForm_Register".Translate()))
            {
                Close();
                Find.WindowStack.Add(new Dialog_Registration());
            }

            if (Widgets.ButtonText(new Rect(inRect.width - btnSize.x, buttonYStart, btnSize.x, btnSize.y), "OCity_LoginForm_Close".Translate()))
            {
                Close();
            }

            //заголовок
            var mainListing = new Listing_Standard();
            mainListing.verticalSpacing = mainListingSpacing;
            mainListing.Begin(inRect);
            Text.Font = GameFont.Medium;
            mainListing.Label("OCity_LoginForm_LabelEnter".Translate());
            mainListing.GapLine();
            if (NeedApprove)
            {
                Text.Font = GameFont.Small;
                mainListing.Label("OCity_LoginForm_NeedApproveText0".Translate());
                mainListing.GapLine();
            }
            mainListing.Gap();
            mainListing.End();
            
            var irect = new Rect(inRect);
            irect.y += mainListing.CurHeight;
            irect.width -= 135;
            mainListing.Begin(irect);

            Text.Font = GameFont.Small;

            var iresct = mainListing.GetRect(30f);

            //что к чему
            ListableOption item = new ListableOption_WebLink("OCity_Dialog_Exchenge_What_Point".Translate(), () => 
            {
                var textForm = new Dialog_TextOut(Dialog_MainOnlineCity.AboutGeneralText);
                Find.WindowStack.Add(textForm);
            }, GeneralTexture.IconForums);
            item.DrawOption(new Vector2(iresct.x, iresct.y), 170f/*iresct.width*/);

            item = new ListableOption_WebLink("Send bug data".NeedTranslate(), () =>
            {
                GameUtils.GetBug();
                Close();
            }, GeneralTexture.OCBug);
            item.DrawOption(new Vector2(iresct.x + 170f, iresct.y), 170f/*iresct.width*/);
            //var rectBug = new Rect(iresct.x + 190f, iresct.y, 140f, 24f);
            //GUI.DrawTexture(new Rect(rectBug.x, rectBug.y, 24f, 24f), GeneralTexture.OCBug);
            //Widgets.Label(new Rect(rectBug.x + 30f, rectBug.y, rectBug.width - 30f, 24f), "Send bug data");
            //if (Widgets.ButtonInvisible(rectBug))
            //{
            //    GameUtils.GetBug();
            //    Close();
            //}

            //список серверов
            mainListing.Gap(6f);
            if (mainListing.ButtonTextLabeled("OCity_Dialog_Choose_server".Translate(), InputName))
            {
                List<FloatMenuOption> floatList1 = new List<FloatMenuOption>();
                foreach (var name in MainHelper.ServerList)
                {
                    floatList1.Add(new FloatMenuOption(name.Key, delegate
                    {
                        InputName = name.Key;
                        InputAddr = name.Value;
                    }, MenuOptionPriority.Default, null, null, 0f, null, null));
                }
                Find.WindowStack.Add(new FloatMenu(floatList1));
            }
            mainListing.Gap(6f);

            //поля ввода
            var textEditSize = new Vector2(150f, 25f);

            TextInput(mainListing, "OCity_LoginForm_Server".Translate(),
                (sub, rect) =>
                {
                    InputAddr = GUI.TextField(new Rect(rect.x, rect.y, textEditSize.x, textEditSize.y), InputAddr, 100);
                });

            TextInput(mainListing, "OCity_LoginForm_Login".Translate(),
                (sub, rect) =>
                {
                    InputLogin = GUI.TextField(new Rect(rect.x, rect.y, textEditSize.x, textEditSize.y), InputLogin, 100);
                });

            TextInput(mainListing, "OCity_LoginForm_Pass".Translate(),
                (sub, rect) =>
                {
                    GUI.SetNextControlName("StartTextField");
                    InputPassword = GUI.PasswordField(new Rect(rect.x, rect.y, textEditSize.x, textEditSize.y), InputPassword, "*"[0], 100);

                });

            mainListing.GetRect(8f);
            iresct = mainListing.GetRect(24f);
            iresct.xMin += 10f;
            iresct.width = 183f;
            Widgets.CheckboxLabeled(iresct, "OC_PlayerClient_RememberPassword".Translate(), ref SavePassword, false, null, null, false); //чекбокс Запомнить пароль

            if (NeedFockus)
            {
                NeedFockus = false;
                GUI.FocusControl("StartTextField");
            }

            mainListing.End();
            //Text.Anchor = TextAnchor.UpperLeft;
        }

        private void SetNeedWaitApprove()
        {
            if (!NeedApprove)
            {
                var th = new Thread(() =>
                {
                    try
                    {
                        Thread.Sleep(500);
                        Find.WindowStack.Add(new Dialog_MessageBox("OCity_LoginForm_NeedApproveText0".Translate()));
                    }
                    catch { }
                });
                th.IsBackground = true;
                th.Start();
            }
            NeedApprove = true;
            Refresh = DateTime.UtcNow;
        }

        private void TextInput(Listing_Standard mainListing, string label, Action<Listing_Standard, Rect> drawInput)
        {
            const float categoryPadding = 10f;
            const float categoryInset = 0;// 30f;
            const float radioLabelInset = 40f;
            const float mainListingSpacing = 6f;
            const float subListingSpacing = 0;// 6f;
            const float subListingLabelWidth = 140f;// 100f;
            const float subListingRowHeight = 25;// 30f;
            const float checkboxListingWidth = 280f;
            const float listingColumnSpacing = 17f;

            var expectedHeight = categoryPadding * 2 + (subListingRowHeight + subListingSpacing) * 1;
            MakeSubListing(mainListing, 0, expectedHeight, categoryPadding, categoryInset, subListingSpacing, (sub, width) => {
                sub.ColumnWidth = subListingLabelWidth;
                Text.Anchor = TextAnchor.MiddleLeft;
                var rect = sub.GetRect(subListingRowHeight);
                Widgets.Label(rect, label);
                Text.Anchor = TextAnchor.UpperLeft;
                sub.NewColumn();
                sub.ColumnWidth = width - subListingLabelWidth - listingColumnSpacing;
                rect = sub.GetRect(subListingRowHeight);
                drawInput(sub, rect);
                //InputPassword = GUI.PasswordField(new Rect(rect.x, rect.y, textEditSize.x, textEditSize.y), InputPassword, "*"[0], 100);
            });
        }

        private void MakeSubListing(Listing_Standard mainListing, float width, float allocatedHeight, float padding, float extraInset, float verticalSpacing, Action<Listing_Standard, float> drawContents)
        {
            var subRect = mainListing.GetRect(allocatedHeight);
            width = width > 0 ? width : subRect.width - (padding + extraInset);
            subRect = new Rect(subRect.x + padding + extraInset, subRect.y + padding, width, subRect.height - padding * 2f);
            var sub = new Listing_Standard { verticalSpacing = verticalSpacing };
            sub.Begin(subRect);
            drawContents(sub, width);
            sub.End();
        }

    }
}
