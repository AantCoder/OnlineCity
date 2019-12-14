using System;
using System.Collections.Generic;

namespace Transfer
{
    [Serializable]
    public class ModelLogin
    {
        public string Login { get; set; }
        public string Pass { get; set; }
        public Dictionary<string, string> ModsID { get; set; } = new Dictionary<string, string>();
    }
}
