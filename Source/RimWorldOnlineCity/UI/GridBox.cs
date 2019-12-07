using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;

namespace RimWorldOnlineCity.UI
{
    public class GridBox<T> : ListBox<T>
    {
        public Action<int, T, Rect> OnDrawLine = null;

        public GridBox()
        {
            LineHeight = 30f;
            OddLineHighlight = true;
            ShowSelected = false;
        }

        protected override void DrowLine(int line, T item, Rect rect)
        {
            GUI.BeginGroup(rect);
            if (OnDrawLine != null) OnDrawLine(line, item, rect.AtZero());
            GUI.EndGroup();
        }
    }
}
