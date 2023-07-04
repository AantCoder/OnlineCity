using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace RimWorldOnlineCity.UI
{
    public class StatusWindow
    {
        private string _status;
        private string _title;
        private string _heading;

        public string Status
        {
            get
            {
                _cancellation.Token.ThrowIfCancellationRequested();
                return _status;
            }
            set { _status = value; }
        }
        public string Title
        {
            get
            {
                _cancellation.Token.ThrowIfCancellationRequested();
                return _title;
            }
            set { _title = value; }
        }
        public string Heading
        {
            get
            {
                _cancellation.Token.ThrowIfCancellationRequested();
                return _heading;
            }
            set { _heading = value; }
        }

        private bool _isClosed;
        private CancellationTokenSource _cancellation = new CancellationTokenSource();
        private InnerWindow _inner;

        private StatusWindow(string title, string heading, string status)
        {
            _isClosed = false;
            Heading = heading;
            Status = status;
            Title = title;
        }

        public ConfiguredTaskAwaitable Close()
        {
            _isClosed = true;
            if (_inner != null)
            {
                var captured = _inner;
                _inner = null;
                return ModBaseData.Scheduler.Schedule(() => captured.Close(false));
            }
            return Task.CompletedTask.ConfigureAwait(false);
        }

        public static async Task<StatusWindow> Create(string title = "Status", string heading = "Processing", string status = "<unknown>")
        {
            var result = new StatusWindow(title, heading, status);
            await ModBaseData.Scheduler.Schedule(() => Find.WindowStack.Add(new InnerWindow(result)));
            return result;
        }

        private class InnerWindow : Window
        {
            public override Vector2 InitialSize
            {
                get { return new Vector2(500f, 500f); } //новое поле ввода с описанием +50
            }

            Verse.WeakReference<StatusWindow> _controller;

            public InnerWindow(StatusWindow controller)
            {
                _controller = new Verse.WeakReference<StatusWindow>(controller);
                controller._inner = this;

                //Widgets.FillableBarLabeled ()
                closeOnCancel = false;
                closeOnAccept = false;
                doCloseButton = false;
                doCloseX = true;
                forcePause = true;
                absorbInputAroundWindow = true;
            }

            public override void PostClose()
            {
                if (_controller.IsAlive)
                {
                    StatusWindow controller = _controller.Target;
                    controller._cancellation.Cancel();
                }


                base.PostClose();
            }

            public override void DoWindowContents(Rect inRect)
            {
                if (!_controller.IsAlive)
                {
                    Close(false);
                    return;
                }

                StatusWindow controller = _controller.Target;
                if (controller._isClosed)
                {
                    Close(false);
                    return;
                }

                const float mainListingSpacing = 6f;

                var btnSize = new Vector2(140f, 40f);
                var buttonYStart = inRect.height - btnSize.y;

                var ev = Event.current;

                var mainListing = new Listing_Standard();
                mainListing.verticalSpacing = mainListingSpacing;
                mainListing.Begin(inRect);
                Text.Font = GameFont.Medium;
                mainListing.Label(controller.Title);

                Text.Font = GameFont.Small;
                mainListing.GapLine();
                mainListing.Gap();

                mainListing.Label(controller.Heading);
                mainListing.Gap();

                var textEditSize = new Vector2(150f, 25f);
                var rect = new Rect(0, 70f, inRect.width, textEditSize.y);
                mainListing.Label(controller.Status, -1f, null);
                mainListing.Gap();

                mainListing.End();
            }
        }
    }
}