using System.IO;

namespace Parlex {
    public interface IMetaSyntax {
        void Generate(Stream stream, Grammar grammar);
        Grammar Parse(Stream stream);
    }
}