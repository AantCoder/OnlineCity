using Model;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RimWorldOnlineCity
{
    public abstract class WorldObjectBaseOnline : WorldObject
    {
        public abstract IModelPlace Place { get; }

        public abstract string ExpandingIconName { get; }
    }
}
