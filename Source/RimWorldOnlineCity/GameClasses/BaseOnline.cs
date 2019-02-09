using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;

namespace RimWorldOnlineCity
{
    [StaticConstructorOnStartup]
    public class BaseOnline : CaravanOnline
    {
        #region Icons
        private Material MatColonyOn;

        private Material MatColonyOff;

        private static Texture2D ColonyOn;

        private static Texture2D ColonyOff;

        private static Texture2D ColonyOnExpanding;

        private static Texture2D ColonyOffExpanding;

        static BaseOnline()
        {
            ColonyOn = ContentFinder<Texture2D>.Get("ColonyOn");
            ColonyOff = ContentFinder<Texture2D>.Get("ColonyOff");
            ColonyOnExpanding = ContentFinder<Texture2D>.Get("ColonyOnExpanding");
            ColonyOffExpanding = ContentFinder<Texture2D>.Get("ColonyOffExpanding");
        }

        public override Material Material
        {
            get
            {
                if (IsOnline)
                {
                    if (this.MatColonyOn == null) this.MatColonyOn = MaterialPool.MatFrom(ColonyOn
                        , ShaderDatabase.WorldOverlayTransparentLit
                        , Color.white
                        , WorldMaterials.WorldObjectRenderQueue);
                    //MaterialPool.MatFrom(ColonyOn);
                    //MaterialPool.MatFrom(base.Faction.def.homeIconPath, ShaderDatabase.WorldOverlayTransparentLit, base.Faction.Color, WorldMaterials.WorldObjectRenderQueue);
                    return this.MatColonyOn;
                }
                else
                {
                    if (this.MatColonyOff == null) this.MatColonyOff = MaterialPool.MatFrom(ColonyOff
                        , ShaderDatabase.WorldOverlayTransparentLit
                        , Color.white
                        , WorldMaterials.WorldObjectRenderQueue);
                    return this.MatColonyOff;
                }
            }
        }

        public override Texture2D ExpandingIcon
        {
            get
            {
                return IsOnline ? ColonyOnExpanding : ColonyOffExpanding;
            }
        }
        #endregion
        /*
        private Material cachedMat;
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
        */
    }
}
