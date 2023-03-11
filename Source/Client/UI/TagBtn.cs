using Model;
using OCUnion.Transfer.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using OCUnion;
using UnityEngine;
using RimWorld.Planet;

namespace RimWorldOnlineCity.UI
{
    public class TagBtn
    {
        /// <summary>
        /// Имя в тэге
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Событие нажатия
        /// </summary>
        public Action<string> ActionClick { get; set; }

        /// <summary>
        /// Каждый фрейм когда мышка над компонентом
        /// </summary>
        public Action<string> ActionIsOver { get; set; }

        /// <summary>
        /// Подсветить при наведении
        /// </summary>
        public bool HighlightIsOver { get; set; }

        /// <summary>
        /// Всплывающая подсказка
        /// </summary>
        public string Tooltip { get; set; }

        public static string GetTagByObject(int tile)
        {
            return $"<btn name=tile{tile} class=tile d={tile} arg={tile}><img Waypoint></btn>";
        }

        public static string GetTagByObject(string playerLogin)
        {
            return $"<btn name=pl{playerLogin} class=player arg={playerLogin}><img ColonyOnExpanding> {playerLogin}</btn>";
        }

        public static string GetTagByObject(Thing thing, bool withText)
        {
            var pawn = thing as Pawn;
            ThingTrade thingTrade;
            if (pawn == null)
            {
                thingTrade = ThingTrade.CreateTrade(thing, thing.stackCount, false);
            }
            else
            {
                thingTrade = PawnStat.CreateTrade(pawn);
            }

            return GetTagByObject(thingTrade, withText);
        }

        /// <summary>
        /// Если это пешка, то будет сокращенная информация (по сути только имя)
        /// </summary>
        public static string GetTagByObject(ThingTrade thingTrade, bool withText)
        {
            var pack = thingTrade.PackToString();

            return $"<btn name=thing{new System.Random().Next(10000000, 99999999)}" +
                $" class=thing d={pack.Replace(" ", "*")}>" +
                (!string.IsNullOrEmpty(thingTrade.PawnParam) && thingTrade.PawnParam.Contains("Human") 
                    ? $"<img IconHuman>" 
                    : $"<img defName={thingTrade.DefName}>") +
                (withText ? thingTrade.LabelTextShort : "") +
                $"</btn>";
        }

        /// <summary>
        /// Создание настроенного TagBtn в зависимости от тэга class (и d), если он есть.
        /// Возможно наличие тэга d c данными (могут быть буквы, цифры, пробел (заменяется на *), тире, запятые, двоеточие)
        /// </summary>
        /// <param name="value">Возможные значения тэга class:
        /// tile - открывает планету и выбирает тайл с номером в d,
        /// player - открывает инфу об игроке с его именем в d,
        /// thing - открывает инфу о предмете, в d содержится ThingTrade.PackToString
        /// </param>
        /// <returns></returns>
        public static TagBtn GetByClass(string value, string d, string arg)
        {
            var btn = new TagBtn() { HighlightIsOver = true };
            try
            {
                if (value == "tile")
                {
                    if (!int.TryParse(d, out var tile)) return btn;

                    var tileLabel = Find.WorldGrid[tile].biome.LabelCap;

                    Vector2 vector = Find.WorldGrid.LongLatOf(tile);
                    btn.Tooltip = "OCity_Coordinates".Translate()
                        + " " + vector.y.ToStringLatitude()
                        + " " + vector.x.ToStringLongitude()
                        + " " + tileLabel;

                    foreach (var wo in Find.WorldObjects.ObjectsAt(tile))
                    {
                        if (wo.SelectableNow)
                        {
                            btn.Tooltip += Environment.NewLine + wo.LabelCap;
                        }
                    }
                    // Получаем полную инфу методом игры
                    //var text = (string)typeof(WorldInspectPane).GetProperty("TileInspectString").GetValue(new WorldInspectPane());

                    //Loger.Log("TestTagBtn 1 " + btn.Tooltip);
                    btn.ActionClick = TagBtnClassTile_ActionClick;
                }
                else if (value == "player")
                {
                    if (SessionClientController.Data.Players.TryGetValue(arg, out var player))
                    {
                        btn.Tooltip = player.Public.Login + Environment.NewLine + player.GetTextInfo();
                        //вызов нового окна игрока
                        btn.ActionClick = TagBtnClassPlayer_ActionClick;
                    }
                }
                else if (value == "thing")
                {
                    var thingTrade = new PawnStat();
                    var pawnParam = thingTrade.UnpackFromString(d.Replace("*", " "));

                    btn.Tooltip = thingTrade.LabelText;
                    //todo 
                }
            }
            catch
            {
                return new TagBtn() { HighlightIsOver = true };
            }
            return btn;
        }

        private static void TagBtnClassTile_ActionClick(string arg)
        {
            if (arg.Length > 1 && arg[0] == '&')
            {
                arg = arg.Remove(0, 1);
                if (!int.TryParse(arg, out var serverId)) return;

                var otherObj = UpdateWorldController.GetOtherByServerIdDirtyRead(serverId);
                if (otherObj != null)
                {
                    GameUtils.CameraJump(otherObj);
                }
                else
                {
                    var myObj = UpdateWorldController.GetWOByServerId(serverId);
                    if (myObj != null)
                    {
                        GameUtils.CameraJump(myObj);
                    }
                }
            }
            else
            {
                if (!int.TryParse(arg, out var t)) return;

                if (!GameUtils.CameraJumpWorldObject(t)) GameUtils.CameraJump(t);
            }
        }

        private static void TagBtnClassPlayer_ActionClick(string arg)
        {
            Dialog_InfoPlayer.ShowInfoPlayer(arg);
        }

        #region Сократить громоздко выглядищие тэги для отображения исходного текста вне PanelText (например в строке ввода в чате)

        private static Dictionary<string, string> Decents = new Dictionary<string, string>();
        private static Dictionary<string, string> Tags = new Dictionary<string, string>();
        private static int DecentNumber = new System.Random().Next(1000, 9999);
        public static string TagToDecent(string tag)
        {
            string decent;
            if (Decents.TryGetValue(tag, out decent)) return decent;
            if (tag.StartsWith("<img"))
            {
                var dni = tag.IndexOf(" defName=");
                if (dni >= 0)
                {
                    var dn = tag.Substring(dni + 9, tag.IndexOf(">") - (dni + 9)).Trim();
                    decent = $"<={dn}>";
                }
                else
                {
                    var dn = tag.Substring(5, tag.IndexOf(">") - 5).Trim();
                    decent = $"<{dn}>";
                }
                Decents[tag] = decent;
                Tags[decent] = tag;
            }
            else if (tag.StartsWith("<btn"))
            {
                var pli = tag.IndexOf(" name=pl");
                if (tag.Contains(" class=player "))
                {
                    var pl = tag.Substring(pli + 8, tag.IndexOf(" ", pli) - (pli + 8)).Trim();
                    decent = $"<@{pl}>";
                }
                else
                {
                    var num = ++DecentNumber;
                    decent = $"<{num}>";
                }
                Decents[tag] = decent;
                Tags[decent] = tag;
            }
            else
                decent = tag;
            return decent;
        }
        public static string DecentToTag(string decent)
        {
            string tag;
            if (Tags.TryGetValue(decent, out tag)) return tag;
            return decent;
        }

        #endregion
    }
}
