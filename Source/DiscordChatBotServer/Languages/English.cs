using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OC.DiscordBotServer.Languages
{
    public class English
    {
        public const string ErrInvalidIP = "Invalid server IP adress or port";
        public const string ErrTryAddToExistChannel = "On Channel {0} registred server, with IP: {1}";
        public const string ErrServerNotAvailable = "The server is not available now, try again later";
        public const string ErrInvalidDiscordToken = "Invalid Discord Token ";

        public const string InfServerReg = "Сongratulation! Server  {0} related to the channel ID {1}";
        public const string InfResctrictInPrivate = "This command must be typed in a channel, not in private Chat";
    }
}