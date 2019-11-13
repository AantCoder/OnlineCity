using DiscordChatBotServer.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordChatBotServer
{
    public class BotContext : DbContext
    {
        public BotContext() : base("DefaultConnection") // DefaultConnection - save in app.config
        {
            // Указывает EF, что если модель изменилась,
            // нужно воссоздать базу данных с новой структурой
            Database.SetInitializer(new DropCreateDatabaseIfModelChanges<BotContext>());
        }
        public DbSet<Chanel2Server> Chanel2Servers { get; set; }
        public DbSet<OCUser> OCUsers { get; set; }
    }
}
