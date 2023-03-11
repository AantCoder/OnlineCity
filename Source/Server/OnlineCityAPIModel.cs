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
        /// Тип запроса:
        /// "s" результат APIResponseStatus - общая информация по онлайну
        /// "p" результат APIResponsePlayers - информация по игроку с ником в Login
        /// "a" результат APIResponsePlayers - информация обо всех зарегистрированных игроках
        /// </summary>
        public string Q { get; set; }
        public string Login { get; set; }
    }

    public abstract class APIResponse
    {
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
    }

}
