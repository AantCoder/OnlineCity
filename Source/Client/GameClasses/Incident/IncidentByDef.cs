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
            var param = incidentParams != null && incidentParams.Count > 1 ? incidentParams[1] : null;

            var incident = IncidentDef.Named(param);

            var target = GetTarget();

            IncidentParms parms = StorytellerUtility.DefaultParmsNow(incident.category, target);
            parms.forced = true;  //игнорировать все условия для события
            if (!incident.Worker.TryExecute(parms))
            {
                Loger.Log("Error start IncidentDef: " + param);
                return false;
            }
            Loger.Log("Start IncidentDef: " + param);

            return true;
        }

    }
}