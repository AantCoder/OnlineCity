using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace RimWorldOnlineCity.UI
{
    public class AsyncDialog_Input : AsyncDialog<string>
    {
        private bool ModeOkCancel = false;
        private bool ModeOkOnly = false;
        private string TitleText;
        private string PrintText;
        private bool NeedFockus = true;

        private string InputText = "";

        public override Vector2 InitialSize
        {
            get { return new Vector2(400f, 300f); }
        }
        public static Task<string> GetAsync(string title, string printText, string inputTextStart)
        {
            var completionSource = new TaskCompletionSource<string>();
            ModBaseData.Scheduler.Schedule(() =>
            {
                Find.WindowStack.Add(new AsyncDialog_Input(completionSource, title, printText, inputTextStart));
            });
            return completionSource.Task;
        }
        public static Task<string> GetAsync(string title, string printText, bool modeOkOnly = false)
        {
            var completionSource = new TaskCompletionSource<string>();
            ModBaseData.Scheduler.Schedule(() =>
            {
                Find.WindowStack.Add(new AsyncDialog_Input(completionSource, title, printText, modeOkOnly));
            });
            return completionSource.Task;
        }

        private AsyncDialog_Input(TaskCompletionSource<string> completionSource) : base(completionSource)
        {
            closeOnCancel = false;
            closeOnAccept = false;
            doCloseButton = false;
            doCloseX = true;
            forcePause = true;
            absorbInputAroundWindow = true;
            //if (GameStarter.AfterStart == null) layer = WindowLayer.SubSuper;
            /*
            closeOnEscapeKey = true;
            doCloseButton = false;
            doCloseX = true;
            resizeable = false;
            draggable = true;
            */
        }

        /// <summary>
        /// Старт в режиме ввода значения
        /// </summary>
        private AsyncDialog_Input(TaskCompletionSource<string> completionSource, string title, string printText, string inputTextStart)
            : this(completionSource)
        {
            TitleText = title;
            PrintText = printText;
            InputText = inputTextStart;
        }

        /// <summary>
        /// Старт в режиме Да/Нет
        /// </summary>
        private AsyncDialog_Input(TaskCompletionSource<string> completionSource, string title, string printText, bool modeOkOnly = false)
            : this(completionSource)
        {
            TitleText = title;
            PrintText = printText;
            if (modeOkOnly) ModeOkOnly = true;
            else ModeOkCancel = true;
        }

        public override void DoWindowContents(Rect inRect)
        {
            const float mainListingSpacing = 6f;

            var btnSize = new Vector2(140f, 40f);
            var buttonYStart = inRect.height - btnSize.y;

            var ev = Event.current;
            if (Widgets.ButtonText(new Rect(0, buttonYStart, btnSize.x, btnSize.y), "OCity_DialogInput_Ok".Translate())
                || ev.isKey && ev.type == EventType.KeyDown && ev.keyCode == KeyCode.Return)
            {
                Accept(InputText);
            }

            if (!ModeOkOnly && Widgets.ButtonText(new Rect(inRect.width - btnSize.x, buttonYStart, btnSize.x, btnSize.y), "OCity_DialogInput_Cancele".Translate()))
            {
                Close();
            }

            var mainListing = new Listing_Standard();
            mainListing.verticalSpacing = mainListingSpacing;
            mainListing.Begin(inRect);
            Text.Font = GameFont.Medium;
            mainListing.Label(TitleText);

            Text.Font = GameFont.Small;
            mainListing.GapLine();
            mainListing.Gap();

            var textEditSize = new Vector2(150f, 25f);

            Widgets.Label(new Rect(0, 70f, inRect.width, 120f), PrintText);

            if (!ModeOkCancel && !ModeOkOnly)
            {
                GUI.SetNextControlName("StartTextField");
                InputText = GUI.TextField(new Rect(0, 70f + 90f, 300f, 25f), InputText, 1000);
            }

            if (NeedFockus)
            {
                NeedFockus = false;
                GUI.FocusControl("StartTextField");
            }

            mainListing.End();
        }
        
    }
}
