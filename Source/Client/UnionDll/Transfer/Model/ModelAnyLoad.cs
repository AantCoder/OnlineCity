using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OCUnion.Transfer.Model
{
    [Serializable]
    public class ModelAnyLoad
    {
        public List<long> Hashs { get; set; }
        public List<string> Datas { get; set; }

    }
}
