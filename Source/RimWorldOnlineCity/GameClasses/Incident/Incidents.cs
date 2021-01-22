using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Transfer;
using Verse;

namespace RimWorldOnlineCity
{
    public class Incidents
    {
        /*
         * Для добавления нового инциндента создайте класс от родителя OCIncident
         * И внесите соответствующие правки в этот и следующие классы:
         * CallIncidentCmd в список и обработку параметров
         * enum RaidTypes в список
         * ModelMailTrade если нужны доп параметры (стараться не раздувать или отрефакторить и разделить контейнер для разных типов писем)
         * MailController.MailProcessStartEvent если были добавлены доп параметр
        */
        public OCIncident GetIncident(RaidTypes type)
        {
            return type == RaidTypes.Raid ? new IncidentRaid()
                : type == RaidTypes.Caravan ? new IncidentCaravan()
                : type == RaidTypes.ChunkDrop ? new IncidentChunkDrop()
                : type == RaidTypes.Infistation ? new IncidentInfistation()
                : type == RaidTypes.Quest ? new IncidentQuest()
                : (OCIncident)null;
        }
    }
}