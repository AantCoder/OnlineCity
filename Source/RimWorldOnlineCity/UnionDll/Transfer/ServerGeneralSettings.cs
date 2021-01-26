using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OCUnion
{
    [Serializable]
    public struct ServerGeneralSettings
    {

        /// <summary>
        /// Разрешены ли инценденты
        /// </summary>
        public bool IncidentEnable { get; set; }

        /// <summary>
        /// Сколько разрешено инцендентов за 10 игровых дней
        /// </summary>
        public int IncidentCountIn10Days { get; set; }

        /// <summary>
        /// Модификатор стоимости найма в процентах
        /// </summary>
        public int IncidentCostPrecent { get; set; }

        /// <summary>
        /// Модификатор силы рейдов в процентах
        /// </summary>
        public int IncidentPowerPrecent { get; set; }

        /// <summary>
        /// Включить синхронизацию объектов на планете между всеми игроками (WIP)
        /// </summary>
        public bool EquableWorldObjects { get; set; }

        /// <summary>
        /// Включить биржу (WIP)
        /// </summary>
        public bool ExchengeEnable { get; set; }

        /// <summary>
        /// Комиссия перевода 1000 серебра его в безналичный счет или назад (50 - это 5%)
        /// </summary>
        public int ExchengePrecentCommissionConvertToCashlessCurrency { get; set; }

        /// <summary>
        /// Стоимость доставки 1000 серебра на расстояние 100 клеток (это примерно 1/15 планеты по экватору, а если /10 то сколько стоит доставка соседям)
        /// </summary>
        public int ExchengeCostCargoDelivery { get; set; }

        /// <summary>
        /// Процент надбавки цены за быструю доставку
        /// </summary>
        public int ExchengeAddPrecentCostForFastCargoDelivery { get; set; }

        public ServerGeneralSettings SetDefault()
        {
            IncidentEnable = true;

            IncidentCountIn10Days = 10;

            IncidentCostPrecent = 100;

            IncidentPowerPrecent = 100;

            EquableWorldObjects = false;

            ExchengeEnable = true;

            ExchengePrecentCommissionConvertToCashlessCurrency = 50;

            ExchengeCostCargoDelivery = 1000;

            ExchengeAddPrecentCostForFastCargoDelivery = 100;

            return this;
        }

    }
}
