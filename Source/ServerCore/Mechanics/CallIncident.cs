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
        public static string CreateIncident(PlayerServer player
            , PlayerServer targetPlayer
            , long serverId
            , IncidentTypes? type
            , int mult
            , IncidentArrivalModes? arrivalMode
            , string faction)
        {
            Loger.Log("IncidentLod CallIncident.CreateIncident 1");

            if (!ServerManager.ServerSettings.GeneralSettings.IncidentEnable) return "OC_incidents_IncidentsTurnedOFF";

            if (type == null) return "OC_Incidents_CallIncidents_TypeErr";

            if (targetPlayer == player) return "OC_Incidents_CallIncidebts_selfErr";

            if (player.Public.LastTick / 3600000 < 2) return "OC_Incidents_CallIncidebts_YearErr1";

            if (targetPlayer.Public.LastTick / 3600000 < 2) return "OC_Incidents_CallIncidebts_YearErr2";

            if (player.AllCostWorldObjects() < 100000f) return "OC_Incidents_CallIncidebts_CostErr1";

            if (targetPlayer.AllCostWorldObjects() < 100000f) return "OC_Incidents_CallIncidebts_CostErr2";

            Loger.Log("IncidentLod CallIncident.CreateIncident 2");

            mult = mult > ServerManager.ServerSettings.GeneralSettings.IncidentMaxMult ? ServerManager.ServerSettings.GeneralSettings.IncidentMaxMult : mult;

            if (arrivalMode == null) arrivalMode = IncidentArrivalModes.EdgeWalkIn;

            //формируем пакет
            var packet = new ModelMailStartIncident()
            {
                From = player.Public,
                To = targetPlayer.Public,
                PlaceServerId = serverId,
                NeedSaveGame = true,
                IncidentType = type.Value,
                IncidentArrivalMode = arrivalMode.Value,
                IncidentMult = mult,
                IncidentFaction = faction
            };
            var fPacket = new FMailIncident(packet);

            Loger.Log("Server test call " + type.Value + " " + targetPlayer.Public.Login);

            //проверка на допустимость и добавление инциндента.
            lock (targetPlayer)
            {
                var ownLogin = player.Public.Login;
                var list = targetPlayer.FunctionMails
                    .Where(m => m is FMailIncident)
                    .Cast<FMailIncident>()
                    .Where(m => m.NumberOrder == fPacket.NumberOrder);

                if (list.Count() >= ServerManager.ServerSettings.GeneralSettings.IncidentCountInOffline)
                    return "OC_Incidents_CallIncidents_MaxIncidentsCnt";

                if (list.Count(m => m.Mail.From.Login == ownLogin) >= 1)
                    return "OC_Incidents_CallIncidents_NotShooted";

                //targetPlayer.Mails.Add(packet);
                //Вместо немедленной отправки, делаем это через обработчик отложенной отправки, для паузы между рейдами
                targetPlayer.FunctionMails.Add(fPacket);
            }
            
            Loger.Log("IncidentLod CallIncident.CreateIncident 3");

            return null;
        }

        public static IncidentTypes? ParseIncidentTypes(string arg)
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
                    return IncidentArrivalModes.EdgeWalkIn;
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
                    return "tribe";
            }
        }
    }
}
