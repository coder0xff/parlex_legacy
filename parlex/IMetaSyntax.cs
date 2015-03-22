using System.IO;

namespace Parlex {
    public interface IMetaSyntax {
        void Generate(Stream s, Grammar grammar);
        Grammar Parse(Stream s);
    }
}