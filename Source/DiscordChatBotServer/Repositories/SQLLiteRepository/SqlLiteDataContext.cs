//using OC.DiscordBotServer.Models;
using Microsoft.EntityFrameworkCore;
using OC.DiscordBotServer.Repositories.SQLLiteRepository.ModelMapping;
using OC.DiscordBotServer.Models;

namespace OC.DiscordBotServer
{
    // Tools -> Extensions and Updates. И здесь среди всех расширений нам надо установить расширение SQLite for Universal App Platform:
    public class SqlLiteDataContext : DbContext
    {
        private readonly string _connectionString;
        public SqlLiteDataContext(string connectionString) : base() // DefaultConnection - save in app.config
        {
            _connectionString = connectionString;
            Database.EnsureCreated();
        }
        public DbSet<Repositories.SQLLiteRepository.ModelMapping.Chanel2Server> Chanel2Servers { get; set; }
         //public DbSet<OCUser> OCUsers { get; set; }
        // modelBuilder.Conventions.Remove<IncludeMetadataConvention>();

        // after release add support migrate version Db here
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite(_connectionString);

        }
    }
}