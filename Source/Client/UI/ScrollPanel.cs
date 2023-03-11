using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace RimWorldOnlineCity.UI
{
    public class ScrollPanel : DialogControlBase
    {
        public Func<Rect, Rect> DrawInner { get; set; }
        public Rect AreaOuter { get; set; }

        private Rect AreaInner;

        private Vector2 ScrollPosition;
        
        public void Draw()
        {
            if (DrawInner == null) return;

            if (AreaInner.width <= 0)
            {
                AreaInner = new Rect(0, 0, AreaOuter.width - WidthScrollLine, AreaOuter.height);
            }
            if (AreaInner.width <= 0) return;

            ScrollPosition = GUI.BeginScrollView(
                AreaOuter
                , ScrollPosition
                , AreaInner);
            var rectDraw = DrawInner(AreaInner);
            GUI.EndScrollView();
            if (rectDraw.height > AreaInner.height) AreaInner.height = rectDraw.height;
            if (AreaOuter.height > AreaInner.height) AreaInner.height = AreaOuter.height;
        }
    }
}
