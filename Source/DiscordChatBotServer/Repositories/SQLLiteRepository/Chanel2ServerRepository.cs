using OC.DiscordBotServer.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OC.DiscordBotServer.Repositories
{
    public class Chanel2ServerRepository : IRepository
    {
        private readonly SqlLiteDataContext _dataContext;

        public Chanel2ServerRepository(SqlLiteDataContext dataContext)
        {
            _dataContext = dataContext;
        }

        public void AddNewServer(OC.DiscordBotServer.Models.Chanel2Server server)
        {
            using (var tran = _dataContext.Database.BeginTransaction())
            {
               // _dataContext.Chanel2Servers.Add(new SQLLiteRepository.ModelMapping.Chanel2Server(server));
                tran.Commit();
            }

            _dataContext.SaveChanges();
        }
    }
}