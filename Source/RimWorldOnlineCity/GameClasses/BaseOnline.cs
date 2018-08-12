using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;

namespace RimWorldOnlineCity
{
    public class BaseOnline : CaravanOnline
    {
        private Material cachedMat;
        /*
        public override string Label
        {
            get
            {
                
                return "Послеление {0}".Translate(new object[]
                    {
                        OnlinePlayerLogin + " " + OnlineName
                    });
                    
            }
        }
        
        public override string GetInspectString()
        {
            return "Поселение игрока {0}".Translate(new object[]
                    {
                        OnlinePlayerLogin + " " + OnlineName
                    });
        }
        */

        public override Texture2D ExpandingIcon
        {
            get
            {
                return base.Faction.def.ExpandingIconTexture;
            }
        }

        public override Material Material
        {
            get
            {
                if (this.cachedMat == null)
                {
                    this.cachedMat = MaterialPool.MatFrom(base.Faction.def.homeIconPath
                        , ShaderDatabase.WorldOverlayTransparentLit
                        , Color.blue
                        , WorldMaterials.WorldObjectRenderQueue);
                }
                return this.cachedMat;
            }
        }

    }
}
