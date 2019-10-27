using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace RimWorldOnlineCity.UI
{
    public class DialogControlBase
    {
        public const float WidthScrollLine = 18f;
        
        private float TextHeight_value = 0;

        public float TextHeight
        {
            get
            {
                if (TextHeight_value == 0)
                {
                    TextHeight_value = Text.CalcSize("HWDOA/|").y;
                }
                return TextHeight_value;
            }
        }
    }
}
