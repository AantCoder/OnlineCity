using System;
using System.Collections.Generic;
using System.Text;

namespace ServerOnlineCity.Model
{
    public interface IFunctionMail
    {
        /// <summary>
        /// Обработка произвольных функций, каждый раз перед отправкой обычных писем
        /// </summary>
        /// <param name="context"></param>
        /// <returns>Нужно ли удалить это функциональное письмо</returns>
        bool Run(ServiceContext context);
    }
}
