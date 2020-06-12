using Model;
using OCUnion;
using ServerOnlineCity.Model;
using System.Collections.Generic;
using Transfer;

namespace ServerOnlineCity.ChatService
{
    interface IChatCmd
    {
        string CmdID { get; }

        Grants GrantsForRun { get; }

        string Help { get; }

        ModelStatus Execute(ref PlayerServer player, Chat chat, List<string> argsM);
    }
}