using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OCUnion.Transfer.Model
{
    [Serializable]
    public class ModelPlayerInfo
    {
        public int DelaySaveGame { get; set; }

        public bool EnablePVP { get; set; }

        public string DiscordUserName { get; set; }

        public string EMail { get; set; }

        public string AboutMyTextBox { get; set; }
    }
}
