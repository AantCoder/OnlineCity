using Model;
using OCUnion;
using OCUnion.Common;
using RimWorld;
using RimWorld.Planet;
using RimWorldOnlineCity.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Transfer;
using UnityEngine;
using Verse;

namespace RimWorldOnlineCity
{
    internal static class ChatController
    {
        private static bool InOnlineGame;
        internal static PanelChat MainPanelChat;

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

        public static void AddToInputChat(string text, bool activeChat = false)
        {
            if (activeChat) Dialog_MainOnlineCity.ShowChat();
            if (MainPanelChat == null) return;
            if (MainPanelChat.ChatInputText == null) MainPanelChat.ChatInputText = "";
            MainPanelChat.ChatInputText += (MainPanelChat.ChatInputText.Length == 0 || MainPanelChat.ChatInputText[MainPanelChat.ChatInputText.Length - 1] == ' ' ? "" : " ")
                + text;
        }
        public static string ServerTranslate(this string textChat, bool onlyTranslate = false)
            => ServerCharTranslate(textChat, onlyTranslate);

        public static string ServerCharTranslate(string textChat, bool onlyTranslate = false)
        {
            if (!onlyTranslate) textChat = PrepareShortTag(textChat);

            int pos = 0;
            var clonSpace = textChat.Replace('\r', ' ').Replace('\n', ' ').Replace('\t', ' ').Replace(',', ' ').Replace('.', ' ')
                .Replace(':', ' ').Replace('*', ' ') + " ";
            while ((pos = textChat.IndexOf("OC_", pos)) >= 0)
            {
                var ep = clonSpace.IndexOf(" ", pos);
                var sub = textChat.Substring(pos, ep - pos);
                var tr = sub.Translate().ToString();
                if (!tr.StartsWith("OC_")) //если перевод удался
                {
                    clonSpace = clonSpace.Replace(sub, tr);
                    textChat = textChat.Replace(sub, tr);
                    pos += tr.Length;
                }
                else
                {
                    pos++;
                }
            }

            return textChat;
        }

        public static string PrepareShortTag(string textChat)
        {
            var current = 0;
            while (true)
            {
                var pos = textChat.IndexOf("<", current); // 1<!2/>3
                if (pos < 0 || textChat.Length < pos + 4) break;
                current = pos + 1;
                var posE = textChat.IndexOf(">", pos);
                if (posE < 0 || posE - pos < 3) continue;
                //содержимое тэга, без <>, без конечной / и без первого символа содержимого
                //Loger.Log($"PrepareShortTag {pos} {posE} {textChat}");
                var content = textChat[posE - 1] != '/'  // 1<!2/>3 => 2
                    ? textChat.Substring(pos + 2, posE - pos - 2)
                    : textChat.Substring(pos + 2, posE - pos - 3);

                //если это коментарий
                if (textChat.Substring(pos + 1, 2) == "!-") continue;
                //обработка в зависимости от первого кодового знака, если его нет, то на обычный тэг не реагируем
                string replace = null;
                switch (textChat[pos + 1])
                {
                    case ':': replace = ShortTagEmoji(content); break;
                    case '@': replace = ShortTagPlayer(content); break;
                    case '#': replace = ShortTagTile(content); break;
                    case '!': replace = ShortTagDef(content); break;
                    case '&': replace = ShortTagServerId(content); break;
                }
                if (replace != null)
                {
                    textChat = textChat.Substring(0, pos) + replace + textChat.Substring(posE + 1);
                }
            }
            return textChat;
        }
        private static string ShortTagEmoji(string content)
        {
            if (content.EndsWith(":")) content = content.Substring(0, content.Length - 1);
            content = content.Trim();
            return $"<img Emoji/Emoji_{content}>";
        }
        private static string ShortTagPlayer(string content)
        {
            content = content.Trim();
            //todo Иконка игрока <Клик для инфо> Иконка клана <Клик для инфо> Имя игрока (Имя клана) <Клик для добавления в чат>
            return $"<btn name=pl{content} class=player arg={content}><img pl_{content}> " + content + "</btn>";
        }
        private static string ShortTagTile(string content)
        {
            content = content.Trim();
            if (!int.TryParse(content, out int tile)) return null;
            if (tile < 0 || tile >= Find.WorldGrid.tiles.Count) return null;

            // Получаем строку с "координаты название (холмистость)"
            var biome = Find.WorldGrid[tile].biome;
            Vector2 vector = Find.WorldGrid.LongLatOf(tile);
            var coor = vector.y.ToStringLatitude() + " " + vector.x.ToStringLongitude();
            var msg = $"<img name=Waypoint />{coor} <l>{biome.defName}.label</l>";
            if (!biome.impassable)
            {
                msg += $" (<l>{GameUtils.GetHillinessLabel(Find.WorldGrid[tile].hilliness)}</l>)";
            }

            return $"<btn name=tile{tile} class=tile d={tile} arg={tile}>" + msg + "</btn>";
        }
        private static string ShortTagDef(string content)
        {
            content = content.Trim();
            //todo клик с инфой и подсказка
            return (content == "Human" ? $"<img IconHuman />" : $"<img defName={content} />")
                + $"<l>{content}.label</l>";
        }
        private static string ShortTagServerId(string content)
        {
            content = content.Trim();
            if (!int.TryParse(content, out int serverId)) return null;

            var tile = 0;
            string player = null;
            var msg = $"<img name=Waypoint />";
            var myObj = UpdateWorldController.GetMyByServerId(serverId);
            if (myObj != null)
            {
                tile = myObj.Tile;
                player = SessionClientController.My.Login;
                msg += "<img ColonyOnExpanding> " + myObj.Name;
            }
            else
            {
                var otherObj = UpdateWorldController.GetOtherByServerIdDirtyRead(serverId);
                if (otherObj == null) return null;

                tile = otherObj.Tile;
                var bo = otherObj as CaravanOnline;
                if (bo != null) player = bo.OnlinePlayerLogin;
                msg += $"<img name={otherObj.ExpandingIconName} /> " + otherObj.LabelCap;
            }

            return $"<btn name=tile&{serverId} class=tile d={tile} arg=&{serverId}>" + msg + "</btn>"
                + (player == null ? "" : " " + ShortTagPlayer(player));
            //return content;
        }


        /// <summary>
        /// Перед отправкой сообщения в игровой чат
        /// </summary>
        /// <param name="chatId"></param>
        /// <param name="msg"></param>
        /// <returns>Если не null, то на сервер не отправляется, а сразу выходит с данным ответом</returns>
        private static ModelStatus Before(int chatId, string msg)
        {
            var command = msg.Trim().ToLower();
            if (command.StartsWith("/call"))
            {
                return BeforeStartIncident(chatId, msg);
            }
            if (command.StartsWith("/debug"))
            {
                return Debug(chatId, msg);
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

        #region For Debug
        private static Dictionary<int, Dictionary<string, int>> AllThingsByMaps = null;

        private static void DebugGetThingsByWO(List<Thing> allt, Dictionary<int, Dictionary<string, int>> newThingsByMaps
            , WorldObject worldObject, string local, bool needSave, bool needDiffNew, bool needDiffOld
            , Dictionary<string, int> newThings = null)
        {
            if (newThings == null)
            {
                newThings = GameUtils.TransferableOneWaysToDictionary(GameUtils.DistinctToTransferableOneWays(allt), true)
                    //Вещи всё равно разбиваются на разные стаки, объединяем их по признаку из ToString() + PawnParam
                    //который похож на сравнение MatchesThingTrade
                    .GroupBy(p =>
                    {
                        var tt = ThingTrade.CreateTrade(p.Key, 1);
                        var stringKey = tt.ToString().Replace(" conc", "") + " " + tt.PawnParam;
                        return stringKey.Trim();
                    })
                    .ToDictionary(g => g.Key, g => g.Sum(p => p.Value));
                newThingsByMaps.Add(worldObject.ID, newThings);
            }

            Dictionary<string, int> things;
            if (needDiffNew)
            {
                var oldThings = AllThingsByMaps.ContainsKey(worldObject.ID)
                    ? AllThingsByMaps[worldObject.ID]
                    : new Dictionary<string, int>();
                things = newThings
                    .Select(p => new { p.Key, Value = p.Value - (oldThings.ContainsKey(p.Key) ? oldThings[p.Key] : 0) })
                    .Where(p => p.Value > 0)
                    .ToDictionary(p => p.Key, p => p.Value);
            }
            else if (needDiffOld)
            {
                var oldThings = AllThingsByMaps.ContainsKey(worldObject.ID)
                    ? AllThingsByMaps[worldObject.ID]
                    : new Dictionary<string, int>();
                things = oldThings
                    .Select(p => new { p.Key, Value = p.Value - (newThings.ContainsKey(p.Key) ? newThings[p.Key] : 0) })
                    .Where(p => p.Value > 0)
                    .ToDictionary(p => p.Key, p => p.Value);
            }
            else
            {
                things = newThings;
            }

            var serverId = UpdateWorldController.GetMyByLocalId(worldObject?.ID ?? 0)?.PlaceServerId;
            Loger.Log($"Debug {local} {{ " +
                $"woID={worldObject?.ID} " +
                $"ServerId={serverId} " +
                $"things={things.Count} " +
                $"cntThings={things.Values.Sum()} " +
                $"allPawns={things.Keys.Where(t => t.Contains("gender:")).Count()} " +
                $"pawns={things.Keys.Where(t => t.Contains("Colonist")).Count()} " +
                $"Name={worldObject?.LabelShortCap}");

            if (!needSave || needDiffNew || needDiffOld)
                foreach (var thing in things
                    .OrderBy(t => (t.Key.Contains("gender:") ? "0" : "1") + (t.Key.Contains("Colonist") ? "0" : "1") + t.Key))
                {
                    Loger.Log("Debug    " + thing.Key.ToString() + " x" + thing.Value.ToString());
                }
            Loger.Log($"Debug {local} }}");
        }
        /// <summary>
        /// Выводим в логи тестовую информацию
        /// </summary>
        private static ModelStatus Debug(int chatId, string msg)
        {
            var needSave = msg.ToLower().Contains("save");
            var needDiffNew = msg.ToLower().Contains("new");
            var needDiffOld = msg.ToLower().Contains("old");
            Loger.Log("Debug start {{{ " + (needDiffOld ? "diffOld" : "") + (needDiffNew ? "diffNew" : "") + (needSave ? "save" : ""));
            try
            {
                if ((needDiffNew || needDiffOld) && AllThingsByMaps == null)
                {
                    Loger.Log("Debug diff fail - not save list");
                    Loger.Log("Debug finish }}}");
                }
                var newThingsByMaps = new Dictionary<int, Dictionary<string, int>>();

                var allWorldObjects = GameUtils.GetAllWorldObjects();
                var wObjects = allWorldObjects
                        .Where(o => (o.Faction?.IsPlayer ?? false) //o.Faction != null && o.Faction.IsPlayer
                            && (o is Settlement || o is Caravan)) //Чтобы отсеч разные карты событий
                        .ToList();

                for (int i = 0; i < wObjects.Count; i++)
                {
                    if (!(wObjects[i] is Settlement)) continue;
                    var m = (wObjects[i] as Settlement).Map;
                    if (!m.IsPlayerHome) continue;
                    var worldObject = m.Parent;

                    var allt = GameUtils.GetAllThings(m, true, false);
                    DebugGetThingsByWO(allt, newThingsByMaps, worldObject, "Map", needSave, needDiffNew, needDiffOld);
                }
                for (int i = 0; i < wObjects.Count; i++)
                {
                    if (!(wObjects[i] is Caravan)) continue;
                    var worldObject = (wObjects[i] as Caravan);

                    var allt = GameUtils.GetAllThings(worldObject, true, false);
                    DebugGetThingsByWO(allt, newThingsByMaps, worldObject, "Caravan", needSave, needDiffNew, needDiffOld);
                }
                //выводим те, которые исчезли
                if (needDiffOld)
                {
                    var removed = AllThingsByMaps.Keys.Where(k => !wObjects.Any(wo => wo.ID == k)).ToList();
                    foreach (var woID in removed)
                    {
                        DebugGetThingsByWO(null, null, null, "removed WorldObject", false, false, false, AllThingsByMaps[woID]);
                    }
                }

                if (needSave) AllThingsByMaps = newThingsByMaps;
            }
            catch (Exception exp)
            {
                Loger.Log("Debug Exception " + exp.ToString());
            }
            Loger.Log("Debug finish }}}");

            return new ModelStatus() { Status = 1 };
        }
        #endregion
        #region For StartIncident
        private static ModelStatus BeforeStartIncident(int chatId, string msg)
        {
            Loger.Log("IncidentLod ChatController.BeforeStartIncident 1 msg:" + msg);
            string error;
            OCIncident.GetCostOnGameByCommand(msg, false, out error);
            if (error != null)
            {
                Loger.Log("IncidentLod ChatController.BeforeStartIncident errorMessage:" + error, Loger.LogLevel.ERROR);
                Find.WindowStack.Add(new Dialog_MessageBox(error));

                return new ModelStatus() { Status = 1 };
            }
            else
            {
                Loger.Log("IncidentLod ChatController.BeforeStartIncident ok");
                return new ModelStatus() { Status = 1 }; //эта команда на сервер не уходит в любом случае, всё решается в OCIncident.GetCostOnGameByCommand
            }
        }

        private static void AfterStartIncident(int chatId, string msg, ModelStatus stat)
        {
            Loger.Log("IncidentLod ChatController.AfterStartIncident Error call incident!", Loger.LogLevel.ERROR);
            Find.WindowStack.Add(new Dialog_MessageBox("Error call incident"));
        }
        #endregion

    }
}
