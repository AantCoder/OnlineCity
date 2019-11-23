using System;
using UnityEngine;
using Verse;
using OCUnion;
using HugsLib;

namespace RimWorldOnlineCity
{
    public class Dialog_LoginForm : Window
    {
        private string InputAddr = "";
        private string InputLogin = "";
        private string InputPassword = "";
        private bool NeedFockus = true;

        public override Vector2 InitialSize
        {
            get { return new Vector2(500f, 300f); }
        }

        public Dialog_LoginForm()
        {
            InputAddr = StorageData.GlobalData.LastIP.Value;
            InputLogin = StorageData.GlobalData.LastLoginName.Value;
            
            if (string.IsNullOrEmpty(InputAddr))
            {
                InputAddr = MainHelper.DefaultIP;
            }
            //Loger.Log("login/beg " + StorageData.GlobalData.LastIP.Value);
            closeOnCancel = false;
            closeOnAccept = false;
            doCloseButton = false;
            doCloseX = true;
            resizeable = false;
            draggable = true;
        }

        public override void PreOpen()
        {
            base.PreOpen();
        }

        public override void PostClose()
        {
        }

        public override void DoWindowContents(Rect inRect)
        {
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
                || ev.isKey && ev.type == EventType.keyDown && ev.keyCode == KeyCode.Return)
            {
                var msgError = SessionClientController.Login(InputAddr, InputLogin, InputPassword);
                if (msgError == null)
                {
                    StorageData.GlobalData.LastIP.Value = InputAddr;
                    StorageData.GlobalData.LastLoginName.Value = InputLogin;
                    HugsLibController.SettingsManager.SaveChanges();
                    //Loger.Log("login " + StorageData.GlobalData.LastIP.Value);
                    Close();
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

            Text.Font = GameFont.Small;
            mainListing.GapLine();
            mainListing.Gap();
            
            var iresct = mainListing.GetRect(20f);

            //что к чему
            ListableOption item = new ListableOption_WebLink("About Mod :-))".NeedTranslate(), () => 
            {
                var textForm = new Dialog_TextOut(Dialog_MainOnlineCity.AboutGeneralText);
                Find.WindowStack.Add(textForm);
            }, Dialog_MainOnlineCity.IconForums);
            item.DrawOption(new Vector2(iresct.x, iresct.y), iresct.width);

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
            
            if (NeedFockus)
            {
                NeedFockus = false;
                GUI.FocusControl("StartTextField");
            }

            mainListing.End();
            //Text.Anchor = TextAnchor.UpperLeft;
        }

        private void TextInput(Listing_Standard mainListing, string label, Action<Listing_Standard, Rect> drawInput)
        {
            const float categoryPadding = 10f;
            const float categoryInset = 0;// 30f;
            const float radioLabelInset = 40f;
            const float mainListingSpacing = 6f;
            const float subListingSpacing = 0;// 6f;
            const float subListingLabelWidth = 130;// 100f;
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