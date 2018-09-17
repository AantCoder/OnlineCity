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


        public ListBox()
        {
        }


        private Vector2 ScrollPosition;

        protected virtual void DrowLine(int line, T item, Rect rect)
        {
            var text = item.ToString();
            Widgets.Label(rect, text);
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
            for (int i = 0; i < DataSource.Count; i++)
            {
                //светлый фон каждой второй строки
                if (OddLineHighlight && i % 2 == 1)
                {
                    GUI.DrawTexture(rect, TradeUI.TradeAlternativeBGTex);
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
                rect.y += LineHeight;
            }

            GUI.EndScrollView();
        }
    }
}
