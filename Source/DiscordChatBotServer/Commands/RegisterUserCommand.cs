using Discord.Commands;
using OC.DiscordBotServer.Languages;
using OC.DiscordBotServer.Models;
using OC.DiscordBotServer.Repositories;
using System;
using System.Linq;

namespace OC.DiscordBotServer.Commands
{
    public class RegmeCommand : ICommand
    {
        ApplicationContext _appContext;
        IRepository<OCUser> _userRepository;

        public RegmeCommand(ApplicationContext appContext, IRepository<OCUser> userRepository)
        {
            _appContext = appContext;
            _userRepository = userRepository;
        }

        public string Execute(SocketCommandContext context, string token)
        {
            if (!context.IsPrivate)
            {
                return Translator.InfResctrictInChannel;
            }

            if (!Guid.TryParse(token, out Guid guidToken))
            {
                return Translator.ErrInvalidToken;
            }

            //Check each Online City Server for this User 
            foreach (var ocServerKeyPair in _appContext.DiscrordToOCServer.Where(srv => srv.Value.IsLogined))
            {
                var ocServer = ocServerKeyPair.Value;
                var player = ocServer.GetPlayerByToken(guidToken);
                if (player != null)
                {
                    OCUser user = _appContext.GetOCUser(ocServerKeyPair.Key, context.User.Id);
                    if (user != null)
                    {
                        return string.Format(Translator.ErrUserTokenExist, user.OCLogin, user.DiscordIdChanel);
                    }

                    user = new OCUser() { DiscordIdChanel = ocServerKeyPair.Key, LastActiveTime = player.LastOnlineTime, OCLogin = player.Login, UserId = context.Message.Author.Id };

                    _appContext.UserOnServers[ocServerKeyPair.Key][context.Message.Author.Id] = user;
                    _userRepository.AddNewItem(user);

                    // to do register user  
                    return string.Format(Translator.InfUserFound, player.Login, player.ServerName);
                }
            }

            return "User not found or server is offline";
        }
    }
}
