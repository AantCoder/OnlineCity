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

            if (!ServerManager.ServerSettings.GeneralSettings.IncidentEnable) return "Инцинденты отключены на этом сервере".NeedTranslate();

            if (type == null) return "OC_Incidents_CallIncidents_TypeErr";

            if (targetPlayer == player) return "Нельзя указывать самого себя".NeedTranslate();

            if (player.Public.LastTick / 3600000 < 2) return "Нападать можно после 2х лет своего развития".NeedTranslate();

            if (targetPlayer.Public.LastTick / 3600000 < 2) return "Нападать можно после 2х лет развития цели нападения".NeedTranslate();

            if (player.AllCostWorldObjects() < 100000f) return "У вас слишком маленькая стоимость поселения".NeedTranslate();

            if (targetPlayer.AllCostWorldObjects() < 100000f) return "У цели нападения слишком маленькая стоимость поселения".NeedTranslate();

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

                if (list.Count() > ServerManager.ServerSettings.GeneralSettings.IncidentCountInOffline)
                    return "OC_Incidents_CallIncidents_MaxIncidentsCnt".NeedTranslate();

                if (list.Count(m => m.Mail.From.Login == ownLogin) > 1)
                    return "Ваш прошлый инциндент для этого игрока ещё не сработал".NeedTranslate();

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
