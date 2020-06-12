using Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OCUnion.Transfer.Model
{
    [Serializable]
    public class AttackCorpse
    {
        public ThingEntry CorpseWithPawn { get; set; }
        public int CorpseId { get; set; }
        public int PawnId { get; set; }
    }
}
