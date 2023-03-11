using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sidekick.Sidekick.Model
{
    public interface ILoaderImage : IDisposable
    {
        OCVImage Get(string name);

    }
}
