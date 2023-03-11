using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;

namespace RimWorldOnlineCity.UI
{
    public class TextImageBox : DialogControlBase
    {
        public string Text
        {
            get => Panel.PrintText;
            set => Panel.PrintText = value;
        }
        public Vector2 ScrollPosition = new Vector2();
        private PanelText Panel;

        private string TextLastDrow;
        private float WidthLastDrow;
        private float HeightLastDrow;
        private bool ScrollToDownLastDrow;

        public TextImageBox()
        {
            Panel = new PanelText();
            Text = "";
        }

        public void Drow(Rect chatAreaOuter, bool scrollToDown = false)
        {
            var chatAreaInner = new Rect(0, 0, chatAreaOuter.width - ListBox<string>.WidthScrollLine, 0);
            if (chatAreaInner.width <= 0) return;

            var calcHeight = 0f;
            if (TextLastDrow == Panel.PrintText && WidthLastDrow == chatAreaInner.width && HeightLastDrow > 0)
            {
                //если размер уже вычеслен для этого текста, то устанавливаем его
                chatAreaInner.height = HeightLastDrow;
                if (ScrollToDownLastDrow)
                {
                    scrollToDown = true;
                    ScrollToDownLastDrow = false;
                }
            }
            else
            {
                //если размер не известен, то выводим до конца
                calcHeight = 10000f;
                chatAreaInner.height = chatAreaOuter.height;
                //если была команда прокрутить, то передадим её на следующий фрейм, т.к. тут нет данных о высоте
                ScrollToDownLastDrow = scrollToDown; 
            }

            if (scrollToDown)
            {
                ScrollPosition.y = chatAreaInner.height - chatAreaOuter.height;
            }

            ScrollPosition = GUI.BeginScrollView(chatAreaOuter, ScrollPosition, chatAreaInner);
            GUILayout.BeginArea(chatAreaInner);

            HeightLastDrow = Panel.Drow(chatAreaInner, calcHeight);
            WidthLastDrow = chatAreaInner.width;
            TextLastDrow = Panel.PrintText;

            GUILayout.EndArea();
            GUI.EndScrollView();


        }
    }
}
