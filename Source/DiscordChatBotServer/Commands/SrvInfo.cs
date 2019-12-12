using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OC.DiscordBotServer.Commands
{
    public class SrvInfo : ICommand
    {
        public SrvInfo()
        {

        }
    }
}


/*
 !OC srvinfo this( этот сервера)
srvinfo my         ( все где я зарегестрирован)
srvinfo all          (информация по всем серверам, отсортированные 1. По количеству пользователей от максимального к минимальному, по дате поледнего онлайна)


1.	Когда был онлайн последний раз
2.	Последнее время проверки 



     */
