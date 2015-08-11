using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenPermit
{
    public class OpenPermitAdapterException : Exception
    {
        public OpenPermitAdapterException()
        {
        }

        public OpenPermitAdapterException(string message)
            : base(message)
        {
        }

        public OpenPermitAdapterException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }
}
