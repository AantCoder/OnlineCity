using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;
using RimWorldOnlineCity.UI;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using Rect = UnityEngine.Rect;
using TextBox = RimWorldOnlineCity.UI.TextBox;

namespace RimWorldOnlineCity
{
    public class Dialog_InputImage : Window
    {
        public bool ResultOK = false;
        public Action PostCloseAction;
        public Action<Texture2D, byte[]> SelectImageAction;

        private DateTime LastCheck;
        private Texture2D LastImage;
        private byte[] LastData;

        public override Vector2 InitialSize
        {
            get { return new Vector2(650f, 800f); }
        }

        public Dialog_InputImage()
        {
            doCloseButton = false;
            doCloseX = true;
            forcePause = true;
            absorbInputAroundWindow = true;
            layer = WindowLayer.SubSuper;
        }

        public override void PreOpen()
        {
            base.PreOpen();
        }

        public override void PostClose()
        {
            base.PostClose();
            if (PostCloseAction != null) PostCloseAction();
        }

        public override void DoWindowContents(Rect inRect)
        {
            var btnSize = new Vector2(140f, 40f);

            if ((DateTime.UtcNow - LastCheck).TotalSeconds > 2d)
            {
                LastCheck = DateTime.UtcNow;

                var au = new AncillaryUtil();
                var imageData = au.GetClipboardImageData();

                if (imageData != null && imageData.Length > 0)
                {
                    Texture2D texture = GameUtils.GetTextureFromSaveData(imageData);
                    if (texture.width > 40 && texture.height > 40)
                    {
                        LastImage = texture;
                        LastData = imageData;
                    }
                }
            }

            Text.Font = GameFont.Small;

            Widgets.Label(inRect, "Скопируйте изображение в буфер обмена и нажмите OK. " + Environment.NewLine
                + "Для этого вы можете свернуть игру, выбрать файл и нажать Ctrl+C, потом перейти назад к игре и нажать OK.");

            var curY = 80f;
            var downY = btnSize.y + 20f;
            var areaOuter = new Rect(inRect.x + 20f, inRect.y + curY, inRect.width - 40f, inRect.height - 20f - curY - downY);
            if (LastImage != null)
            {
                var areaInner = LastImage.width / areaOuter.width > LastImage.height / areaOuter.height
                    ? new Rect(areaOuter.x, areaOuter.y, areaOuter.width, areaOuter.width * LastImage.height / LastImage.width)
                    : new Rect(areaOuter.x, areaOuter.y, areaOuter.height * LastImage.width / LastImage.height, areaOuter.height);
                areaInner.x += (areaOuter.width - areaInner.width) / 2f;
                areaInner.y += (areaOuter.height - areaInner.height) / 2f;
                GUI.DrawTexture(areaInner, LastImage);
            }

            var ev = Event.current;
            if (LastImage != null 
                && (Widgets.ButtonText(new Rect(inRect.width - btnSize.x - 20f, inRect.height - btnSize.y - 20f, btnSize.x, btnSize.y), "OK")
                || ev.isKey && ev.type == EventType.KeyDown && ev.keyCode == KeyCode.Return))
            {
                Close();
                SelectImageAction(LastImage, LastData);
            }
        }

    }
}
