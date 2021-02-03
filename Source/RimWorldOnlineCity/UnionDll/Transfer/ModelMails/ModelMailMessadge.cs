using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Transfer.ModelMails
{

    [Serializable]
    public class ModelMailMessadge : ModelMail
    {
        public string label;
        public string text;
        public MessadgeTypes type = MessadgeTypes.Neutral;
        public override string GetHash()
        {
            return "NotContent";
        }

        public enum MessadgeTypes
        {
            ThreatBig,
            ThreatSmall,
            Negative,
            Neutral,
            Positive,
            Death,
            Visitor,
        }
    }
}