using OCUnion;
using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace RimWorldOnlineCity
{
    class IncidentByDef : OCIncident
    {
        public override bool TryExecuteEvent()
        {
            var incident = IncidentDef.Named(this.param);

            var target = GetTarget();

            IncidentParms parms = StorytellerUtility.DefaultParmsNow(incident.category, target);
            parms.forced = true;  //игнорировать все условия для события
            if (!incident.Worker.TryExecute(parms))
            {
                Loger.Log("Error start IncidentDef: " + this.param);
                return false;
            }
            Loger.Log("Start IncidentDef: " + this.param);

            return true;
        }

    }
}