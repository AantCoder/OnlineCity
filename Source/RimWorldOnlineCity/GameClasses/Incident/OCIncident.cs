using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Transfer;

namespace RimWorldOnlineCity
{
    public abstract class OCIncident
    {
        public float mult = 1;
        public IncidentStrategys strategy = IncidentStrategys.ImmediateAttack;
        public IncidentArrivalModes arrivalMode = IncidentArrivalModes.EdgeWalkIn;
        public string faction = null;

        public abstract bool TryExecuteEvent();
    }

}
