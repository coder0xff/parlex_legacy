using System;

namespace Parlex {
    public class ParseException : Exception {
        public ParseException(string message) : base(message) {
            
        }
    }
}