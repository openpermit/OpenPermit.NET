using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenPermit
{
    public interface IOpenPermitAdapterFactory
    {
        IOpenPermitAdapter GetOpenPermitAdapter(OpenPermitContext context = null);
    }
}
