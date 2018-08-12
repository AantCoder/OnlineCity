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
        
        public Func<T, string> Tooltip = (item) => item.ToString();

        //public event Action<int, string> OnClickRight;

        public ListBox()
        {
        }

        private Vector2 ScrollPosition;

        public void Drow()
        {
            if (DataSource == null) return;
            ScrollPosition = GUI.BeginScrollView(
                Area
                , ScrollPosition
                , new Rect(0, 0, Area.width - WidthScrollLine, DataSource.Count * TextHeight));
            
            var rect = new Rect(0, 0, Area.width - WidthScrollLine, TextHeight);
            for (int i = 0; i < DataSource.Count; i++)
            {
                var text = DataSource[i].ToString();
                Widgets.Label(rect, text);

                if (i == SelectedIndex)
                {
                    Widgets.DrawHighlightSelected(rect);
                    //Widgets.DrawHighlight(rect);
                }
                if (Mouse.IsOver(rect))
                {
                    Widgets.DrawHighlight(rect);
                }
                if (Tooltip != null)
                {
                    var tt = Tooltip(DataSource[i]);
                    if (!string.IsNullOrEmpty(tt))
                    {
                        TooltipHandler.TipRegion(rect, tt);
                    }
                }

                if (Widgets.ButtonInvisible(rect))
                {
                    SelectedIndex = i;
                    if (OnClick != null) OnClick(i, DataSource[i]);
                }
                rect.y += TextHeight;
            }

            GUI.EndScrollView();
        }
    }
}
