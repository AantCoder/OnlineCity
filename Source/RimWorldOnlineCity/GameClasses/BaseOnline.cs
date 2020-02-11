using OCUnion;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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

        private Texture2D ColonyOnExpandingTexture;
        private Texture2D ColonyOffExpandingTexture;

        private static Texture2D ColonyOn;

        private static Texture2D ColonyOff;

        private static Texture2D ColonyOnExpanding;

        private static Texture2D ColonyOffExpanding;

        private static WealthTexture[] WealthTexturesOn;
        private static WealthTexture[] WealthTexturesOff;

        private static int[] WealthLevels;

        static BaseOnline()
        {
            ColonyOn = ContentFinder<Texture2D>.Get("ColonyOn");
            ColonyOff = ContentFinder<Texture2D>.Get("ColonyOff");
            ColonyOnExpanding = ContentFinder<Texture2D>.Get("ColonyOnExpanding");
            ColonyOffExpanding = ContentFinder<Texture2D>.Get("ColonyOffExpanding");

            WealthLevels = new int[] { 0, 10_000, 50_000, 100_000, 200_000, 300_000, 500_000, 1_000_000, 2_000_000 };
            WealthTexturesOn = new WealthTexture[WealthLevels.Length];
            WealthTexturesOff = new WealthTexture[WealthLevels.Length];

            for (int i = 0; i < WealthLevels.Length; i++)
            {
                WealthTexturesOn[i] = new WealthTexture() { Wealth = WealthLevels[i], Texture = ContentFinder<Texture2D>.Get("ColonyOnExpanding" + i.ToString()) };
                WealthTexturesOff[i] = new WealthTexture() { Wealth = WealthLevels[i], Texture = ContentFinder<Texture2D>.Get("ColonyOffExpanding" + i.ToString()) };
            }
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
                    if (this.MatColonyOn == null)
                    {
                        Loger.Log("this.MatColonyOn == null");
                    }
                    else
                        Loger.Log("this.MatColonyOn != null");
                    return this.MatColonyOn;
                }

                if (this.MatColonyOff == null) this.MatColonyOff = MaterialPool.MatFrom(ColonyOff
                    , ShaderDatabase.WorldOverlayTransparentLit
                    , Color.white
                    , WorldMaterials.WorldObjectRenderQueue);

                if (this.MatColonyOff == null)
                {
                    Loger.Log("this.MatColonyOff == null");
                }
                else
                    Loger.Log("this.MatColonyOff != null");

                return this.MatColonyOff;
            }
        }

        public override Texture2D ExpandingIcon
        {
            get
            {
                if (IsOnline)
                {
                    if (ColonyOnExpandingTexture == null)
                    {
                        ColonyOnExpandingTexture = GetMaterialByWealth(WealthTexturesOn);
                    }

                    return ColonyOnExpandingTexture;
                }

                if (ColonyOffExpandingTexture == null)
                {
                    ColonyOffExpandingTexture = GetMaterialByWealth(WealthTexturesOff);
                }

                return ColonyOffExpandingTexture;
            }
        }

        private Texture2D GetMaterialByWealth(WealthTexture[] wealthTextures)
        {
            for (int i = 1; i < WealthLevels.Length - 1; i++)
            {
                if (this.OnlineWObject.MarketValue < wealthTextures[i].Wealth)
                {
                    return wealthTextures[i].Texture;
                }
            }

            return wealthTextures[WealthLevels.Length - 1].Texture;
        }

        #endregion
    }

    public class WealthTexture
    {
        public int Wealth;
        public Texture2D Texture;
    }
}
