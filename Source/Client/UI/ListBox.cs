using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;

namespace RimWorldOnlineCity.UI
{
    public class ListBox<T> : DialogControlBase
    {
        public Rect Area;

        public List<T> DataSource;

        public int SelectedIndex = -1;

        public event Action<int, T> OnClick;

        //public event Action<int, string> OnClickRight;
        
        public Func<T, string> Tooltip = (item) => item.ToString();

        public bool OddLineHighlight = false;

        public bool ShowSelected = true;

        public float LineHeight = 0;

        public bool UsePanelText = false;

        private PanelText Text = new PanelText();

        public ListBox()
        {
        }


        private Vector2 ScrollPosition;

        protected virtual void DrowLine(int line, T item, Rect rect)
        {
            var text = item.ToString();
            if (UsePanelText)
            {
                var li = new Listing_Standard();
                li.Begin(rect);
                Text.PrintText = text;
                Text.Drow(new Rect(0, 0, rect.width, rect.height));
                li.End();
            }
            else
            {
                Widgets.Label(rect, text);
            }
        }

        public virtual void Drow()
        {
            if (DataSource == null) return;
            if (LineHeight == 0) LineHeight = TextHeight;

            ScrollPosition = GUI.BeginScrollView(
                Area
                , ScrollPosition
                , new Rect(0, 0, Area.width - WidthScrollLine, DataSource.Count * LineHeight));

            var rect = new Rect(0, 0, Area.width - WidthScrollLine, LineHeight);
            var iBeg = (int)(ScrollPosition.y / LineHeight - 1);
            if (iBeg < 0) iBeg = 0;
            var iEnd = iBeg + (int)(Area.height / LineHeight + 2);
            for (int i = iBeg; i < DataSource.Count && i < iEnd; i++)
            {
                rect.y = i * LineHeight;

                //светлый фон каждой второй строки
                if (OddLineHighlight && i % 2 == 1)
                {
                    Widgets.DrawAltRect(rect);
                }

                //отрисовка строки
                DrowLine(i, DataSource[i], rect);

                //выделенная строка
                if (ShowSelected && i == SelectedIndex)
                {
                    Widgets.DrawHighlightSelected(rect);
                    //Widgets.DrawHighlight(rect);
                }
                //подсветка строки под курсором
                if (!OddLineHighlight && Mouse.IsOver(rect))
                {
                    Widgets.DrawHighlight(rect);
                }
                //всплывающая подсказка
                if (Tooltip != null)
                {
                    var tt = Tooltip(DataSource[i]);
                    if (!string.IsNullOrEmpty(tt))
                    {
                        TooltipHandler.TipRegion(rect, tt);
                    }
                }

                //событие нажатия на строку
                if (Widgets.ButtonInvisible(rect))
                {
                    SelectedIndex = i;
                    if (OnClick != null) OnClick(i, DataSource[i]);
                }
            }

            GUI.EndScrollView();
        }
    }
}
