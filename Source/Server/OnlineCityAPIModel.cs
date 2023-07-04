using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServerOnlineCity
{
    public class APIRequest
    {
        /// <summary>
        /// Тип запроса (регистр не важен):
        /// "s" результат APIResponseStatus - общая информация по онлайну
        /// "p" результат APIResponsePlayers - информация по игроку с ником в Login
        /// "a" результат APIResponsePlayers - информация обо всех зарегистрированных игроках
        /// "PlayerStat" результат APIResponseRawData, заполнить Key для доступа - отчет обо всех игроках, который можно получить нажав в консоле сервера S
        /// "BlockKey" результат APIResponseRawData, заполнить Key для доступа - передает содержимое blockkey.txt с сервера
        /// "cc" заполнить N и Key для доступа - выполняет однобуквенную команду в консоли, как если бы она была нажата на сервере
        /// "cp" заполнить N и Key для доступа - пишет N в общий чат игры, как выполнение command.txt
        /// "pa" результат APIResponseStatus, заполнить Key для доступа - передать подтверждение регистрации пользователя (в Login списком через *) и отказ в регистрации (ники игроков к удалению в N через *)
        /// "li" результат APIResponseRawData, заполнить N и Key для доступа - получить изображение
        /// "si" заполнить N, Data и Key для доступа - сохранить изображение
        /// </summary>
        public string Q { get; set; }
        public int T { get; set; }
        public string Login { get; set; }
        public string N { get; set; }
        public string Key { get; set; }
        public byte[] Data { get; set; }
    }

    public abstract class APIResponse
    {
    }

    public class APIResponseRawData : APIResponse
    {
        public byte[] Data { get; set; }
    }

    public class APIResponseError : APIResponse
    {
        public string Error { get; set; }
    }

    public class APIResponseStatus : APIResponse
    {
        /// <summary>
        /// Кол-во игроков онлайн
        /// </summary>
        public int OnlineCount { get; set; }
        /// <summary>
        /// Кол-во зарегистрированно игроков
        /// </summary>
        public int PlayerCount { get; set; }
        /// <summary>
        /// Список ников игроков онлайн
        /// </summary>
        public List<string> Onlines { get; set; }
        /// <summary>
        /// Игроки требующие подтверждения после регистрации. Здесь логин*discordИмя
        /// </summary>
        public List<string> NeedApprove { get; set; }
    }

    public class APIResponsePlayers : APIResponse
    {
        public List<APIPlayer> Players { get; set; }
    }

    public class APIPlayer
    {
        public string Login { get; set; }

        public string DiscordUserName { get; set; }

        /// <summary>
        /// Время последней активности в игре по гринвичу
        /// </summary>
        public DateTime LastOnlineTime { get; set; }

        /// <summary>
        /// Кол-во игровых дней. Days / 60 = годов
        /// </summary>
        public long Days { get; set; }

        /// <summary>
        /// Кол-во поселений игрока
        /// </summary>
        public int BaseCount { get; set; }

        /// <summary>
        /// Кол-во караванов игрока
        /// </summary>
        public int CaravanCount { get; set; }

        /// <summary>
        /// Общая игровая стоимость
        /// </summary>
        public float MarketValueTotal { get; set; }

        /// <summary>
        /// Id поселений игрока разделенные запятой
        /// </summary>
        public string BaseServerIds { get; set; }
    }

}
