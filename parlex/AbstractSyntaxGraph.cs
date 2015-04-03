using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Parlex {
    public class AbstractSyntaxGraph {
        public Dictionary<MatchClass, List<Match>> NodeTable;
        public MatchClass Root;

        public bool IsEmpty { get { return NodeTable.Keys.Count == 0; } }

        public bool IsAmbiguous {
            get { return NodeTable.Keys.Any(matchClass => NodeTable[matchClass].Count > 1); }
        }

        internal void StripWhiteSpaceEaters() {
            foreach (var matches in NodeTable.Values) {
                foreach (var match in matches) {
                    match.StripWhiteSpaceEaters();
                }
            }
        }

        private void RenderVisualization(StringBuilder builder, Match match, int indent) {
            builder.Append(new string(' ', indent));
            builder.AppendLine("Match :");
            foreach (var matchClass in match.Children) {
                RenderVisualization(builder, matchClass, indent + 4);
            }
        }

        private void RenderVisualization(StringBuilder builder, MatchClass matchClass, int indent) {
            builder.Append(new string(' ', indent));
            builder.Append(matchClass.Symbol.Name);
            builder.Append(" ");
            builder.Append(matchClass.Position);
            builder.Append(" ");
            builder.Append(matchClass.Length.ToString());
            builder.Append(" : ");
            var firstMatch = NodeTable[matchClass].First();
            var text = firstMatch.Engine.Document.Utf32Substring(matchClass.Position, matchClass.Length);
            builder.AppendLine(Util.QuoteStringLiteral(text));
            foreach (var match in NodeTable[matchClass]) {
                RenderVisualization(builder, match, indent + 4);
            }
        }

        public String RenderVisualization() {
            var builder = new StringBuilder();
            RenderVisualization(builder, Root, 0);
            return builder.ToString();
        }
    }
}
