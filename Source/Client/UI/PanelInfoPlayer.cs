using Model;
using OCUnion;
using OCUnion.Transfer;
using OCUnion.Transfer.Model;
using RimWorldOnlineCity.Services;
using RimWorldOnlineCity.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace RimWorldOnlineCity
{
    public class PanelInfoPlayer : DialogControlBase
    {
        public PlayerClient player;
        private bool Loading = true;
        private WorldObjectsValues AllWorldObjects;
        private ModelPlayerInfoExtended info = null;

        public Vector2 ScrollPosition = new Vector2();
        public float Height = 0f;

        private Color WindowBGBorderColor = new ColorInt(97, 108, 122).ToColor;
        private Texture2D SkillBarFillTex = SolidColorMaterials.NewSolidColorTexture(new Color(1f, 1f, 1f, 0.1f));

        public PanelInfoPlayer(PlayerClient player)
        { 
            Init(player);
        }

        private void Init(PlayerClient pl)
        {
            this.player = pl;
            Loading = true;
            info = null;
            Task.Run(() =>
            {
                try
                {
                    SessionClientController.Command((connect) =>
                    {
                        info = connect.GetPlayerInfoExtended(pl.Public.Login);
                    });
                }
                catch (Exception ex)
                {
                    Loger.Log("Exception PanelInfoPlayer " + ex.ToString());
                }
            });
        }

        private void Init2()
        {
            Loading = false;
            AllWorldObjects = player.CostWorldObjects();
        }

        public void Drow(Rect inRect)
        {
            if (player == null) return;
            player = player.Refrash(); //обновляем для актуализации онлайн статуса

            if (Loading && info != null) Init2();

            var chatAreaInner = new Rect(0, 0, inRect.width - ListBox<string>.WidthScrollLine, 0);
            if (chatAreaInner.width <= 0) return;
            chatAreaInner.height = Height;
            ScrollPosition = GUI.BeginScrollView(inRect, ScrollPosition, chatAreaInner);
            GUILayout.BeginArea(chatAreaInner);
            Text.Anchor = TextAnchor.MiddleLeft;

            var curHeight = 0f;
            var iconBorder = 3f;

            Action<Texture2D, float, List<Action<int, Rect>>> drawPanel = (icon, iconHeight, drawRow) =>
            {
                if (iconHeight == 0) iconHeight = 128f;
                var prect = new Rect(0, curHeight, 128f + iconBorder * 2f, iconHeight + iconBorder * 2f);
                if (icon != null)
                {
                    GUI.DrawTexture(prect, Command.BGTexShrunk); //BGTex);
                    prect = prect.ContractedBy(iconBorder);
                    GUI.DrawTexture(prect, icon);
                }
                var col0 = drawRow.Count > 4 ? (chatAreaInner.width - 128f - 8f) * 0.45f : chatAreaInner.width - 128f - 8f;
                var col1 = (chatAreaInner.width - 128f - 8f) - col0;

                var rowCount = drawRow.Count <= 4 ? drawRow.Count : (drawRow.Count + 1) / 2;
                var rowHeight = iconHeight / (float)rowCount;
                for (var i = 0; i < drawRow.Count; i++)
                {
                    prect = new Rect(128f + iconBorder * 2f + 16f + col0 * (i / rowCount)
                        , curHeight + rowHeight * (i % rowCount) + (i % rowCount) + 1
                        , i < rowCount ? col0 : col1
                        , rowHeight);
                    drawRow[i](i, prect);
                }

                curHeight += iconHeight + iconBorder * 2f + 10f;

                prect = new Rect(0, curHeight, chatAreaInner.width, 2f);
                GUI.color = WindowBGBorderColor;
                Widgets.DrawBox(prect);
                GUI.color = Color.white;

                curHeight += 2f + 10f;
            };


            if (Loading || info == null)
            {
                drawPanel(GeneralTexture.Get.ByName("pl_" + player.Public.Login), 0, new List<Action<int, Rect>>() {
                    (num, rect) =>
                    {
                        Text.Font = GameFont.Medium;
                        Widgets.Label(rect, player.Public.Login + Environment.NewLine + "OC_Loading".Translate() + "...");
                        Text.Font = GameFont.Small;
                    },
                });

                Height = curHeight;
                Text.Anchor = TextAnchor.UpperLeft;
                GUILayout.EndArea();
                GUI.EndScrollView();
                return;
            }

            /// Панель игрока
            
            drawPanel(GeneralTexture.Get.ByName("pl_" + player.Public.Login), 0, new List<Action<int, Rect>>() {
                (num, rect) => //1
                {
                    Text.Font = GameFont.Medium;
                    Widgets.Label(rect, player.Public.Login);
                    Text.Font = GameFont.Small;
                },
                (num, rect) =>
                {
                    if (player.Online)
                    {
                        Widgets.Label(rect, "Online");
                        var labelsize = Text.CalcSize("Online");
                        var icon = GeneralTexture.Get.GetEmoji("green_circle");
                        GUI.DrawTexture(new Rect(rect.x + 2f + labelsize.x, rect.y + labelsize.y / 2f - 6f , 12f, 12f), icon);
                    }
                    else
                    {
                        Widgets.Label(rect, "Offline");
                        var labelsize = Text.CalcSize("Offline");
                        var icon = GeneralTexture.Get.GetEmoji("red_circle");
                        GUI.DrawTexture(new Rect(rect.x + 2f + labelsize.x, rect.y + labelsize.y / 2f - 6f , 12f, 12f), icon);
                    }
                },
                (num, rect) =>
                {
                    var txt = "OCity_PlayerClient_LastSaveTime".Translate().Replace("{2}", "") +
                        (player.Public.LastSaveTime == DateTime.MinValue ? "OCity_PlayerClient_LastSaveTimeNon".Translate() : new TaggedString (player.Public.LastSaveTime.ToGoodUtcString()));
                    Widgets.Label(rect, txt);

                },
                (num, rect) =>
                {
                    var txt = string.Format("OCity_PlayerClient_LastTick".Translate()
                        , player.Public.LastTick / 3600000
                        , player.Public.LastTick / 60000);
                    Widgets.Label(rect, txt);
                },
                (num, rect) => //2
                {
                    GUI.DrawTexture(new Rect(rect.x, rect.y, 32f, 32f), GeneralTexture.HomeAreaOn);
                    rect.xMin += 32f + 2f;
                    var txt = "OCity_PlayerClient_baseCount".Translate().Replace("{3}", "") + AllWorldObjects.BaseCount;
                    Widgets.Label(rect, txt);
                    var labelsize = Text.CalcSize(txt);
                    rect.xMin += labelsize.x + 2f;

                    GUI.DrawTexture(new Rect(rect.x, rect.y, 32f, 32f), GeneralTexture.Caravan);
                    rect.xMin += 32f + 2f;
                    txt = "OCity_PlayerClient_caravanCount".Translate().Replace("{4}", "") + AllWorldObjects.CaravanCount;
                    Widgets.Label(rect, txt);
                },
                (num, rect) =>
                {
                    GUI.DrawTexture(new Rect(rect.x, rect.y, 32f, 32f), GeneralTexture.ItemStash);
                    rect.xMin += 32f + 2f;
                    var txt = "OCity_PlayerClient_marketValue".Translate().Replace("{5}", "") + AllWorldObjects.MarketValue.ToStringMoney();
                    Widgets.Label(rect, txt);
                },
                (num, rect) =>
                {
                    GUI.DrawTexture(new Rect(rect.x, rect.y, 32f, 32f), GeneralTexture.ItemStash);
                    rect.xMin += 32f + 2f;
                    var txt = "OCity_PlayerClient_marketValuePawn".Translate().Replace("{6}", "") + AllWorldObjects.MarketValuePawn.ToStringMoney();
                    Widgets.Label(rect, txt);
                },
                (num, rect) =>
                {
                    GUI.DrawTexture(new Rect(rect.x, rect.y, 32f, 32f), GeneralTexture.OpenBox);
                    rect.xMin += 32f + 2f;
                    var txt = "OCity_PlayerClient_marketValueTrading".Translate().Replace("{7}", "") + (AllWorldObjects.MarketValueBalance + AllWorldObjects.MarketValueStorage).ToStringMoney();
                    Widgets.Label(rect, txt);
                },
            });

            /// Панель рейтинга

            drawPanel(null, 0, new List<Action<int, Rect>>() {
                (num, rect) =>
                {
                    var iconRect = new Rect(rect.x - (128f + iconBorder * 2f + 16f), rect.y, 128f + iconBorder * 2f, 128f + iconBorder * 2f);
                    var barRect = new Rect(iconRect);
                    barRect.height /= 3f;

                    /// выводим ачивки
                    
                    if (info.Achievements != null)
                    {
                        var w = 32f;
                        for (int i = 0; i < info.Achievements.Count; i++)
                        {
                            var x = (barRect.width - w) / (info.Achievements.Count + 1) * (i + 1);
                            var texture = ContentFinder<Texture2D>.Get(info.Achievements[i], false) ?? GeneralTexture.Get.ByName(info.Achievements[i]);
                            var r = new Rect(barRect.x + x, barRect.y, w, w);
                            GUI.DrawTexture(r, texture);
                            var text = ("OC_Achievements_" + info.Achievements[i]).Translate(); //"Сдохни или умри!";
                            if (Mouse.IsOver(r)) Widgets.DrawHighlight(r);
                            TooltipHandler.TipRegion(r, text);
                        }
                    }

                    /// выводим рейтинг
                    
                    barRect.y += barRect.height;
                    var anchor = Text.Anchor;
                    Text.Anchor = TextAnchor.MiddleCenter;
                    var barRect2 = new Rect(barRect);
                    Text.Font = GameFont.Medium;
                    if (info.MarketValueRanking > 0)
                    {
                        barRect2.xMax -= 16f;
                        Widgets.Label(barRect2, "Рейтинг".NeedTranslate() + " " + info.MarketValueRanking);
                    }
                    else
                        Widgets.Label(barRect2, "Нет рейтинга".NeedTranslate());
                    Text.Font = GameFont.Small;
                    if (info.MarketValueRanking > 0)
                    {
                        barRect2 = new Rect(barRect.x + barRect.width - 16f, barRect.y + 16f, 16f, 16f);
                        var tl = "Прошлое место".NeedTranslate() + " " + (info.MarketValueRankingLast == 0 ? "-" : info.MarketValueRankingLast.ToString());
                        if (info.MarketValueRankingLast == 0 || info.MarketValueRankingLast > info.MarketValueRanking)
                        {
                            GUI.DrawTexture(barRect2, GeneralTexture.RankingUp);
                            if (Mouse.IsOver(barRect2)) Widgets.DrawHighlight(barRect2);
                            TooltipHandler.TipRegion(barRect2, tl);
                        }
                        else if (info.MarketValueRankingLast < info.MarketValueRanking)
                        {
                            GUI.DrawTexture(barRect2, GeneralTexture.RankingDown);
                            if (Mouse.IsOver(barRect2)) Widgets.DrawHighlight(barRect2);
                            TooltipHandler.TipRegion(barRect2, tl);
                        }

                        barRect.y += 32f;
                        var precent = (info.RankingCount - 1) > 0 ? 100 * (info.RankingCount - info.MarketValueRanking) / (info.RankingCount - 1) : 100;
                        Widgets.Label(barRect, String.Format("Лучше {0}% игроков".NeedTranslate(), precent));
                    }
                    Text.Anchor = anchor;

                    /// выводим график

                    var widthCol = 4f;
                    var graRect = new Rect(rect);
                    graRect.height += 4; //почему-то рисует снизу 14 вместо 10
                    graRect.width = 8f + (widthCol + 1f)  * 60;
                    GUI.color = WindowBGBorderColor;
                    Widgets.DrawBox(graRect);
                    GUI.color = Color.white;

                    rect.xMin += graRect.width + 8f;

                    graRect = graRect.ContractedBy(4);
                    graRect.width = widthCol;
                    var historyMax = info.MarketValueHistory?.Max() ?? AllWorldObjects.MarketValueTotal;

                    if (info.MarketValueHistory != null && historyMax > 0f)
                    {
                        for(int i = 0; i < info.MarketValueHistory.Count && i < 60; i++)
                        {
                            var val = info.MarketValueHistory[i] / historyMax;
                            if (val < 0f) val = 0f;
                            if (val > 1f) val = 1f;
                            var pix = val * graRect.height;

                            if (pix >= 1f) GUI.DrawTexture(new Rect(graRect.x, graRect.y + graRect.height - pix, graRect.width, pix), Command.BGTexShrunk);
                            if (Mouse.IsOver(graRect)) Widgets.DrawHighlight(graRect);
                            TooltipHandler.TipRegion(graRect, info.MarketValueHistory[i].ToStringMoney());

                            graRect.x += widthCol + 1f;
                        }
                    }

                    /// цифры: максимум стоимость, текущая стоимость, под атакой, пешек всего, больных, с кровотечением

                    var itemRect = new Rect(rect.x, rect.y, rect.width, rect.height / 4f);

                    GUI.DrawTexture(new Rect(itemRect.x, itemRect.y, 32f, 32f), GeneralTexture.ItemStash);
                    itemRect.xMin += 32f + 2f;
                    Widgets.Label(itemRect, "Общая стоимость".NeedTranslate() + ": " + AllWorldObjects.MarketValueTotal.ToStringMoney());

                    itemRect = new Rect(rect.x, rect.y + rect.height / 4f * 1f, rect.width, rect.height / 4f);
                    GUI.DrawTexture(new Rect(itemRect.x, itemRect.y, 32f, 32f), GeneralTexture.ItemStash);
                    itemRect.xMin += 32f + 2f;
                    Widgets.Label(itemRect, "Максимальная: " + historyMax.ToStringMoney());

                    itemRect = new Rect(rect.x, rect.y + rect.height / 4f * 2f, rect.width / 16f * 3.5f , rect.height / 4f);
                    if (Mouse.IsOver(itemRect)) Widgets.DrawHighlight(itemRect);
                    TooltipHandler.TipRegion(itemRect, "Всего колонистов".NeedTranslate());
                    GUI.DrawTexture(new Rect(itemRect.x, itemRect.y, 32f, 32f), GeneralTexture.Pawns);
                    itemRect.xMin += 32f + 2f;
                    Widgets.Label(itemRect, info.ColonistsCount.ToString());

                    itemRect = new Rect(rect.x + rect.width / 16f * 3.5f * 1f, rect.y + rect.height / 4f * 2f, rect.width / 16f * 3.5f, rect.height / 4f);
                    if (Mouse.IsOver(itemRect)) Widgets.DrawHighlight(itemRect);
                    TooltipHandler.TipRegion(itemRect, "Колонистов требующих лечения".NeedTranslate());
                    GUI.DrawTexture(new Rect(itemRect.x, itemRect.y, 32f, 32f), GeneralTexture.PawnsNeedingTend);
                    itemRect.xMin += 32f + 2f;
                    Widgets.Label(itemRect, info.ColonistsNeedingTend.ToString());

                    itemRect = new Rect(rect.x + rect.width / 16f * 3.5f * 2f, rect.y + rect.height / 4f * 2f, rect.width / 16f * 3.5f, rect.height / 4f);
                    if (Mouse.IsOver(itemRect)) Widgets.DrawHighlight(itemRect);
                    TooltipHandler.TipRegion(itemRect, "Колонистов без сознания".NeedTranslate());
                    GUI.DrawTexture(new Rect(itemRect.x, itemRect.y, 32f, 32f), GeneralTexture.PawnsDown);
                    itemRect.xMin += 32f + 2f;
                    Widgets.Label(itemRect, info.ColonistsDownCount.ToString());

                    itemRect = new Rect(rect.x + rect.width / 16f * 3.5f * 3f, rect.y + rect.height / 4f * 2f, rect.width / 16f * 5.5f, rect.height / 4f);
                    if (Mouse.IsOver(itemRect)) Widgets.DrawHighlight(itemRect);
                    TooltipHandler.TipRegion(itemRect, "Всего обучаемых животных".NeedTranslate());
                    GUI.DrawTexture(new Rect(itemRect.x, itemRect.y, 32f, 32f), GeneralTexture.PawnsAnimal);
                    itemRect.xMin += 32f + 2f;
                    Widgets.Label(itemRect, info.AnimalObedienceCount.ToString());

                    if (info.ExistsEnemyPawns)
                    {
                        itemRect = new Rect(rect.x, rect.y + rect.height / 4f * 3f, rect.width, rect.height / 4f);
                        GUI.DrawTexture(new Rect(itemRect.x, itemRect.y, 32f, 32f), GeneralTexture.AttackSettlement);
                        itemRect.xMin += 32f + 2f;
                        Widgets.Label(itemRect, "На карте враги".NeedTranslate());
                    }
                },
                });

            /// Панель навыков команды
            
            Action<Rect, string, int> drawSkill = (rect, caption, skill) =>
            {
                var anchor = Text.Anchor;
                Text.Anchor = TextAnchor.MiddleLeft;
                //rect = rect.ContractedBy(2);
                Widgets.Label(rect, caption);
                rect.xMin += rect.width - 50f;
                float fillPercent = Mathf.Max(0.01f, (float)skill / 20f);
                Text.Anchor = TextAnchor.MiddleCenter;
                Widgets.FillableBar(rect.ContractedBy(2), fillPercent, SkillBarFillTex, null, doBorder: false);
                Widgets.Label(rect, skill.ToString());
                Text.Anchor = anchor;
            };

            if (info.MaxSkills?.Count == 12)
                drawPanel(null, 120f, new List<Action<int, Rect>>() {
                    (num, rect) => //1
                    {
                        var anchor = Text.Anchor;
                        Text.Anchor = TextAnchor.MiddleCenter;

                        var iconRect = new Rect(rect.x - (128f + iconBorder * 2f + 16f), rect.y, 128f + iconBorder * 2f, 120f + iconBorder * 2f);
                        Text.Font = GameFont.Medium;
                        Widgets.Label(iconRect, "TeamSkills".Translate());
                        Text.Font = GameFont.Small;
                        Text.Anchor = anchor;

                        drawSkill(rect, "Дальний бой".NeedTranslate(), info.MaxSkills[0]);
                    },
                    (num, rect) =>
                    {
                        drawSkill(rect, "Ближний бой".NeedTranslate(), info.MaxSkills[1]);
                    },
                    (num, rect) =>
                    {
                        drawSkill(rect, "Строительство".NeedTranslate(), info.MaxSkills[2]);
                    },
                    (num, rect) =>
                    {
                        drawSkill(rect, "Горное дело".NeedTranslate(), info.MaxSkills[3]);
                    },
                    (num, rect) =>
                    {
                        drawSkill(rect, "Кулинария".NeedTranslate(), info.MaxSkills[4]);
                    },
                    (num, rect) =>
                    {
                        drawSkill(rect, "Растеневодство".NeedTranslate(), info.MaxSkills[5]);
                    },
                    (num, rect) =>
                    {
                        rect.xMin += 16f;
                        drawSkill(rect, "Животноводство".NeedTranslate(), info.MaxSkills[6]);
                    },
                    (num, rect) =>
                    {
                        rect.xMin += 16f;
                        drawSkill(rect, "Ремесло".NeedTranslate(), info.MaxSkills[7]);
                    },
                    (num, rect) =>
                    {
                        rect.xMin += 16f;
                        drawSkill(rect, "Искусство".NeedTranslate(), info.MaxSkills[8]);
                    },
                    (num, rect) =>
                    {
                        rect.xMin += 16f;
                        drawSkill(rect, "Медицина".NeedTranslate(), info.MaxSkills[9]);
                    },
                    (num, rect) =>
                    {
                        rect.xMin += 16f;
                        drawSkill(rect, "Общение".NeedTranslate(), info.MaxSkills[10]);
                    },
                    (num, rect) =>
                    {
                        rect.xMin += 16f;
                        drawSkill(rect, "Умственный труд".NeedTranslate(), info.MaxSkills[11]);
                    },
                });

            /// Государство

            //todo

            /// Панели поселений

            if (player.WObjects != null)
            {
                foreach (var wo in player.WObjects
                    .OrderBy(wo => (wo is BaseOnline ? 1000000 : 2000000) + wo.OnlineWObject.PlaceServerId))
                {
                    drawPanel(ContentFinder<Texture2D>.Get(wo.ExpandingIconName, false), 0, new List<Action<int, Rect>>() {
                        (num, rect) => //1
                        {
                            rect = new Rect(rect.x, rect.y, chatAreaInner.width - (rect.x - chatAreaInner.x), rect.height);
                            Text.Font = GameFont.Medium;
                            Widgets.Label(rect, wo.LabelCap); // wo.OnlineWObject.Name
                            Text.Font = GameFont.Small;
                            var rectBut = new Rect(rect.x + rect.width - rect.height, rect.y, rect.height, rect.height);

                            GUI.DrawTexture(rectBut, Command.BGTexShrunk);
                            GUI.DrawTexture(rectBut.ContractedBy(2), ContentFinder<Texture2D>.Get("Waypoint", false));
                            if (Mouse.IsOver(rectBut)) Widgets.DrawHighlight(rectBut);
                            if (Widgets.ButtonInvisible(rectBut))
                            {
                                GameUtils.CameraJump(wo);
                            }

                            var wobase = wo as BaseOnline;
                            if (wobase != null && wo.OnlineWObject.LoginOwner != SessionClientController.My.Login)
                            {
                                foreach(var giz in wobase.GetGizmos().Reverse())
                                {
                                    var command_Action = giz as Command_Action;
                                    if (command_Action == null || command_Action.icon == GeneralTexture.OCInfo) continue;

                                    //var command_Action = GameUtils.CommandShowMap(wobase);
                                    rectBut.x -= rectBut.width + 4f;
                                    GUI.DrawTexture(rectBut, Command.BGTexShrunk);
                                    GUI.DrawTexture(rectBut.ContractedBy(2), command_Action.icon);
                                    if (Mouse.IsOver(rectBut)) Widgets.DrawHighlight(rectBut);
                                    if (Widgets.ButtonInvisible(rectBut))
                                    {
                                        command_Action.action();
                                    }
                                    TooltipHandler.TipRegion(rectBut, command_Action.defaultDesc);
                                }
                            }
                        },
                        (num, rect) =>
                        {
                            //var tileLabel = Find.WorldGrid[wo.Tile].biome.LabelCap;
                            Vector2 vector = Find.WorldGrid.LongLatOf(wo.Tile);
                            var text = "OCity_Coordinates".Translate()
                                + " " + vector.y.ToStringLatitude()
                                + " " + vector.x.ToStringLongitude();
                                //+ Environment.NewLine + tileLabel;
                            Widgets.Label(rect, text);

                            var textsize = Text.CalcSize(text);
                            rect.xMin += textsize.x;
                            rect.width += 20f;
                            var prevColor = GUI.color;
                            GUI.color = Color.gray;
                            Widgets.Label(rect, $" sId: {wo.OnlineWObject.PlaceServerId}");
                            GUI.color = prevColor;
                        },
                        (num, rect) =>
                        {
                            if (!(wo is BaseOnline))
                            {
                                GUI.DrawTexture(new Rect(rect.x, rect.y, 32f, 32f), GeneralTexture.OCE_To);
                                rect.xMin += 32f + 2f;
                                var txt = "OCity_Caravan_FreeWeight".Translate().ToString() + wo.OnlineWObject.FreeWeight.ToStringMass();
                                //немного наезжаем на соседние строки, чтобы поместились две с переносом
                                rect.y -= 2f;
                                rect.height += 4f;
                                Widgets.Label(rect, txt);
                            }
                            else
                            {
                                var tileLabel = Find.WorldGrid[wo.Tile].biome.LabelCap;
                                Widgets.Label(rect, tileLabel);
                            }
                        },
                        (num, rect) =>
                        {
                            GUI.DrawTexture(new Rect(rect.x, rect.y, 32f, 32f), GeneralTexture.ItemStash);
                            rect.xMin += 32f + 2f;
                            var txt = "Общая стоимость".NeedTranslate() + ": " + wo.OnlineWObject.MarketValueTotal.ToStringMoney();
                            Widgets.Label(rect, txt);

                        },
                        (num, rect) => //2
                        {
                            //долно быть пусто, т.к. с первой колонки занимает всю строку
                        },
                        (num, rect) =>
                        {
                            GUI.DrawTexture(new Rect(rect.x, rect.y, 32f, 32f), GeneralTexture.ItemStash);
                            rect.xMin += 32f + 2f;
                            var txt = "OCity_PlayerClient_marketValue".Translate().Replace("{5}", "") + wo.OnlineWObject.MarketValue.ToStringMoney();
                            Widgets.Label(rect, txt);
                        },
                        (num, rect) =>
                        {
                            GUI.DrawTexture(new Rect(rect.x, rect.y, 32f, 32f), GeneralTexture.ItemStash);
                            rect.xMin += 32f + 2f;
                            var txt = "OCity_PlayerClient_marketValuePawn".Translate().Replace("{6}", "") + wo.OnlineWObject.MarketValuePawn.ToStringMoney();
                            Widgets.Label(rect, txt);
                        },
                        (num, rect) =>
                        {
                            GUI.DrawTexture(new Rect(rect.x, rect.y, 32f, 32f), GeneralTexture.OpenBox);
                            rect.xMin += 32f + 2f;
                            var txt = "OCity_PlayerClient_marketValueTrading".Translate().Replace("{7}", "") + (wo.OnlineWObject.MarketValueBalance + wo.OnlineWObject.MarketValueStorage).ToStringMoney();
                            Widgets.Label(rect, txt);
                        },
                    });





                }
            }

            /// Инциденты
            /*
            if ((info.FunctionMailsView?.Count ?? 0) > 0)
            {
                var mg = info.FunctionMailsView.GroupBy(m => m.NumberOrder);
                
                drawPanel(GeneralTexture.IncidentViewIcon, 0, new List<Action<int, Rect>>() {
                    (num, rect) =>
                    {
                        //todo
                    },
                });
            }
            */
            Height = curHeight;
            Text.Anchor = TextAnchor.UpperLeft;
            GUILayout.EndArea();
            GUI.EndScrollView();

        }
    }
}
