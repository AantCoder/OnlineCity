using Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Transfer.ModelMails
{
    /// <summary>
    /// Родительский класс для писем. Какое именно действие будет осуществлено на клиенте при получении письма см. класс RimWorldOnlineCity.MailController
    /// </summary>
    [Serializable]
    public abstract class ModelMail
    {
        /// <summary>
        /// Время когда было добавлено на сервер в первый раз, пока служит только для уникальности письма
        /// </summary>
        public DateTime Created { get; set; } 
        public Player From { get; set; }
        public Player To { get; set; }

        public bool NeedSaveGame { get; set; }

        public ModelMail()
        {
            Created = DateTime.UtcNow;
        }

        public virtual string ContentString()
        {
            return this.GetType().Name;
        }

        public abstract string GetHash();

        public int GetHashBase()
        {
            return (GetType().Name + $":pf{From?.Login ?? "-"}pt{To?.Login ?? "-"}с{Created.Ticks}:" + GetHash()).GetHashCode();
        }
    }

    public interface IModelMailPlace
    {
        int Tile { get; set; }
        long PlaceServerId { get; set; }
    }

}
