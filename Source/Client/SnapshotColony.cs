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
using MapRenderer;
using System.IO;
using RimWorld.Planet;

namespace RimWorldOnlineCity
{
    internal class SnapshotColony
    {
        public bool HighQuality = false;
        public bool Background = false;

        public void Exec(Settlement settlement)
        {
            var serverId = UpdateWorldController.GetMyByLocalId(settlement?.ID ?? 0)?.PlaceServerId ?? 0;
            Loger.Log($"SnapshotColony serverId={serverId}");

            if (serverId == 0) return;

            //RenderMap0 renderMap = GameObject.Find("GameRoot").AddComponent<RenderMap0>() as RenderMap0;
            var renderMap = new RenderMap();
            if (HighQuality)
            {
                renderMap.SettingsPixelOnCell = 30;
                renderMap.SettingsQuality = 86;
            }

            renderMap.ImageReady = (image) => SendToServer(image, serverId, Background);

            renderMap.Initialize(settlement.Map);
            renderMap.Render();
        }

        private static void SendToServer(byte[] data, long serverId, bool background)
        {
            Loger.Log($"SnapshotColony Send serverId={serverId} data.Len=" + (data?.Length ?? 0));
            SessionClientController.Command((connect) =>
            {
                try
                {
                    connect.FileSharingUpload(FileSharingCategory.ColonyScreen, SessionClientController.My.Login + "@" + serverId, data);
                    if (!background)
                    {
                        var p = connect.FileSharingDownload(FileSharingCategory.ColonyScreen, SessionClientController.My.Login + "@" + serverId);

                        GeneralTexture.Clear();

                        var msg = data?.Length > 0 && p?.Data?.Length > 0
                            ? "OCity_Successfully".Translate() : "OCity_Error".Translate();
                        Find.WindowStack.Add(new Dialog_MessageBox(msg));
                    }
                }
                catch
                { }
            });
        }
    }
}
