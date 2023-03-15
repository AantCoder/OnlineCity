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

namespace RimWorldOnlineCity
{
    internal class SnapshotColony
    {
        public void Exec()
        {
            //RenderMap0 renderMap = GameObject.Find("GameRoot").AddComponent<RenderMap0>() as RenderMap0;
            var renderMap = new RenderMap();
            renderMap.ImageReady = (image) => SendToServer(image);

            renderMap.Initialize();
            renderMap.Render();
        }

        private void SendToServer(byte[] data)
        {
            Loger.Log("SnapshotColony Send " + (data?.Length ?? 0));
            SessionClientController.Command((connect) =>
            {
                connect.FileSharingUpload(FileSharingCategory.ColonyScreen, SessionClientController.My.Login + "@0", data);
                var p = connect.FileSharingDownload(FileSharingCategory.ColonyScreen, SessionClientController.My.Login + "@0");

                GeneralTexture.Clear();

                var msg = data?.Length > 0 && p?.Data?.Length > 0
                    ? "OCity_Successfully".Translate() : "OCity_Error".Translate();
                Find.WindowStack.Add(new Dialog_MessageBox(msg));
            });
        }
    }
}
