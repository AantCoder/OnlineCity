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
        /// Включить режим нападения игроков друг на друга онлайн с ограниченным управлением
        /// </summary>
        public bool EnablePVP { get; set; }

        /// <summary>
        /// Запретить менять настройки рассказчика и модификаций в игре
        /// </summary>
        public bool DisableGameSettings { get; set; }

        /// <summary>
        /// Разрешены ли инценденты
        /// </summary>
        public bool IncidentEnable { get; set; }

        /// <summary>
        /// Сколько разрешено инцендентов в очереди
        /// </summary>
        public int IncidentCountInOffline { get; set; }

        /// <summary>
        /// Максимальный коэф. силы инцендентов
        /// </summary>
        public int IncidentMaxMult { get; set; }

        /// <summary>
        /// Минимальная пауза между инциндентами в тиках (1 день = 60000)
        /// </summary>
        public int IncidentTickDelayBetween { get; set; }

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

        /// <summary>
        /// Назначит стартовый год в игре вместо 5500
        /// </summary>
        public int StartGameYear { get; set; }

        /// <summary>
        /// Предупреждение при входе на сервер.
        /// </summary>
        public string EntranceWarning { get; set; }

        /// <summary>
        /// Предупреждение при входе на сервер. На русском.
        /// </summary>
        public string EntranceWarningRussian { get; set; }

        public ServerGeneralSettings SetDefault()
        {
            EnablePVP = true;

            DisableGameSettings = false;

            IncidentEnable = true;

            IncidentCountInOffline = 2;

            IncidentMaxMult = 10;

            IncidentTickDelayBetween = 60000;

            IncidentCostPrecent = 100;

            IncidentPowerPrecent = 100;

            EquableWorldObjects = false;

            ExchengeEnable = true;

            ExchengePrecentCommissionConvertToCashlessCurrency = 50;

            ExchengeCostCargoDelivery = 1000;

            ExchengeAddPrecentCostForFastCargoDelivery = 100;

            StartGameYear = -1;

            return this;
        }

    }
}
