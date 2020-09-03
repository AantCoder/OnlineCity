using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace RimWorldOnlineCity
{
    [StaticConstructorOnStartup]
    public class GeneralTexture
    {
        public static readonly Texture2D IconAddTex;
        public static readonly Texture2D IconDelTex;
        public static readonly Texture2D IconSubMenuTex;
        public static readonly Texture2D IconForums;

        static GeneralTexture()
        {
            IconAddTex = ContentFinder<Texture2D>.Get("OCAdd");
            IconDelTex = ContentFinder<Texture2D>.Get("OCDel");
            IconSubMenuTex = ContentFinder<Texture2D>.Get("OCSubMenu");
            IconForums = ContentFinder<Texture2D>.Get("UI/HeroArt/WebIcons/Forums", true);
        }
    }
}
