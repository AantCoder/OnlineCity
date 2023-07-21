using Model;
using OCUnion;
using OCUnion.Transfer;
using OCUnion.Transfer.Model;
using RimWorldOnlineCity.Services;
using RimWorldOnlineCity.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace RimWorldOnlineCity
{
    public class PanelViewInfo : DialogControlBase
    {
        private bool Inited;


        public void Init()
        {
            Inited = true;
        }

        public void Drow(Rect inRect)
        {
            if (!Inited) Init();

        }
    }
}
