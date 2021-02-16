using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Transfer;
using Transfer.ModelMails;
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
        public OCIncident GetIncident(IncidentTypes type)
        {
            return type == IncidentTypes.Raid ? new IncidentRaid()
                : type == IncidentTypes.Caravan ? new IncidentCaravan()
                : type == IncidentTypes.ChunkDrop ? new IncidentChunkDrop()
                : type == IncidentTypes.Infistation ? new IncidentInfistation()
                : type == IncidentTypes.Quest ? new IncidentQuest()
                : type == IncidentTypes.Bombing ? new IncidentBombing()
                : type == IncidentTypes.Acid ? new IncidentAcid()
                : type == IncidentTypes.EMP ? new IncidentEMP()
                : type == IncidentTypes.Pack ? new IncidentPack()
                : type == IncidentTypes.Eclipse ? new IncidentEclipse()
                : (OCIncident)null;
        }
    }
}