using System;
using System.Collections.Generic;
using System.Text;

namespace ServerOnlineCity.Model
{
    public class ServiceContext
    {
        public PlayerServer Player;

        public Action<Action<SessionServer>> AllSessionAction;
    }
}
