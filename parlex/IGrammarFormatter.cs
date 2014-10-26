using System.IO;

namespace Parlex {
    public interface IGrammarFormatter {
        void Serialize(Stream s, Grammar grammar);
        Grammar Deserialize(Stream s);
    }
}