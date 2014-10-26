using System;

namespace Parlex {
    public interface IGrammarFormatter{
        void Serialize(System.IO.Stream s, Grammar grammar);
        Grammar Deserialize(System.IO.Stream s);
    }
}