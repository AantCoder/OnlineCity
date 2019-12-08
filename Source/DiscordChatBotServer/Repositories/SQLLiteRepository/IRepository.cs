using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OC.DiscordBotServer.Repositories
{
    public interface IRepository<TEntity>
        where TEntity : class
    {
        bool AddNewItem(TEntity entity);
        IReadOnlyList<TEntity> GetAll();
        void Delete( IEnumerable<TEntity> entityes);
    }
}
