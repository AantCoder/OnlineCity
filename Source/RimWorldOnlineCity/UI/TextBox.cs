using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;

namespace RimWorldOnlineCity.UI
{
    public class TextBox : DialogControlBase
    {
        public string Text;
        public Vector2 ScrollPosition = new Vector2();

        public void Drow(Rect chatAreaOuter, bool scrollToDown = false)
        {
            var chatAreaInner = new Rect(0, 0, chatAreaOuter.width - ListBox<string>.WidthScrollLine, 0);
            if (chatAreaInner.width <= 0) return;
            //chatAreaInner.height = Text.CalcHeight(ChatText, chatAreaInner.width);
            GUI.skin.textField.wordWrap = true;
            chatAreaInner.height = GUI.skin.textField.CalcHeight(new GUIContent(Text), chatAreaInner.width);
            //if (chatAreaInner.height > chatAreaOuter.height) chatAreaInner.height -= chatAreaOuter.height;
            if (scrollToDown)
            {
                ScrollPosition.y = chatAreaInner.height - chatAreaOuter.height;
            }

            ScrollPosition = GUI.BeginScrollView(chatAreaOuter, ScrollPosition, chatAreaInner);
            GUILayout.BeginArea(chatAreaInner);
            GUILayout.TextField(Text ?? "", "Label");
            GUILayout.EndArea();
            GUI.EndScrollView();
        }
    }
}
