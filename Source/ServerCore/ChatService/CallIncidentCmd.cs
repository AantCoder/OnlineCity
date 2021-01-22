using Model;
using OCUnion;
using OCUnion.Transfer.Types;
using ServerOnlineCity.Model;
using ServerOnlineCity.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using Transfer;

namespace ServerOnlineCity.ChatService
{
    class CallIncidentCmd : IChatCmd
    {
        public string CmdID => "call";

        //todo: только для модераторов и админов?
        public Grants GrantsForRun => Grants.UsualUser | Grants.SuperAdmin | Grants.Moderator | Grants.DiscordBot;

        public string Help => ChatManager.prefix + "call {raid|caravan|...} {UserLogin} [{params}]";

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
                    "Укажите тип инциндента и игрока, для некоторых действий возможны дополнительные параметры".NeedTranslate());

            switch (argsM[0])
            {
                case "raid":
                case "caravan":
                    break;
                default:
                    return _chatManager.PostCommandPrivatPostActivChat(ChatCmdResult.IncorrectSubCmd, ownLogin, chat,
                        "Укажите допустимый тип инциндента".NeedTranslate());
            }

            //собираем данные
            PlayerServer targetPlayer = Repository.GetPlayerByLogin(argsM[1]);

            int mult = 1;
            if(argsM.Count > 2)
            {
                mult = Int32.Parse(argsM[2]);
            }
            mult = mult > 10 ? 10 : mult;

            if (targetPlayer == null)
            {
                return _chatManager.PostCommandPrivatPostActivChat(ChatCmdResult.UserNotFound, ownLogin, chat, "User " + argsM[1] + " not found");
            }

            //проверка на платёжеспособность
            var paymentPacket = new ModelMailTrade();
            paymentPacket.To = player.Public;
            paymentPacket.RaidType = RaidTypes.Raid;
            paymentPacket.RaidMult = mult;
            paymentPacket.isCustomer = true;
            player.Mails.Add(paymentPacket);

            var msg = argsM[0] + " lvl " + mult + " for user " + targetPlayer.Public.Login + " from " + ownLogin;
            _chatManager.AddSystemPostToPublicChat(msg);

            //формируем пакет
            var packet = new ModelMailTrade();
            packet.Type = ModelMailTradeType.StartEvent;
            packet.To = targetPlayer.Public;
            packet.RaidType = RaidTypes.Raid;
            packet.RaidMult = mult;
            packet.isCustomer = false;
            //todo use Raid*

            Loger.Log("Server test call " + argsM[0] + " " + targetPlayer.Public.Login);
            
            //проверка на допустимость и добавление инциндента. Возможно подобную проверку делать при добавлении инциндента из любого места
            lock (targetPlayer)
            {
                var now = DateTime.UtcNow;
                if (targetPlayer.LastIncidents.Count > 0)
                {
                    targetPlayer.LastIncidents = targetPlayer.LastIncidents.Where(i => (now - i).TotalHours < 1).ToList();
                    if (targetPlayer.LastIncidents.Count >= RaidInHours)
                        return _chatManager.PostCommandPrivatPostActivChat(ChatCmdResult.IncorrectSubCmd, ownLogin, chat,
                            "Достигнуто максимальное количество инциндентов для этого игрока за час".NeedTranslate());
                }

                if (targetPlayer.Mails.Count(m => m.Type == ModelMailTradeType.StartEvent) > RaidInOffline)
                    return _chatManager.PostCommandPrivatPostActivChat(ChatCmdResult.IncorrectSubCmd, ownLogin, chat,
                        "Достигнуто максимальное количество инциндентов для этого игрока".NeedTranslate());

                targetPlayer.Mails.Add(packet);
                targetPlayer.LastIncidents.Add(now);
            }

            return new ModelStatus() { Status = 0 };
        }
    }
}
