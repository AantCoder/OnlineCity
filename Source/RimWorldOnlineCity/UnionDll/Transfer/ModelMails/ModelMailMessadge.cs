using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using RimWorld;

namespace Transfer.ModelMails
{

    [Serializable]
    public class ModelMailMessadge : ModelMail
    {
        public string label;
        public string text;
        public LetterDef def = LetterDefOf.NeutralEvent;
        public override string GetHash()
        {
            return "NotContent";
        }
    }
}