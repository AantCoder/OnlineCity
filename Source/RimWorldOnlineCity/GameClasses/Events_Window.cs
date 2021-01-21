using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using RimWorld;
using UnityEngine;

namespace Event_Window
{
    class Events_Window : MainTabWindow  // ui для отладки ивентов, нужно будет убрать
    {
        public override Vector2 InitialSize => new Vector2(600, 400);

        public override void DoWindowContents(Rect inRect) //окно с кнопками вызовов ивентов
        {
            base.DoWindowContents(inRect);
            resizeable = false;
            forcePause = false;
            doCloseX = true;

            if (Widgets.ButtonText(new Rect(0, 10, 250, 35), $"do_Test_Event"))
            {
                var incident_pod = new OC_Incidents.Test_Incident();
                if (!incident_pod.TryExecuteEvent())
                {
                    Messages.Message($"FailedTestEvent", MessageTypeDefOf.RejectInput);
                }
            }
            if (Widgets.ButtonText(new Rect(0, 55, 250, 35), $"do_Test_raid"))
            {
                var raid_incident = new OC_Incidents.Raid_worker();
                if (!raid_incident.TryExecuteEvent())
                {
                    Messages.Message($"FailedTestRaid", MessageTypeDefOf.RejectInput);
                }
            }
        }
    }
}
