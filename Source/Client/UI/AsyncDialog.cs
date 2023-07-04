using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace RimWorldOnlineCity.UI
{
    public abstract class AsyncDialog<T> : Window
    {
        private TaskCompletionSource<T> completionSource;

        protected AsyncDialog(TaskCompletionSource<T> completionSource)
        {
            this.completionSource = completionSource;

            closeOnCancel = false;
            closeOnAccept = false;
            doCloseButton = false;
            doCloseX = true;
            resizeable = false;
            draggable = true;
        }

        public override void PostClose()
        {
            if (!completionSource.Task.IsCompleted)
                completionSource.SetCanceled();
            base.PostClose();
        }

        public override void OnAcceptKeyPressed()
        {
            Accept(default(T));
            base.OnAcceptKeyPressed();
        }

        protected void Accept(T value)
        {
            completionSource.TrySetResult(value);
            Close(false);
        }
    }
}
