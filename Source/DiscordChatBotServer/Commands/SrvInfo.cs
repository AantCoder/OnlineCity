using System.Collections.Generic;
using System.Linq;
using System.Text;
using Discord.Commands;
using OC.DiscordBotServer.Common;
using OC.DiscordBotServer.Languages;
using OCUnion.Transfer;

namespace OC.DiscordBotServer.Commands
{
    public class SrvInfoCommand : ICommand
    {
        private readonly ApplicationContext _app;

        public SrvInfoCommand(ApplicationContext app)
        {
            _app = app;
        }

        internal string Execute(SocketCommandContext context, string param)
        {
            var sb = new StringBuilder();
            var author = context.Message.Author.Id;

            switch (param)
            {
                // "srvinfo all: информация по всем серверам, отсортированные 1.По количеству пользователей от максимального к минимальному, по дате поcледнего онлайна"
                case "all":
                    {
                        //foreach (var sessionClient in _app.DiscrordToOCServer.Values)
                        //{
                        //    //sessionClient.
                        //}

                        return "";
                    }
                //все где я зарегестрирован\n"
                case "my":
                    {
                        var servers = new List<ulong>();
                        foreach (var kv in _app.UserOnServers)
                        {
                            if (kv.Value.Values.Any(x => x.UserId == author))
                            {
                                servers.Add(kv.Key);
                            }
                        }

                        return getServersInfo(servers, ServerInfoType.Full);
                    }
            }

            var sessionClient = _app.TryGetSessionClientByIP(param);
            if (sessionClient == null)
            {
                return Translator.ErrInvalidIP;
            }

            return sessionClient.GetDescription(ServerInfoType.FullWithDescription);
        }

        private string getServersInfo(IEnumerable<ulong> servers, ServerInfoType infoType)
        {
            var sb = new StringBuilder();
            foreach (var y in servers)
            {
                if (!_app.DiscrordToOCServer.TryGetValue(y, out SessionClientWrapper sessionClient))
                {
                    continue;
                }

                sessionClient.GetDescription(infoType);
            }

            return "";
        }
    }
}
