using Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Transfer
{
    [Serializable]
    public class ModelGameServerInfo
    {
        public List<WorldObjectOnline> WObjectOnlineList  { get; set; }
        public List<FactionOnline> FactionOnlineList { get; set; }
    }
}
