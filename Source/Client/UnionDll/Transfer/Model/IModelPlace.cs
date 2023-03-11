using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Model
{

    public interface IModelPlace
    {
        int Tile { get; set; }

        /// <summary>
        /// Id с сервера, соответствующий определенному игровому объекту WorldObject
        /// </summary>
        long PlaceServerId { get; set; }
    }
}
