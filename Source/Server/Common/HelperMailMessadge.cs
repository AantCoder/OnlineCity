using ServerOnlineCity.Model;
using System;
using System.Collections.Generic;
using System.Text;
using Transfer.ModelMails;

namespace ServerOnlineCity.Common
{
    public static class HelperMailMessadge
    {
        public static void Send(PlayerServer from
            , PlayerServer to
            , string title
            , string text
            , ModelMailMessadge.MessadgeTypes typeIcon
            , int tile = 0)
        {

            var packet = new ModelMailMessadge()
            {
                From = from.Public,
                type = typeIcon,
                label = title,
                text = text,
                Tile = tile,
            };
            lock (to)
            {
                packet.To = to.Public;
                to.Mails.Add(packet);
            }
        }
    }
}
