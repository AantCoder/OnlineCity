using OCUnion;
using OCUnion.Common;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Transfer;
using Verse;

namespace RimWorldOnlineCity
{
    internal static class ChatController
    {
        private static bool InOnlineGame;

        /// <summary>
        /// Можно инициализировать несколько раз. Сделать в любой мемент после создания SessionClient и до первого вызова PostingChat.
        /// Не имеет смысла, если чат не в игре, т.к. меняет игровые данные
        /// </summary>
        public static void Init(bool inOnlineGame)
        {
            var connect = SessionClient.Get;
            InOnlineGame = inOnlineGame;
            connect.OnPostingChatAfter = After;
            connect.OnPostingChatBefore = Before;
        }

        /// <summary>
        /// Перед отправкой сообщения в игровой чат
        /// </summary>
        /// <param name="chatId"></param>
        /// <param name="msg"></param>
        /// <returns>Если не null, то на сервер не отправляется, а сразу выходит с данным ответом</returns>
        private static ModelStatus Before(int chatId, string msg)
        {
            if (msg.Trim().ToLower().StartsWith("/call"))
            {
                return BeforeStartIncident(chatId, msg);
            }
            //при ошибке вызвать так: return new ModelStatus() { Status = 1 }; //что внтри сейчас никто не проверяет, просто чтоб не null
            return null;
        }

        /// <summary>
        /// После отправки запроса на сервер с ответом от него
        /// </summary>
        /// <param name="chatId"></param>
        /// <param name="msg"></param>
        /// <param name="stat"></param>
        private static void After(int chatId, string msg, ModelStatus stat)
        {
            if (msg.Trim().ToLower().StartsWith("/call"))
            {
                AfterStartIncident(chatId, msg, stat);
            }
        }

        #region For StartIncident
        private static ModelStatus BeforeStartIncident(int chatId, string msg)
        {
            Loger.Log("IncidentLod ChatController.BeforeStartIncident 1 msg:" + msg);
            //разбираем аргументы в кавычках '. Удвоенная кавычка указывает на её символ.
            string command;
            List<string> args;
            ChatUtils.ParceCommand(msg, out command, out args);

            if (args.Count < 3)
            {
                var errorMessage = "Неверно заданы аргументы".NeedTranslate().ToString();
                Loger.Log("IncidentLod ChatController.BeforeStartIncident errorMessage:" + errorMessage);
                Find.WindowStack.Add(new Dialog_Input(errorMessage, msg, true));

                return new ModelStatus() { Status = 1 };
            }

            //проверка, что денег хватает
            int cost = OCIncident.CalculateRaidCost(Int64.Parse(args[2]), Int32.Parse(args[3]));
            int gold = Find.CurrentMap?.resourceCounter.GetCount(ThingDefOf.Gold) ?? -1;
            if (cost < 0 || gold < 0 || gold < cost)
            {
                Loger.Log("IncidentLod ChatController.BeforeStartIncident no");

                var errorMessage = cost < 0 || gold < 0
                    ? "Ошибка определения стоимости".NeedTranslate().ToString() + $" cost={cost} gold={gold}"
                    : "Недостаточно золота {0} из {1}, нехватает {2}".NeedTranslate(gold, cost, cost - gold).ToString();
                Loger.Log("IncidentLod ChatController.BeforeStartIncident errorMessage:" + errorMessage);
                Find.WindowStack.Add(new Dialog_Input(errorMessage, msg, true));

                return new ModelStatus() { Status = 1 };
            }
            else
            {
                Loger.Log("IncidentLod ChatController.BeforeStartIncident ok");
                return null;
            }
        }

        private static void AfterStartIncident(int chatId, string msg, ModelStatus stat)
        {
            Loger.Log("IncidentLod ChatController.AfterStartIncident 1 msg:" + msg);
            if (stat.Status == 0)
            {
                //разбираем аргументы в кавычках '. Удвоенная кавычка указывает на её символ.
                string command;
                List<string> args;
                ChatUtils.ParceCommand(msg, out command, out args);

                if (args.Count < 3)
                {
                    Loger.Log("IncidentLod ChatController.AfterStartIncident 1 error");
                    return;
                }

                //отнимаем нужное кол-во денег(золото или серебро... или что-нибудь ещё)
                int cost = OCIncident.CalculateRaidCost(Int64.Parse(args[2]), Int32.Parse(args[3]));
                List<Thing> things = GameUtils.GetAllThings(Find.CurrentMap);
                foreach(Thing thing in things)
                {
                    if(thing.def == ThingDefOf.Gold)
                    {
                        if (thing.stackCount < cost)
                        {
                            cost -= thing.stackCount;
                            thing.Destroy();
                        }
                        else
                        {
                            thing.SplitOff(cost);
                            break;
                        }
                    }
                }
                Loger.Log("IncidentLod ChatController.AfterStartIncident 2");
                //принудительное сохранение
                if (!SessionClientController.Data.BackgroundSaveGameOff)
                    SessionClientController.SaveGameNow(true);
                Loger.Log("IncidentLod ChatController.AfterStartIncident 3");
            }
            else
            {
                //выводим сообщение с ошибкой
                var errorMessage = string.IsNullOrEmpty(stat.Message) ? "Error call" : stat.Message.Translate().ToString();
                Loger.Log("IncidentLod ChatController.AfterStartIncident errorMessage:" + errorMessage);
                Find.WindowStack.Add(new Dialog_Input(errorMessage, msg, true));
            }
        }
        #endregion

    }
}
