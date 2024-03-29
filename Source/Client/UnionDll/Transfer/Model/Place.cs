﻿using Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Transfer
{
    [Serializable]
    public class Place //: IModelPlace
    {
        public string ServerName { get; set; }
        public int Tile { get; set; }
        public long PlaceServerId { get; set; }
        public string Name { get; set; }

        [NonSerialized]
        private float DayPath_p;
        /// <summary>
        /// Расстояние пути до точки, временное перезаписывается при отображении или для расчета
        /// </summary>
        public float DayPath
        {
            get { return DayPath_p; }
            set { DayPath_p = value; }
        }
    }
}
