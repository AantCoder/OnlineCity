using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using RimWorld;
using UnityEngine;
using Transfer;
using Transfer.ModelMails;

namespace RimWorldOnlineCity
{
    class MainTabTestButton : MainTabWindow  // ui для отладки ивентов, нужно будет убрать
    {

        public override Vector2 InitialSize => new Vector2(600, 400);

        public override void DoWindowContents(Rect inRect) //окно с кнопками вызовов ивентов
        {
            base.DoWindowContents(inRect);
            resizeable = false;
            forcePause = false;
            doCloseX = true;

            if (Widgets.ButtonText(new Rect(0, 10, 250, 35), $"do_Test_raid"))
            {
                var incident = new RimWorldOnlineCity.Incidents().GetIncident(IncidentTypes.Raid);
                //incident.mult = ;
                //incident.arrivalMode = ;
                //incident.strategy = ;
                //incident.faction = ;
                if (!incident.TryExecuteEvent())
                {
                    Messages.Message($"FailedTestRaid", MessageTypeDefOf.RejectInput);
                }
            }
            /*if (Widgets.ButtonText(new Rect(0, 55, 250, 35), $"do_Test_raid"))
            {
            }*/
        }
    }
}
