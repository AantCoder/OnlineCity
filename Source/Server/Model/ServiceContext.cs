﻿using OCUnion;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ServerOnlineCity.Model
{
    public class ServiceContext
    {
        public PlayerServer Player;

        public Action<Action<SessionServer>> AllSessionAction;

        public string AddrIP;

        /// <summary>
        /// Если при аунтификации не было предоставлено ключа.
        /// С этим статусом можно обновляться, но при при попытке загрузить мир для игры диссконект
        /// </summary>
        public bool PossiblyIntruder;

        /// <summary>
        /// Временное поле для хранения ключей проверки Intruder
        /// </summary>
        public string IntruderKeys;

        public void Logined()
        {
            if (IntruderKeys != null)
            {
                if (Player.IntruderKeys == null) Player.IntruderKeys = "";

                var lks = Player.IntruderKeys.Split(new string[] { "@@@" }, StringSplitOptions.None)
                    .Where(k => k.Length > 3)
                    .ToList();

                var add = IntruderKeys.Split(new string[] { "@@@" }, StringSplitOptions.None)
                    .Where(k => k.Length > 3)
                    .Where(k => !lks.Contains(k))
                    .ToList();

                if (add.Count > 0) Player.IntruderKeys = lks.Union(add).Aggregate((r, k) => r + "@@@" + k);
            }

        }

        public void Disconnect(string logMsg)
        {
            AllSessionAction(session =>
            {
                var sc = session.GetContext();
                if (sc != this) return;

                Loger.Log("Disconnect " + logMsg + " " + Player?.Public?.Login, Loger.LogLevel.LOGIN);
                session.Dispose();
            });
        }

        public void DisconnectLogin(string login, string logMsg)
        {
            AllSessionAction(session =>
            {
                var sc = session.GetContext();
                if (sc?.Player?.Public?.Login == null
                    || sc.Player.Public.Login != login) return;

                Loger.Log("DisconnectLogin " + logMsg + " " + login + " by " + Player?.Public?.Login, Loger.LogLevel.LOGIN);
                session.Dispose();
            });
        }
    }
}
