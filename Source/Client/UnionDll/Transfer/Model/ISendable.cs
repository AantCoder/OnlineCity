using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OCUnion.Transfer.Model
{
    public interface ISendable
    {
        PackageType PackageType { get; }
    }
}
