using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Parlex {
    class CSharpParserGenerator : IParserGenerator {
        private static String CSharpName(String name) {
            bool capitalized = true;
            var result = new StringBuilder();
            foreach (var t in name) {
                var c = t;
                if (Char.IsLetter(c)) {
                    c = capitalized ? Char.ToUpper(c) : Char.ToLower(c);
                    result.Append(c);
                    capitalized = false;
                    continue;
                }
                capitalized = false;
                if (c == '_') {
                    capitalized = true;
                    continue;
                }
                result.Append(c);
            }
            return result.ToString();
        }

        private String _namespace;

        public CSharpParserGenerator(String @namespace) {
            _namespace = @namespace;
        }
        public void Generate(string destinationDirectory, Grammar grammar) {
            foreach (var production in grammar.Productions) {
                var cSharpName = CSharpName(production.Name);
                var fileName = destinationDirectory + "/" + cSharpName + ".cs";
                var builder = new StringBuilder();
                builder.AppendLine("using Parlex;");
                builder.AppendLine();
                builder.AppendLine("namespace " + _namespace + " {");
                builder.AppendLine("\tpartial class " + cSharpName + " : SyntaxNode {");
                builder.AppendLine("\t\tvoid StartState(int position) {");
                builder.AppendLine("\t}");
                builder.AppendLine("}");
            }
        }
    }
}
