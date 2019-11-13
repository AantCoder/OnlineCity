using Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Transfer
{
    [Serializable]
    public class ModelCreateWorld
    {
        public string Seed { get; set; }
        public int Difficulty { get; set; }
        public int MapSize { get; set; }
        public float PlanetCoverage { get; set; }
        public List<WorldObjectEntry> WObjects { get; set; }
    }
}
