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
    public class PanelProfilePlayer : DialogControlBase
    {
        private bool Inited;
        private Player Player;

        private bool Input_EnablePVP;
        private string Input_DelaySaveGame;
        private string Input_DiscordUserName;
        private string Input_EMail;
        private TextBox Input_AboutMyTextBox = new TextBox() { Editable = true };

        public void Init()
        {
            Inited = true;
            Player = SessionClientController.My;
            Input_EnablePVP = Player.EnablePVP;
            Input_DelaySaveGame = SessionClientController.Data.DelaySaveGame.ToString();
            Input_DiscordUserName = Player.DiscordUserName ?? "";
            Input_EMail = Player.EMail ?? "";
            Input_AboutMyTextBox.Text = Player.AboutMyText ?? "";
        }


        public bool Validate()
        {
            if (!int.TryParse(Input_DelaySaveGame, out int delaySaveGame)
                || delaySaveGame < 5
                ) return false;

            return true;
        }

        public void Save()
        {
            if (!Validate())
            {
                Load();
                return;
            }
            var delaySaveGame = int.Parse(Input_DelaySaveGame);

            var spi = new SetPlayerInfo(SessionClient.Get);
            spi.GenerateRequestAndDoJob(new ModelPlayerInfo()
            {
                DelaySaveGame = delaySaveGame,
                EnablePVP = Input_EnablePVP,
                DiscordUserName = Input_DiscordUserName,
                EMail = Input_EMail,
                AboutMyTextBox = Input_AboutMyTextBox.Text,
            });

            Load();
        }

        public void Load()
        {
            var connect = SessionClient.Get;
            var serverInfo = connect.GetInfo(ServerInfoType.Full);
            SessionClientController.SetFullInfo(serverInfo);
            Init();
        }

        public void Drow(Rect inRect)
        {
            if (!Inited) Init();
            /*
             * 24 высота строки
             * 4 между строк
            */

            Text.Font = GameFont.Medium;
            Widgets.Label(inRect, "OCity_PlayerClient_Settings".Translate().ToString() + " " + Player.Login);
            Text.Font = GameFont.Small;
            float topOffset = 30;
            Rect rect;

            /// Я учавствую в PVP
            if (SessionClientController.Data.GeneralSettings.EnablePVP || Player.EnablePVP)
            {
                rect = new Rect(inRect.x + 30f, inRect.y + topOffset, 250f, 25f);
                Widgets.CheckboxLabeled(rect, "OCity_PlayerClient_InvolvedInPVP".Translate(), ref Input_EnablePVP, SessionClientController.Data.TimeChangeEnablePVP >= DateTime.UtcNow, null, null, true);
                rect = new Rect(inRect.x + 30f + 250f, inRect.y + topOffset, inRect.width - 30f - 250f, 25f);
                if (SessionClientController.Data.TimeChangeEnablePVP >= DateTime.UtcNow)
                {
                    Input_EnablePVP = Player.EnablePVP;
                    Widgets.Label(rect, "OCity_PlayerClient_CanChange".Translate(SessionClientController.Data.TimeChangeEnablePVP.ToGoodUtcString()));
                }
                else
                {
                    Widgets.Label(rect, "OCity_PlayerClient_CanChangeNow".Translate());
                }
            }
            topOffset += 30f;

            /// Интервал сохранений в минутах
            rect = new Rect(inRect.x + 30f, inRect.y + topOffset, inRect.width - 30f, 25f);
            Widgets.Label(rect, "OCity_PlayerClient_SaveInterval".Translate());
            topOffset += 25f;
            rect = new Rect(inRect.x + 30f, inRect.y + topOffset, 250f, 25f);
            Input_DelaySaveGame = GUI.TextField(rect, Input_DelaySaveGame, 1000);
            topOffset += 30f;

            /// Мой дискорд
            rect = new Rect(inRect.x + 30f, inRect.y + topOffset, inRect.width - 30f, 25f);
            Widgets.Label(rect, "OCity_PlayerClient_Discord".Translate());
            topOffset += 25f;
            rect = new Rect(inRect.x + 30f, inRect.y + topOffset, 250f, 25f);
            Input_DiscordUserName = GUI.TextField(rect, Input_DiscordUserName, 1000);
            topOffset += 30f;

            /// Почта
            rect = new Rect(inRect.x + 30f, inRect.y + topOffset, inRect.width - 30f, 25f);
            Widgets.Label(rect, "OCity_PlayerClient_Email".Translate());
            topOffset += 25f;
            rect = new Rect(inRect.x + 30f, inRect.y + topOffset, 250f, 25f);
            Input_EMail = GUI.TextField(rect, Input_EMail, 1000);
            topOffset += 30f;

            /// Обо мне
            rect = new Rect(inRect.x + 30f, inRect.y + topOffset, inRect.width - 30f, 25f);
            Widgets.Label(rect, "OCity_PlayerClient_AboutMyself".Translate());
            topOffset += 25f;
            rect = new Rect(inRect.x + 30f, inRect.y + topOffset, inRect.width - 50f, inRect.height - topOffset - 50f);
            Input_AboutMyTextBox.Drow(rect);
            topOffset = inRect.height - 50f;
            
            rect = new Rect(inRect.x + 70f, inRect.y + topOffset, 200f, 30f);
            if (Widgets.ButtonText(rect, "OCity_PlayerClient_Save".Translate()))
            {
                Save();
            }

        }
    }
}
