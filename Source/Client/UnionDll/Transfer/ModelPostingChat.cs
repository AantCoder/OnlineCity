using System;

namespace Transfer
{
    [Serializable]
    public class ModelPostingChat
    {
        public int IdChat;

        public string Message;

        /// <summary>
        /// Use it only in Discord, for mark owner login message
        /// </summary>
        public string Owner;

        /// <summary>
        /// if message recieved from discord property != null 
        /// </summary>
        public ulong IdDiscordMsg;
    }
}
