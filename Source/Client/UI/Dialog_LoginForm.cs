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
using System.Threading.Tasks;
using System.Threading;

namespace RimWorldOnlineCity
{
    public struct LoginFormResult
    {
        public string hostname;
        public string username;
        public string password;
    }
    public class Dialog_LoginForm : AsyncDialog<LoginFormResult>
    {
        private string InputName = "";
        private string InputAddr = "";
        private string InputLogin = "";
        private string InputPassword = "";
        private bool NeedFockus = true;

        public override Vector2 InitialSize
        {
            get { return new Vector2(500f, 400f); }
        }

        public static Task<LoginFormResult> GetAsync(string hostname = "", string username = "", string password = "")
        {
            var completionSource = new TaskCompletionSource<LoginFormResult>();
            ModBaseData.Scheduler.Schedule(() =>
            {
                Find.WindowStack.Add(new Dialog_LoginForm(completionSource, hostname, username, password));
            });
            return completionSource.Task;
        }
        private Dialog_LoginForm(TaskCompletionSource<LoginFormResult> completionSource, string hostname = "", string username = "", string password = "") : base(completionSource)
        {
            InputAddr = hostname;
            InputLogin = username;
            InputPassword = password;
            InputName = MainHelper.ServerList.FirstOrDefault(p => p.Value == InputAddr).Key ?? "";
        }

        public override void DoWindowContents(Rect inRect)
        {
            const float mainListingSpacing = 6f;

            var btnSize = new Vector2(140f, 40f);
            var buttonYStart = inRect.height - btnSize.y;


            //кнопки
            var ev = Event.current;
            if (Widgets.ButtonText(new Rect(inRect.width - btnSize.x * 3, buttonYStart, btnSize.x, btnSize.y), "OCity_LoginForm_BtnEnter".Translate())
                || ev.isKey && ev.type == EventType.KeyDown && ev.keyCode == KeyCode.Return)
            {
                Accept(new LoginFormResult() { hostname = InputAddr, username = InputLogin, password = InputPassword });
            }

            if (Widgets.ButtonText(new Rect(inRect.width - btnSize.x * 2, buttonYStart, btnSize.x, btnSize.y), "OCity_LoginForm_Register".Translate()))
            {
                Close();
                _ = SessionClientController.DoUserRegistration();
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

            item = new ListableOption_WebLink("Send bug data", () =>
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
