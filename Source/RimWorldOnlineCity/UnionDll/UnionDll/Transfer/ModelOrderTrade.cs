using Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Transfer
{
    [Serializable]
    public class ModelOrderTrade
    {
        /// <summary>
        /// Создатель сделки
        /// </summary>
        public Player Owner { get; set; }

        /// <summary>
        /// Где находиться товар
        /// </summary>
        public Place Place { get; set; }

        /// <summary>
        /// Время размещения на сервере
        /// </summary>
        public DateTime Created  { get; set; }

        /// <summary>
        /// Вещи (тип и кол-во) которые отдаст Owner. Все должны быть Concrete = true
        /// </summary>
        public List<ThingTrade> SellThings { get; set; }

        /// <summary>
        /// Вещи которые получит Owner. Все должны быть Concrete = false
        /// </summary>
        public List<ThingTrade> BuyThings { get; set; }

        /// <summary>
        /// Максимальное кол-во сделок установленное создателем
        /// </summary>
        public int CountBeginMax { get; set; }

        /// <summary>
        /// Кол-во сделок успешно реализованнх
        /// </summary>
        public int CountFnished { get; set; }

        /// <summary>
        /// Кол-во доступных сделок. Равно CountBeginMax - CountFnished, но не больше текущих возможностей создателя сделки.
        /// </summary>
        public int CountReady { get; set; }

        /// <summary>
        /// Если список не пустой, то это приватная сделка доступная только перечисленным игрокам
        /// </summary>
        public List<Player> PrivatPlayers { get; set; }
    }
}
///todo:
///Функция изменнено (и в нем CountReady)
///Событие по добавлению своего объекта на продажу
///Не авктвен выбор своих бъектов, если не редактирвется ордер
///Функция Встречное предложение переворачивает шелл и бай и меняет конкрете
///Функция поиска реальных вещей по записи (для конкрете максимально похоже с признаком не совпадения (например ниже качество))
///Редактирование приватные пользователей