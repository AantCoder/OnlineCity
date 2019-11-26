using Microsoft.EntityFrameworkCore;
using OC.DiscordBotServer.Models;

namespace OC.DiscordBotServer
{
    // Tools -> Extensions and Updates. И здесь среди всех расширений нам надо установить расширение SQLite for Universal App Platform:
    public class BotDataContext : DbContext
    {
        private readonly string _connectionString;
        public BotDataContext(string connectionString) : base()
        {
            _connectionString = connectionString;
            Database.EnsureCreated();
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite(_connectionString);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<OCUser>()
                .HasKey(u => new { u.DiscordIdChanel,u.UserId});
        }

        public DbSet<Chanel2Server> Chanel2Servers { get; set; }
        public DbSet<OCUser> OCUsers { get; set; }

        // after release add support migrate version Db here
    }
}