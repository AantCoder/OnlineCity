using Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace OCUnion.Transfer.Model
{

    [Serializable]
    public class PawnStat : ThingTrade
    {
        public static PawnStat CreateTrade(Pawn pawn)
        {
            var that = new PawnStat();
            that.SetFromThing(pawn, 1, false);

            that.Skills = GetSkills(pawn);

            return that;
        }

        public static List<int> GetSkills(Pawn pawn)
        { 
            var skills = new List<int>();
            for (int i = 0; i < pawn.skills.skills.Count; i++)
            {
                skills.Add(pawn.skills.skills[i].Level);
            }
            return skills;
        }

        public List<int> Skills { get; set; }

        private string SkillsToString => Skills.Aggregate("", (r, i) => r != "" ? r + "-" + i.ToString() : i.ToString());

        public string ToStringLog() =>
            $"{PawnParam}, cost: {GameCost}, skills: {SkillsToString}";

        /// <summary>
        /// Информация достатоная для отображения.
        /// defName, кол-во, цена, качество, параметры пешки (PawnParam), скилы (SkillsToString)
        /// </summary>
        /// <returns></returns>
        public override string PackToString()
        {
            return $"{SkillsToString}," + base.PackToString();
        }

        public override ThingTradeInfoParam UnpackFromString(string str)
        {
            var comps = str.Split(new char[] { ',' }, 2);
            var skills = comps[0].Split('-');

            if (skills.Length < 8) return base.UnpackFromString(str);

            Skills = new List<int>();
            for (int i = 0; i < skills.Length; i++)
            {
                Skills.Add(int.Parse(skills[i]));
            }

            return base.UnpackFromString(comps[1]);
        }
    }
}
