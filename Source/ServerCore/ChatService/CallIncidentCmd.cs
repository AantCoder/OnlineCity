using Model;
using OCUnion;
using OCUnion.Transfer.Types;
using ServerOnlineCity.Model;
using ServerOnlineCity.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using Transfer;
using Transfer.ModelMails;

namespace ServerOnlineCity.ChatService
{
    class CallIncidentCmd : IChatCmd
    {
        public string CmdID => "call";

        //todo: только для модераторов и админов?
        public Grants GrantsForRun => Grants.UsualUser | Grants.SuperAdmin | Grants.Moderator | Grants.DiscordBot;

        public string Help => ChatManager.prefix + "call {raid|caravan|...} {UserLogin} [{params}]";
        //  /call raid Aant 4 air

        private readonly ChatManager _chatManager;

        /// <summary>
        /// Максимум за последний час
        /// </summary>
        private int RaidInHours = 5;

        /// <summary>
        /// Максимум до момента получения игроком накопленных инциндентов
        /// </summary>
        private int RaidInOffline = 3;

        public CallIncidentCmd(ChatManager chatManager)
        {
            _chatManager = chatManager;
        }

        public ModelStatus Execute(ref PlayerServer player, Chat chat, List<string> argsM)
        {
            var ownLogin = player.Public.Login;
            
            //базовая проверка аргументов
            if (argsM.Count < 2)
                return _chatManager.PostCommandPrivatPostActivChat(ChatCmdResult.IncorrectSubCmd, ownLogin, chat,
                    "OC_Incidents_CallIncidents_Err1".NeedTranslate());

            //собираем данные
            IncidentTypes type = IncidentTypes.Raid;
            switch (argsM[0])
            {
                case "raid":
                    type = IncidentTypes.Raid;
                    break;
                case "inf":
                    type = IncidentTypes.Infistation;
                    break;
                //case "bomb":
                //   type = IncidentTypes.Bombing;
                //  break;
                case "acid":
                    type = IncidentTypes.Acid;
                    break;
                default:
                    return _chatManager.PostCommandPrivatPostActivChat(ChatCmdResult.IncorrectSubCmd, ownLogin, chat,
                        "OC_Incidents_CallIncidents_TypeErr".NeedTranslate());
            }

            PlayerServer targetPlayer = Repository.GetPlayerByLogin(argsM[1]);
            if (targetPlayer == null)
            {
                return _chatManager.PostCommandPrivatPostActivChat(ChatCmdResult.UserNotFound, ownLogin, chat, "User " + argsM[1] + " not found");
            }
            if (targetPlayer == player)
            {
                return _chatManager.PostCommandPrivatPostActivChat(ChatCmdResult.IncorrectSubCmd, ownLogin, chat,
                "Нельзя указывать самого себя".NeedTranslate());
            }
            if (player.Public.LastTick / 3600000 < 2)
            {
                return _chatManager.PostCommandPrivatPostActivChat(ChatCmdResult.IncorrectSubCmd, ownLogin, chat,
                "Нападать можно после 2х лет своего развития".NeedTranslate());
            }
            if (targetPlayer.Public.LastTick / 3600000 < 2)
            {
                return _chatManager.PostCommandPrivatPostActivChat(ChatCmdResult.IncorrectSubCmd, ownLogin, chat,
                "Нападать можно после 2х лет развития цели нападения".NeedTranslate());
            }
            if (player.AllCostWorldObjects() < 100000f)
            {
                return _chatManager.PostCommandPrivatPostActivChat(ChatCmdResult.IncorrectSubCmd, ownLogin, chat,
                "У вас слишком маленькая стоимость поселения".NeedTranslate());
            }
            if (targetPlayer.AllCostWorldObjects() < 100000f)
            {
                return _chatManager.PostCommandPrivatPostActivChat(ChatCmdResult.IncorrectSubCmd, ownLogin, chat,
                "У цели нападения слишком маленькая стоимость поселения".NeedTranslate());
            }

            int mult = 1;
            if (argsM.Count > 2)
            {
                mult = Int32.Parse(argsM[2]);
            }
            mult = mult > 10 ? 10 : mult;

            //  walk, random, air
            IncidentArrivalModes arrivalMode = IncidentArrivalModes.EdgeWalkIn;
            if (argsM.Count > 3)
            {
                switch (argsM[3])
                {
                    case "walk":
                        arrivalMode = IncidentArrivalModes.EdgeWalkIn;
                        break;
                    case "random":
                        arrivalMode = IncidentArrivalModes.RandomDrop;
                        break;
                    case "air":
                        arrivalMode = IncidentArrivalModes.CenterDrop;
                        break;
                }
            }

            string faction = null;
            if (argsM.Count > 4) // заменить на enum бы
            {
                switch (argsM[4])
                {
                    case "mech":
                        faction = "mech";
                        break;
                    case "pirate":
                        faction = "pirate";
                        break;
                    case "tribe":
                        faction = "tribe";
                        break;
                    default:
                        break;
                }
            }

            var msg = argsM[0] + " lvl " + mult + " for user " + targetPlayer.Public.Login + " from " + ownLogin;
            _chatManager.AddSystemPostToPublicChat(msg);

            
            //формируем пакет
            var packet = new ModelMailStartIncident();
            packet.From = player.Public;
            packet.To = targetPlayer.Public;
            packet.NeedSaveGame = true;
            packet.IncidentType = type;
            packet.IncidentArrivalMode = arrivalMode;
            packet.IncidentMult = mult;
            packet.IncidentFaction = faction;

            Loger.Log("Server test call " + argsM[0] + " " + targetPlayer.Public.Login);

            //проверка на допустимость и добавление инциндента. Возможно подобную проверку делать при добавлении инциндента из любого места
            lock (targetPlayer)
            {
                if (targetPlayer.Mails.Count(m => m is ModelMailStartIncident && m.From.Login == ownLogin) > 1)
                    return _chatManager.PostCommandPrivatPostActivChat(ChatCmdResult.IncorrectSubCmd, ownLogin, chat,
                        "Ваш прошлый инциндент для этого игрока ещё не сработал".NeedTranslate());
                
                /* // для тестов:
                var now = DateTime.UtcNow;
                if (targetPlayer.LastIncidents.Count > 0)
                {
                    targetPlayer.LastIncidents = targetPlayer.LastIncidents.Where(i => (now - i).TotalHours < 1).ToList();
                    if (targetPlayer.LastIncidents.Count >= RaidInHours)
                        return _chatManager.PostCommandPrivatPostActivChat(ChatCmdResult.IncorrectSubCmd, ownLogin, chat,
                            "Достигнуто максимальное количество инциндентов для этого игрока за час".NeedTranslate());
                }
                if (targetPlayer.Mails.Count(m => m is ModelMailStartIncident) > RaidInOffline)
                    return _chatManager.PostCommandPrivatPostActivChat(ChatCmdResult.IncorrectSubCmd, ownLogin, chat,
                        "Достигнуто максимальное количество инциндентов для этого игрока".NeedTranslate());
                targetPlayer.LastIncidents.Add(now);
                */
                targetPlayer.Mails.Add(packet);
            }

            return new ModelStatus() { Status = 0 };
        }
    }
}
