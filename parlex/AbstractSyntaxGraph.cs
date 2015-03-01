using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Parlex {
    public class AbstractSyntaxGraph {
        internal Dictionary<MatchClass, List<Match>> NodeTable;
        internal MatchClass Root;

        public bool IsEmpty { get { return NodeTable.Keys.Count == 0; } }

        internal bool IsAmbiguous {
            get { return NodeTable.Keys.Any(matchClass => NodeTable[matchClass].Count > 1); }
        }

        internal void StripWhiteSpaceEaters() {
            foreach (var matches in NodeTable.Values) {
                foreach (var match in matches) {
                    match.StripWhiteSpaceEaters();
                }
            }
        }
    }
}
