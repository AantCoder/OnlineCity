using Model;
using OCUnion;
using ServerOnlineCity.Model;
using System.Collections.Generic;

namespace ServerOnlineCity.ChatService
{
    interface IChatCmd
    {
        string CmdID { get; }

        Grants GrantsForRun { get; }

        string Help { get; }

        void Execute(ref PlayerServer player, Chat chat, List<string> argsM);
    }
}