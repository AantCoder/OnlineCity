using System;
using System.Collections.Generic;
using System.Text;
using Transfer.ModelMails;

namespace ServerOnlineCity.Model
{
    public class FMailIncident : IFunctionMail
    {
        public ModelMailStartIncident Mail { get; set; }

        public bool Run(ServiceContext context)
        {
            //todo


            //отправляем вложенное письмо
            context.Player.Mails.Add(Mail);
            return true;
        }

    }
}
