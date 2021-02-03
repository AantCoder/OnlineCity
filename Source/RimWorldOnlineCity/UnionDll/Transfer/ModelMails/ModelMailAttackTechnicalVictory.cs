using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Transfer.ModelMails
{

    [Serializable]
    public class ModelMailAttackTechnicalVictory : ModelMail
    {
        public override string GetHash()
        {
            return "NotContent";
        }
    }
}
