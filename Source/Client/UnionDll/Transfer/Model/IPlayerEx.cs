using Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Model
{
    public interface IPlayerEx
    {
        Player Public { get; }

        bool Online { get; }

        int MinutesIntervalBetweenPVP { get; }

        WorldObjectsValues CostWorldObjects(long serverId = 0);
    }

    public class WorldObjectsValues
    {
        public float MarketValue = 0;
        public float MarketValuePawn = 0;
        public float MarketValueStorage = 0;
        public float MarketValueBalance = 0;
        public float MarketValueTotal => MarketValue + MarketValuePawn + MarketValueStorage + MarketValueBalance;
        public int BaseCount = 0;
        public int CaravanCount = 0;
        public string Details;
        public string BaseServerIds;
    }
}
