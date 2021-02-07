using Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Transfer.ModelMails
{
    /// <summary>
    /// Послыка от каравана другого игрока
    /// </summary>
    [Serializable]
    public class ModelMailTrade : ModelMail, IModelMailPlace
    {
        public int Tile { get; set; }
        public long PlaceServerId { get; set; }
        public List<ThingEntry> Things { get; set; }

        public override string GetHash()
        {
            return $"T{Tile}P{PlaceServerId} " + ContentString();
        }

        public override string ContentString()
        {
            return Things == null ? "" : Things.Aggregate("", (r, i) => r + Environment.NewLine + i.Name + " x" + i.Count);
        }
    }

}
