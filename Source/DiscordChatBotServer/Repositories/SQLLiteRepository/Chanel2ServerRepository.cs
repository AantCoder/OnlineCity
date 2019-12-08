using OC.DiscordBotServer.Models;
using System.Collections.Generic;
using System.Linq;

namespace OC.DiscordBotServer.Repositories
{
    public class Chanel2ServerRepository : IRepository <Chanel2Server>
    {
        private readonly BotDataContext _dataContext;

        public Chanel2ServerRepository(BotDataContext dataContext)
        {
            _dataContext = dataContext;
        }

        public bool AddNewItem(Chanel2Server server)
        {
            using (var tran = _dataContext.Database.BeginTransaction())
            {
                _dataContext.Chanel2Servers.Add(server);
                tran.Commit();
            }

            _dataContext.SaveChanges();
            return true;
        }

        public IReadOnlyList<Chanel2Server> GetAll()
        {
            return _dataContext.Chanel2Servers.ToList().AsReadOnly();
        }

        public void Delete(IEnumerable<Chanel2Server> servers)
        {
            _dataContext.Chanel2Servers.RemoveRange(servers);
        }
    }
}