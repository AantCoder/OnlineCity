using OC.DiscordBotServer.Helpers;

namespace OC.DiscordBotServer.Languages
{
    public class English
    {
        public const string ErrInvalidIP = "Invalid server IP adress or port";
        public const string ErrTryAddToExistChannel = "On this channel already registred server linked to";
        public const string ErrServerNotAvailable = "The server is unavailable now, try again later";
        public const string ErrInvalidToken = "Invalid Token";

        public const string ErrUserTokenExist = "On this token exist user {0} on Discord Channel {1}";
        public const string InfServerReg = "Сongratulation! Server  {0} related to the channel ID {1}";
        public const string InfResctrictInPrivate = "This command must be typed in a channel, not in private Chat";
        public const string InfResctrictInChannel = "This command must be typed in private Chat, not in a channel ";

        public const string InfUserFound = "Registred user {0} on the server {1}";
        public const string InfUserNotFound = "Your message was been deleted \n" +
            "For registred type:" + Helper.PREFIX + " regme MY_TOKEN";
    }
}