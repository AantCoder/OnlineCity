using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Transfer;

namespace OC.DiscordBotServer.Commands
{
    public class RegisterUserCommand : ICommand
    {
        ApplicationContext _appContext;
        public RegisterUserCommand(ApplicationContext appContext)
        {
            _appContext = appContext;
        }

        public string Execute(SocketCommandContext context, string token)
        {
            token = token.ToUpper();
            if (!Guid.TryParse(token, out Guid guidToken))
            {
                return Languages.Translator.ErrInvalidToken;
            }

            // fast check
            foreach (var usersOnServer in _appContext.UserOnServers.Values)
            {
                //var userInfo = usersOnServer.Values.FirstOrDefault(x => x.Token.Equals(token));
                
                //if (usersOnServer.Values.Any(x => x.Token.Equals(token))) 
                //{                    
                //    return string.Format(Languages.Translator.ErrUserTokenExist, userInfo.OCLogin, userInfo.DiscordIdChanel);
                //}
            }

            //Check each Online City Server for this User 
            foreach (var ocServer in _appContext.DiscrordToOCServer.Where (x => x.Value.IsLogined))
            {
                var requestPackage = new ModelPostingChat() { ChatId = (long)context.Message.Author.Id, Message = token };
            }

            return string.Empty;
        }
    }
}
