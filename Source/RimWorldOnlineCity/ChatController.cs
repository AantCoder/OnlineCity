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
        private static ModelStatus After(int chatId, string msg)
        {
            if (msg.Trim().ToLower().StartsWith("/call"))
            {
                return AfterStartIncident(chatId, msg);
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
        private static void Before(int chatId, string msg, ModelStatus stat)
        {
            if (msg.Trim().ToLower().StartsWith("/call"))
            {
                BeforeStartIncident(chatId, msg, stat);
            }
        }

        #region For StartIncident
        private static ModelStatus AfterStartIncident(int chatId, string msg)
        {
            //разбираем аргументы в кавычках '. Удвоенная кавычка указывает на её символ.
            string command;
            List<string> args;
            ChatUtils.ParceCommand(msg, out command, out args);

            //todo: проверка, что денег хватает
            int gold = Find.CurrentMap.resourceCounter.GetCount(ThingDefOf.Gold);
            if (gold < 100500) return new ModelStatus() { Status = 1 };
            else return null;
        }

        private static void BeforeStartIncident(int chatId, string msg, ModelStatus stat)
        {
            if (stat.Status == 0)
            {
                //разбираем аргументы в кавычках '. Удвоенная кавычка указывает на её символ.
                string command;
                List<string> args;
                ChatUtils.ParceCommand(msg, out command, out args);

                //todo: отнимаем нужное кол-во денег

                //принудительное сохранение
                SessionClientController.SaveGameNow(true);
            }
            else
            {
                //выводим сообщение с ошибкой
                var errorMessage = string.IsNullOrEmpty(stat.Message) ? "Error call" : stat.Message.Translate().ToString();
                Find.WindowStack.Add(new Dialog_Input(errorMessage, msg, true));
            }
        }

        #endregion

    }
}
