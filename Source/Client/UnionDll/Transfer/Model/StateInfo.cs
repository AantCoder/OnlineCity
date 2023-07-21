using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OCUnion.Transfer.Model
{
    [Serializable]
    public class StateInfo : State
    {
        public string Head { get; set; }
        public List<string> Players { get; set; }

        public StateInfo()
        {
            Players = new List<string>();
        }

        public StateInfo(State state)
            : this()
        {
            Name = state.Name;
            Color = state.Color;
            Description = state.Description;
        }
    }
}
