using OCUnion;
using ServerOnlineCity.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Transfer.ModelMails;

namespace ServerOnlineCity.Mechanics
{
    public static class CallIncident
    {
        /// <summary>
        /// Максимум за последний час
        /// </summary>
        private static int RaidInHours = 5;

        /// <summary>
        /// Максимум до момента получения игроком накопленных инциндентов
        /// </summary>
        private static int RaidInOffline = 3;

        public static string CreateIncident(PlayerServer player
            , PlayerServer targetPlayer
            , IncidentTypes? type
            , int mult
            , IncidentArrivalModes? arrivalMode
            , string faction)
        {
            if (type == null) return "OC_Incidents_CallIncidents_TypeErr";

            if (targetPlayer == player) return "Нельзя указывать самого себя".NeedTranslate();
            
            if (player.Public.LastTick / 3600000 < 2) return "Нападать можно после 2х лет своего развития".NeedTranslate();
            
            if (targetPlayer.Public.LastTick / 3600000 < 2) return "Нападать можно после 2х лет развития цели нападения".NeedTranslate();
            
            if (player.AllCostWorldObjects() < 100000f) return "У вас слишком маленькая стоимость поселения".NeedTranslate();
            
            if (targetPlayer.AllCostWorldObjects() < 100000f) return "У цели нападения слишком маленькая стоимость поселения".NeedTranslate();

            mult = mult > 10 ? 10 : mult;

            if (arrivalMode == null) arrivalMode = IncidentArrivalModes.EdgeWalkIn;

            //формируем пакет
            var packet = new ModelMailStartIncident();
            packet.From = player.Public;
            packet.To = targetPlayer.Public;
            packet.NeedSaveGame = true;
            packet.IncidentType = type.Value;
            packet.IncidentArrivalMode = arrivalMode.Value;
            packet.IncidentMult = mult;
            packet.IncidentFaction = faction;

            Loger.Log("Server test call " + type.Value + " " + targetPlayer.Public.Login);

            //проверка на допустимость и добавление инциндента.
            lock (targetPlayer)
            {
                var ownLogin = player.Public.Login;

                //if (targetPlayer.Mails.Count(m => m is ModelMailStartIncident && m.From.Login == ownLogin) > 1)
                if (targetPlayer.FunctionMails.Count(m => (m as FMailIncident)?.Mail.From.Login == ownLogin) > 1)
                    return "Ваш прошлый инциндент для этого игрока ещё не сработал".NeedTranslate();

                /* todo
                var now = DateTime.UtcNow;
                if (targetPlayer.LastIncidents.Count > 0)
                {
                    targetPlayer.LastIncidents = targetPlayer.LastIncidents.Where(i => (now - i).TotalHours < 1).ToList();
                    if (targetPlayer.LastIncidents.Count >= RaidInHours)
                        return "OC_Incidents_CallIncidents_MaxIncidentsInHour".NeedTranslate();
                }
                if (targetPlayer.Mails.Count(m => m is ModelMailStartIncident) > RaidInOffline)
                    return "OC_Incidents_CallIncidents_MaxIncidentsCnt".NeedTranslate();

                targetPlayer.LastIncidents.Add(now);
                */

                //targetPlayer.Mails.Add(packet);
                //Вместо немедленной отправки, делаем это через обработчик отложенной отправки, для паузы между рейдами
                targetPlayer.FunctionMails.Add(new FMailIncident() { Mail = packet });
            }

            return null;
        }

        internal static IncidentTypes? ParseIncidentTypes(string arg)
        {
            switch (arg.ToLower().Trim())
            {
                case "raid":
                    return IncidentTypes.Raid;
                case "inf":
                    return IncidentTypes.Infistation;
                //case "bomb":
                //  return IncidentTypes.Bombing;
                case "acid":
                    return IncidentTypes.Acid;
                default:
                    return null;
            }
        }

        public static IncidentArrivalModes? ParseArrivalMode(string arg)
        {
            switch (arg.ToLower().Trim())
            {
                case "random":
                    return IncidentArrivalModes.RandomDrop;
                case "air":
                    return IncidentArrivalModes.CenterDrop;
                case "walk":
                    return IncidentArrivalModes.EdgeWalkIn;
                default:
                    return null;
            }
        }

        public static string ParseFaction(string arg)
        {
            switch (arg.ToLower().Trim())
            {
                case "mech":
                    return "mech";
                case "pirate":
                    return "pirate";
                case "tribe":
                    return "tribe";
                default:
                    return null;
            }
        }
    }
}
