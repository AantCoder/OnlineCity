using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Transfer.ModelMails
{

    [Serializable]
    public class ModelMailDeleteWO : ModelMail, IModelMailPlace
    {
        public int Tile { get; set; }
        public long PlaceServerId { get; set; }

        public override string GetHash()
        {
            return $"T{Tile}P{PlaceServerId}";
        }
    }
}
