﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;
using Transfer;
using OCUnion;
using HugsLib;

namespace RimWorldOnlineCity
{
    public class Dialog_Registration : Window
    {
        private string InputAddr = "";
        private string InputLogin = "";
        private string InputPassword = "";
        private string InputPassword2 = "";
        private string InputEmail = "";
        private bool NeedFockus = true;

        public override Vector2 InitialSize
        {
            get { return new Vector2(400f, 400f); }
        }

        public Dialog_Registration()
        {
            InputAddr = ModBaseData.GlobalData.LastIP.Value;
            if (string.IsNullOrEmpty(InputAddr))
            {
                InputAddr = MainHelper.DefaultIP;
            }
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

            if (Widgets.ButtonText(new Rect(inRect.width - btnSize.x, buttonYStart, btnSize.x, btnSize.y), "OCity_Dialog_Registration_Close".Translate()))
            {
                Close();
            }

            var ev = Event.current;
            if (Widgets.ButtonText(new Rect(inRect.width - btnSize.x * 2, buttonYStart, btnSize.x, btnSize.y), "OCity_Dialog_Registration_BtnReg".Translate())
                || ev.isKey && ev.type == EventType.KeyDown && ev.keyCode == KeyCode.Return)
            {
                if (InputPassword2 != InputPassword)
                {
                    Find.WindowStack.Add(new Dialog_MessageBox("OCity_LoginForm_Err1".Translate()));
                }
                else if (InputAddr.Length <= 2 || InputLogin.Length <= 2 || InputPassword.Length <= 2)
                {
                    Find.WindowStack.Add(new Dialog_MessageBox("OCity_LoginForm_Err2".Translate()));
                }
                else
                {
                    var msgError = SessionClientController.Registration(InputAddr, InputLogin, InputPassword, InputEmail
                        , () => 
                        { 
                            SessionClientController.LoginInNewServerIP = ModBaseData.GlobalData.LastIP.Value != InputAddr;
                            ModBaseData.GlobalData.LastIP.Value = InputAddr;
                            ModBaseData.GlobalData.LastLoginName.Value = InputLogin;
                            HugsLibController.SettingsManager.SaveChanges();
                        });
                    if (msgError == null)
                    {
                        Close();
                    }
                }
            }

            var mainListing = new Listing_Standard();
            mainListing.verticalSpacing = mainListingSpacing;
            mainListing.Begin(inRect);
            Text.Font = GameFont.Medium;
            mainListing.Label("OCity_Dialog_Registration_LabelReg".Translate());
            
            Text.Font = GameFont.Small;
            mainListing.GapLine();
            mainListing.Gap();

            var textEditSize = new Vector2(150f, 25f);
            
            TextInput(mainListing, "OCity_Dialog_Registration_Server".Translate(),
                (sub, rect) =>
                {
                    GUI.SetNextControlName("StartTextField");
                    InputAddr = GUI.TextField(new Rect(rect.x, rect.y, textEditSize.x, textEditSize.y), InputAddr, 100);
                });

            TextInput(mainListing, "OCity_Dialog_Registration_Login".Translate(),
                (sub, rect) =>
                {
                    InputLogin = GUI.TextField(new Rect(rect.x, rect.y, textEditSize.x, textEditSize.y), InputLogin, 100);
                });

            TextInput(mainListing, "OCity_Dialog_Registration_Pass".Translate(),
                (sub, rect) =>
                {
                    InputPassword = GUI.PasswordField(new Rect(rect.x, rect.y, textEditSize.x, textEditSize.y), InputPassword, "*"[0], 100);
                });

            TextInput(mainListing, "OCity_Dialog_Registration_Check".Translate(),
                (sub, rect) =>
                {
                    InputPassword2 = GUI.PasswordField(new Rect(rect.x, rect.y, textEditSize.x, textEditSize.y), InputPassword2, "*"[0], 100);
                });

            TextInput(mainListing, "OCity_LoginForm_Mail".Translate(),
                (sub, rect) =>
                {
                    InputEmail = GUI.TextField(new Rect(rect.x, rect.y, textEditSize.x, textEditSize.y), InputEmail, 100);
                });

            if (NeedFockus)
            {
                NeedFockus = false;
                GUI.FocusControl("StartTextField");
            }

            //mainListing.Label("Регистрация3.");
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
            const float subListingLabelWidth = 180f;// 130f;
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
