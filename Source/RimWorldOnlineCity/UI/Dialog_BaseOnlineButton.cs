using OCUnion;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Transfer.ModelMails;
using UnityEngine;
using Verse;

namespace RimWorldOnlineCity.UI
{
    public class Dialog_BaseOnlineButton : Window
    {
        private BaseOnline Base;
        private int TabIndex = 0;



        public override Vector2 InitialSize
        {
            get { return new Vector2(700f, 680f); }
        }

        public override void PreOpen()
        {
            base.PreOpen();
            //windowRect.Set(InitialSize.x, InitialSize.y, windowRect.width, windowRect.height);
        }

        public Dialog_BaseOnlineButton(BaseOnline baseOnline)
        {
            Base = baseOnline;

            closeOnCancel = true;
            closeOnAccept = false;
            doCloseButton = false;
            doCloseX = true;
            resizeable = false;
            draggable = true;

            SelectTab0MultMax = SessionClientController.Data.GeneralSettings.IncidentMaxMult;
            StatusNeedUpdate = true;
        }

        public override void DoWindowContents(Rect inRect)
        {
            try
            {
                if (!SessionClient.Get.IsLogined)
                {
                    Close();
                    return;
                }

                var screenRect = new Rect(inRect.x, inRect.y + 31f, 400f, 0);
                var tabRect = new Rect(inRect.x, inRect.y + 31f, inRect.width, inRect.height - 31f);

                ///Внимание! Вкладки разбивают событие на очереди ожидания (т.е. одномерменно можно вызвать по одному со всех вкладок)
                ///Реализацию NumberOrder смотри в FMailIncident
                List<TabRecord> list = new List<TabRecord>();
                list.Add(new TabRecord("OC_Incidents_Hire_label".Translate(), () => { TabIndex = 0; StatusNeedUpdate = true; }, TabIndex == 0));
                list.Add(new TabRecord("OC_Incidents_Impact_label".Translate(), () => { TabIndex = 1; StatusNeedUpdate = true; }, TabIndex == 1));
                TabDrawer.DrawTabs(screenRect, list);
                if (TabIndex == 0) DoTab0Contents(tabRect);
                else if (TabIndex == 1) DoTab1Contents(tabRect);
            }
            catch (Exception e)
            {
                Loger.Log("Dialog_BaseOnlineButton Exception: " + e.Message + Environment.NewLine + e.ToString());
            }
        }

        private string SelectTab0Type; //IncidentTypes
        private string SelectTab0Faction;
        private string SelectTab0ArrivalModes; //IncidentArrivalModes
        private float SelectTab0Mult;
        private float SelectTab0MultMax;

        private string SelectTab1Type; //IncidentTypes

        private bool StatusNeedUpdate = false;
        private bool StatusCheck = false;
        private string StatusText;

        public void DoTab1Contents(Rect inRect)
        {
            Text.Font = GameFont.Small;
            var rect = new Rect(inRect.x, inRect.y + 10f, inRect.width, 80f);
            Widgets.Label(rect, "OC_Incidents_Impact_text".Translate());
            rect.height = 30f;
            rect.y += 80f;

            Text.Font = GameFont.Medium;
            Widgets.Label(rect, "OC_What_To_DO".Translate());
            Text.Font = GameFont.Small;
            rect.y += rect.height;

            if (Widgets.RadioButtonLabeled(rect, "OC_Acid_Rain".Translate(), SelectTab1Type == "acid"))
            {
                StatusNeedUpdate = true;
                SelectTab1Type = "acid";
            }
            rect.y += rect.height;

            DoTab1And2ContentsDown(rect);
        }

        public void DoTab0Contents(Rect inRect)
        {
            Text.Font = GameFont.Small;
            var rect = new Rect(inRect.x, inRect.y + 10f, inRect.width, 80f);
            Widgets.Label(rect, "OC_Incidents_Hire_text".Translate());
            rect.height = 30f;
            rect.y += 80f;

            Text.Font = GameFont.Medium;
            Widgets.Label(rect, "OC_What_To_DO".Translate());
            Text.Font = GameFont.Small;
            rect.y += rect.height;

            if (Widgets.RadioButtonLabeled(rect, "OC_Raid".Translate(), SelectTab0Type == "raid"))
            {
                StatusNeedUpdate = true;
                SelectTab0Type = "raid";
            }
            rect.y += rect.height;

            if (Widgets.RadioButtonLabeled(rect, "OC_Bugs".Translate(), SelectTab0Type == "inf"))
            {
                StatusNeedUpdate = true;
                SelectTab0Type = "inf";
            }
            rect.y += rect.height;

            if (SelectTab0Type == "raid")
            {
                rect.y += rect.height; //пробел
                Text.Font = GameFont.Medium;
                Widgets.Label(rect, "OC_Who_Hire".Translate());
                Text.Font = GameFont.Small;
                rect.y += rect.height;

                if (Widgets.RadioButtonLabeled(rect, "OC_Tribe".Translate(), SelectTab0Faction == "tribe"))
                {
                    StatusNeedUpdate = true;
                    SelectTab0Faction = "tribe";
                }
                rect.y += rect.height;

                if (Widgets.RadioButtonLabeled(rect, "OC_Pirate".Translate(), SelectTab0Faction == "pirate"))
                {
                    StatusNeedUpdate = true;
                    SelectTab0Faction = "pirate";
                }
                rect.y += rect.height;

                if (Widgets.RadioButtonLabeled(rect, "OC_Mech".Translate(), SelectTab0Faction == "mech"))
                {
                    StatusNeedUpdate = true;
                    SelectTab0Faction = "mech";
                }
                rect.y += rect.height;
            }

            if (SelectTab0Type == "raid")
            {
                rect.y += rect.height; //пробел
                Text.Font = GameFont.Medium;
                Widgets.Label(rect, "OC_Arrive".Translate());
                Text.Font = GameFont.Small;
                rect.y += rect.height;

                if (Widgets.RadioButtonLabeled(rect, "OC_Arrive_EdgeWalk".Translate(), SelectTab0ArrivalModes == "walk"))
                {
                    StatusNeedUpdate = true;
                    SelectTab0ArrivalModes = "walk";
                }
                rect.y += rect.height;

                if (Widgets.RadioButtonLabeled(rect, "OC_Arrive_DropCenter".Translate(), SelectTab0ArrivalModes == "air"))
                {
                    StatusNeedUpdate = true;
                    SelectTab0ArrivalModes = "air";
                }
                rect.y += rect.height;

                if (Widgets.RadioButtonLabeled(rect, "OC_Arrive_RandomDrop".Translate(), SelectTab0ArrivalModes == "random"))
                {
                    StatusNeedUpdate = true;
                    SelectTab0ArrivalModes = "random";
                }
                rect.y += rect.height;
            }

            rect.y += rect.height; //пробел
            Text.Font = GameFont.Medium;
            Widgets.Label(rect, "OC_Raid_Power".Translate((int)SelectTab0Mult));
            Text.Font = GameFont.Small;
            rect.y += rect.height;

            rect.height = 22f;
            if (SelectTab0Mult > SelectTab0MultMax) SelectTab0Mult = SelectTab0MultMax;
            if (SelectTab0Mult < 1) SelectTab0Mult = 1;
            SelectTab0Mult = (float)Math.Round(SelectTab0Mult + 0.001f);
            var newVal = Widgets.HorizontalSlider(rect, SelectTab0Mult, 1, SelectTab0MultMax);
            newVal = (float)Math.Round(newVal + 0.001f);
            if (newVal != SelectTab0Mult)
            {
                StatusNeedUpdate = true;
                SelectTab0Mult = newVal;
            }
            rect.y += rect.height;
            rect.height = 30f;

            DoTab1And2ContentsDown(rect);
        }

        public void DoTab1And2ContentsDown(Rect rect)
        {
            if (StatusNeedUpdate)
            {
                StatusNeedUpdate = false;
                if (TabIndex == 0)
                {
                    if (string.IsNullOrEmpty(SelectTab0Type))
                    {
                        StatusCheck = false;
                        StatusText = "OC_Choose_what_to_do".Translate();
                    }
                    else if (SelectTab0Type == "raid" && string.IsNullOrEmpty(SelectTab0Faction))
                    {
                        StatusCheck = false;
                        StatusText = "OC_Choose_who_hire".Translate();
                    }
                    else if (SelectTab0Type == "raid" && string.IsNullOrEmpty(SelectTab0ArrivalModes))
                    {
                        StatusCheck = false;
                        StatusText = "OC_Choose_arrive".Translate();
                    }
                    else
                    {
                        var command = GetCommand();
                        //расчет стоимости
                        string error;
                        var result = OCIncident.GetCostOnGameByCommand(command, true, out error);
                        StatusText = error ?? result;
                        StatusCheck = error == null;
                    }
                }
                else if (TabIndex == 1)
                {
                    if (string.IsNullOrEmpty(SelectTab1Type))
                    {
                        StatusCheck = false;
                        StatusText = "OC_Choose_what_to_do".Translate();
                    }
                    else
                    {
                        var command = GetCommand();
                        //расчет стоимости
                        string error;
                        var result = OCIncident.GetCostOnGameByCommand(command, true, out error);
                        StatusText = error ?? result;
                        StatusCheck = error == null;
                    }
                }
            }

            Color colorOrig = GUI.color;
            GUI.color = StatusCheck ? Color.green : ColoredText.RedReadable;
            Widgets.Label(new Rect(rect.x + 150f, rect.y + 10f, rect.width - 150f, 40f), StatusText);
            GUI.color = colorOrig;

            if (StatusCheck
                && Widgets.ButtonText(new Rect(rect.x, rect.y, 140f, 40f)
                , TabIndex == 0 && SelectTab0Mult == SelectTab0MultMax && (SelectTab0Faction != "tribe" || SelectTab0Type != "raid") 
                    ? "EXTERMINATUS" : "Сделать!".NeedTranslate()))
            {
                var command = GetCommand();
                var mainCannal = SessionClientController.Data.Chats[0];
                SessionClientController.Command((connect) =>
                {
                    var res = connect.PostingChat(mainCannal.Id, command);
                    StatusNeedUpdate = true;
                    /*
                    if (res != null && res.Status == 0)
                    {
                        Find.WindowStack.Add(new Dialog_MessageBox("Команда успешно отправлена!".NeedTranslate()));
                    }
                    */
                });
            }

        }

        private string GetCommand()
        {
            //return "/call raid 111 1 5 random pirate";
            return "/call "
                + (TabIndex == 0 ? SelectTab0Type : SelectTab1Type)
                + $" '{Base.OnlineWObject.LoginOwner}'"
                + $" {Base.OnlineWObject.ServerId}"
                + (TabIndex != 0 ? ""
                    : $" {SelectTab0Mult}"
                        + (SelectTab0Type != "raid" ? ""
                            : $" {SelectTab0ArrivalModes} {SelectTab0Faction}")
                    );
        }


    }
}
