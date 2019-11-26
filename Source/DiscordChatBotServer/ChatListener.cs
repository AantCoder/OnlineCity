using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OC.DiscordBotServer
{
    public class ChatListener
    {
        private readonly ApplicationContext _applicationContext;
        ChatListener(ApplicationContext applicationContext)
        {
            _applicationContext = applicationContext;
        }

        public Task StartListener() 
        {
            return Task.Run(() => 
            {

                Task.Delay(500);

            });
        }
    }
}
