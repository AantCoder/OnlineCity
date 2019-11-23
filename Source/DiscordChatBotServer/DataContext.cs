using OC.DiscordBotServer.Models;
using System.Data.Entity;

namespace OC.DiscordBotServer
{
    // Tools -> Extensions and Updates. И здесь среди всех расширений нам надо установить расширение SQLite for Universal App Platform:
    public class DataContext : DbContext
    {
        public DataContext() : base("DefaultConnection") // DefaultConnection - save in app.config
        {
            // Указывает EF, что если модель изменилась,
            // нужно воссоздать базу данных с новой структурой
            /// !!!! Remove it after developing to Migrate !!!!
            Database.SetInitializer(new DropCreateDatabaseIfModelChanges<DataContext>());
            //Database.SetInitializer(new MigrateDatabaseToLatestVersion<DataContext());
        }
        public DbSet<Chanel2Server> Chanel2Servers { get; set; }
        public DbSet<OCUser> OCUsers { get; set; }
    }
}