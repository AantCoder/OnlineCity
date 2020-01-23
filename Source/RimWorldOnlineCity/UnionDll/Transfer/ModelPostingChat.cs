﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Transfer
{
    [Serializable]
    public class ModelPostingChat
    {
        public long ChatId;

        public string Message;

        /// <summary>
        /// Use it only in Discord, for mark owner login message
        /// </summary>
        public string Owner;

        /// <summary>
        /// if message recieved from discord property != null 
        /// </summary>
        public ulong DiscordMsgId;
    }
}
