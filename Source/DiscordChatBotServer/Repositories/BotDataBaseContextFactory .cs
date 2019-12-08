using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OC.DiscordBotServer.Repositories
{
    /// <summary>
    /// Do not delete, this class provide DB Migration
    /// </summary>
    public class BotDataBaseContextFactory : IDesignTimeDbContextFactory<BotDataContext>
    {
        public BotDataContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<BotDataContext>();
            optionsBuilder.UseSqlite("Data Source=blog.db");

            return new BotDataContext(optionsBuilder.Options);
        }
    }
}
