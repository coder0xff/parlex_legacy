using System;

namespace Parlex {
    internal class InvalidNfaOperationException : Exception {
        public InvalidNfaOperationException(string p) : base(p) {}
    }
}