using Model;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Transfer;
using Verse;

namespace RimWorldOnlineCity
{
    public class ClientData : OCUnion.ClientData
    {
        /// <summary>
        /// Если не null, значит сейчас режим атаки на другое поселение online
        /// </summary>
        public GameAttacker AttackModule = null;

        /// <summary>
        /// Если не null, значит сейчас режим атаки кого-то на наше поселение online
        /// </summary>
        public GameAttackHost AttackUsModule = null;

        public Dictionary<string, PlayerClient> Players = new Dictionary<string, PlayerClient>();

        public static bool UIInteraction = false; //говорят уведомления слева сверху мешают, поэтому выключено (можно сделать настройку если кому надо будет)

        public ClientData(string myLogin) : base(myLogin) { }

        public Faction FactionPirate
        {
            get
            {
                if (FactionPirateData == null)
                    FactionPirateData = Find.FactionManager.AllFactions.FirstOrDefault(f => f.def.defName == "Pirate")
                    ?? Find.FactionManager.OfAncientsHostile;
                return FactionPirateData;
            }
        }

        private Faction FactionPirateData = null;

        public bool ApplyChats(ModelUpdateChat updateDate)
        {
            string newStr = string.Empty;
            bool result = base.ApplyChats(updateDate, ref newStr);

            if (result && UIInteraction)
            {
                if (newStr.Length > 50) newStr = newStr.Substring(0, 49) + "OCity_ClientData_ChatDot".Translate();
                Messages.Message("OCity_ClientData_Chat".Translate() + newStr, MessageTypeDefOf.NeutralEvent);
            }

            return result;
        }
    }
}
