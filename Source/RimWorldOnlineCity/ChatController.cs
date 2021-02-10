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
            string error;
            OCIncident.GetCostOnGameByCommand(msg, true, out error);

            if (error != null)
            {
                Loger.Log("IncidentLod ChatController.BeforeStartIncident errorMessage:" + error);
                Find.WindowStack.Add(new Dialog_MessageBox(error));

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
                string error;
                var result = OCIncident.GetCostOnGameByCommand(msg, false, out error);
                //Find.WindowStack.Add(new Dialog_Input("Вызов инциндента", error ?? result, true));
                Find.WindowStack.Add(new Dialog_MessageBox(error ?? result));
            }
            else
            {
                //выводим сообщение с ошибкой
                var errorMessage = string.IsNullOrEmpty(stat.Message) ? "Error call" : stat.Message.Translate().ToString();
                Loger.Log("IncidentLod ChatController.AfterStartIncident errorMessage:" + errorMessage);
                Find.WindowStack.Add(new Dialog_MessageBox(errorMessage));
            }
        }
        #endregion

    }
}
