using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Parlex
{
    class InvalidNfaOperationException : Exception
    {
        public InvalidNfaOperationException(string p) : base(p) { }
    }
}
