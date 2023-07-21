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
        /// Деф рассказчика.
        /// </summary>
        public string StorytellerDef { get; set; }

        /// <summary>
        /// Сложность.
        /// </summary>
        public string Difficulty { get; set; }

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
        /// Модификатор передышки после рейда
        /// </summary>
        public int IncidentCoolDownPercent { get; set; }

        /// <summary>
        /// Модификатор предупреждения перед рейдом
        /// </summary>
        public int IncidentAlarmInHours { get; set; }

        /// <summary>
        /// Включить синхронизацию объектов на планете между всеми игроками (WIP)
        /// </summary>
        public bool EquableWorldObjects { get; set; }

        /// <summary>
        /// Включить биржу (WIP)
        /// </summary>
        public bool ExchengeEnable { get; set; }

        /// <summary>
        /// включить выбор старта
        /// </summary>
        public bool ScenarioAviable { get; set; }

        /// <summary>
        /// Стоимость вещей на бирже, в сделках и на счету для рейдов на 1000 серебра (50 - это 5%). Рекомендация 1000 или 1200.
        /// </summary>
        public int ExchengePrecentWealthForIncident { get; set; }

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
        /// Перечисленные через запятую defName вещей, которые запрещены к передаче.
        /// </summary>
        public string ExchengeForbiddenDefNames { get; set; }
        [NonSerialized]
        private HashSet<string> ExchengeForbiddenDefNamesListData;
        public HashSet<string> ExchengeForbiddenDefNamesList
        {
            get
            {
                if (ExchengeForbiddenDefNamesListData == null)
                    if (string.IsNullOrEmpty(ExchengeForbiddenDefNames))
                        ExchengeForbiddenDefNamesListData = new HashSet<string>();
                    else
                        ExchengeForbiddenDefNamesListData = new HashSet<string>(ExchengeForbiddenDefNames.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()));
                return ExchengeForbiddenDefNamesListData;
            }
        }

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

        /// <summary>
        /// Делать снимки колоний каждый полдень
        /// </summary>
        public bool ColonyScreenEnable { get; set; }

        /// <summary>
        /// Высокое качество снимков колоний
        /// </summary>
        public bool ColonyScreenHighQuality { get; set; }

        /// <summary>
        /// Через сколько дней делать снимки колоний. От 1 до 60 (больше 59 снимок 1 раз в год)
        /// </summary>
        public int ColonyScreenDelayDays { get; set; }

        public ServerGeneralSettings SetDefault()
        {
            StorytellerDef = "";

            Difficulty = "";

            EnablePVP = false;

            DisableGameSettings = false;

            IncidentEnable = true;

            IncidentCountInOffline = 2;

            IncidentMaxMult = 10;

            IncidentTickDelayBetween = 60000;

            IncidentCostPrecent = 100;

            IncidentPowerPrecent = 100;

            IncidentCoolDownPercent = 100;

            IncidentAlarmInHours = 10;

            EquableWorldObjects = false;

            ScenarioAviable = true;

            ExchengeEnable = true;

            ExchengePrecentWealthForIncident = 1000;

            ExchengePrecentCommissionConvertToCashlessCurrency = 50;

            ExchengeCostCargoDelivery = 1000;

            ExchengeAddPrecentCostForFastCargoDelivery = 100;

            StartGameYear = -1;

            ColonyScreenEnable = true;
            
            ColonyScreenHighQuality = true;

            ColonyScreenDelayDays = 1;

            return this;
        }

    }
}
