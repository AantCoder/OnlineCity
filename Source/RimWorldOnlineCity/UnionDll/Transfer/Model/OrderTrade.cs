using Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Transfer
{
    [Serializable]
    public class OrderTrade
    {
        /// <summary>
        /// Id на сервере. 
        /// Если 0 - то добавить новый.
        /// Если больше 0 - отредактировать
        /// Если меньше 0 - удалить c Id без минуса
        /// </summary>
        public long Id { get; set; }

        /// <summary>
        /// Сделано на основе др. ордера.
        /// При id=0, если эта основа предназначена только для одного игрока, она удаляется
        /// </summary>
        public long ByBaseId { get; set; }

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
        /// Вещи которые получит Owner. Могут быть с любым Concrete
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
///Не авктвен выбор своих бъектов, если не редактирвется ордер
///Функция Встречное предложение переворачивает шелл и бай
///
///Ввести цену в $ справа?
///Ввести целостность слева?
///Добавить ServerId и при встречном предложении не только создается ордер а и удаляется чужой (проверка, что он предложен строго одному адресату)
