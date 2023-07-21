using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Transfer.ModelMails;
using Verse;

namespace OCUnion.Transfer.Model
{
    [Serializable]
    public class ModelPlayerInfoExtended
    {
        public int ColonistsCount { get; set; }
        public int ColonistsNeedingTend { get; set; }
        public int ColonistsDownCount { get; set; }
        public int AnimalObedienceCount { get; set; }
        public bool ExistsEnemyPawns { get; set; }
        public List<int> MaxSkills { get; set; }
        public List<float> MarketValueHistory { get; set; }
        public int RankingCount { get; set; }
        public int MarketValueRanking { get; set; }
        public int MarketValueRankingLast { get; set; }
        public List<string> Achievements { get; set; }

        public List<ModelFunctionMailsView> FunctionMailsView { get; set; }
    }

    [Serializable]
    public class ModelFunctionMailsView
    {
        /// <summary>
        /// Номер очереди, зависит от типа события
        /// 0 - без очереди, без ожиданий
        /// 1 - положительная
        /// 2 - природное бедствие
        /// 3 - агресивное, с предупреждением и ожиданием перед событием (если лвл>=5)
        /// </summary>
        public int NumberOrder { get; set; }

        /// <summary>
        /// Началто ли уже событие. На игроке на каждую очередь может быть только одно такое.
        /// </summary>
        public bool AlreadyStart { get; set; }

        public long PlaceServerId { get; set; }

        public IncidentTypes IncidentType { get; set; }

        public int IncidentMult { get; set; }

            /*
        public static string NumberOrderToString(int numberOrder)
        {
            switch (numberOrder)
            {
                case 3: return "OC_Incidents_Hire_label".Translate(); //Нанять рейд первая вкладка
                case 2: return "OC_Incidents_Impact_label".Translate(); //Воздействие на область вторая вкладка
            }

            

                case IncidentTypes.Caravan:
                case IncidentTypes.ChunkDrop:
                case IncidentTypes.Quest:
                    NumberOrder = 1;
            break;
                case IncidentTypes.Acid:
                case IncidentTypes.EMP:
                case IncidentTypes.Storm:
                case IncidentTypes.Eclipse:
                    NumberOrder = 2;
            break;
                case IncidentTypes.Bombing:
                case IncidentTypes.Infistation:
                case IncidentTypes.Raid:
                    NumberOrder = 3;
            break;
                case IncidentTypes.Def:
                default:
                    NumberOrder = 0;
            break;
            
                case "raid":
                    return IncidentTypes.Raid;
                case "inf":
                    return IncidentTypes.Infistation;
                //case "bomb":
                //  return IncidentTypes.Bombing;
                case "acid":
                    return IncidentTypes.Acid;////
                case "storm":
                    return IncidentTypes.Storm;
                case "emp":
                    return IncidentTypes.EMP;////
                case "eclipse":
                    return IncidentTypes.Eclipse;////
                case "plague":
                    return IncidentTypes.Plague;////
                case "def":
                    return IncidentTypes.Def;
            default:



    < OC_Acid_Rain>Кислотный дождь</OC_Acid_Rain>
	<OC_Storm>Шторм</OC_Storm>
	<OC_Eclipse>Поглотитель света</OC_Eclipse>
	<OC_EMP>Э.М.И.</OC_EMP>
	<OC_Plague>Эпидемия</OC_Plague> 

	<OC_Raid>Рейд</OC_Raid>
	<OC_Bugs>Гигантские жуки</OC_Bugs>

                

            if (Widgets.RadioButtonLabeled(rect, "OC_Acid_Rain".Translate(), SelectTab1Type == "acid"))
            {
                StatusNeedUpdate = true;
                SelectTab1Type = "acid";
            }
            rect.y += rect.height;
            if (Widgets.RadioButtonLabeled(rect, "OC_Plague".Translate(), SelectTab1Type == "plague"))
            {
                StatusNeedUpdate = true;
                SelectTab1Type = "plague";
            }
            rect.y += rect.height;
            if (Widgets.RadioButtonLabeled(rect, "OC_EMP".Translate(), SelectTab1Type == "emp"))
            {
                StatusNeedUpdate = true;
                SelectTab1Type = "emp";
            }
            rect.y += rect.height;
            if (Widgets.RadioButtonLabeled(rect, "OC_Eclipse".Translate(), SelectTab1Type == "eclipse"))
            {
                StatusNeedUpdate = true;
                SelectTab1Type = "eclipse";
            }
        }
            */
    }

}
