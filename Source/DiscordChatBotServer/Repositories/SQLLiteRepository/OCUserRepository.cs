using System.Collections.Generic;
using System.Linq;
using OC.DiscordBotServer.Models;

namespace OC.DiscordBotServer.Repositories
{
    public class OCUserRepository : IRepository<OCUser>
    {
        private readonly BotDataContext _dataContext;

        public OCUserRepository(BotDataContext dataContext)
        {
            _dataContext = dataContext;
        }

        public bool AddNewItem(OCUser ocUser)
        {
            using (var tran = _dataContext.Database.BeginTransaction())
            {
                _dataContext.OCUsers.Add(ocUser);
                tran.Commit();
            }

            _dataContext.SaveChanges();
            return true;
        }

        public void Delete(IEnumerable<OCUser> users)
        {
            using (var tran = _dataContext.Database.BeginTransaction())
            {
                _dataContext.OCUsers.RemoveRange(users);
                tran.Commit();
            }
        }

        public IReadOnlyList<OCUser> GetAll()
        {

            return _dataContext.OCUsers.ToList().AsReadOnly();
        }
    }
}
