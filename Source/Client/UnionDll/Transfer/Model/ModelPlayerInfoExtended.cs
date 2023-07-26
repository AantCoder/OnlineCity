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

        public List<ModelMailStartIncident> FunctionMailsView { get; set; }
    }


}
