using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace parlex
{
    class Analyzer
    {
        class SymbolClassNode
        {
            public int SpanStart;
            public SymbolClassNode[][] RelationBranches;
            public SymbolClass SymbolClass;

            public SymbolClassNode(int spanStart, int spanLength, SymbolClass symbolClass)
            {
                SpanStart = spanStart;
                RelationBranches = new SymbolClassNode[spanLength + 1][]; //+1 to find what comes after this
                SymbolClass = symbolClass;
            }
        }

        void CreateRelations(GrammarExampleEntry entry)
        {
            int length = entry.Text.Length;
            List<SymbolClassNode>[] temp = new List<SymbolClassNode>[length];
            for (int init = 0; init < length; init++) temp[init] = new List<SymbolClassNode>();
            foreach (SymbolClassSpan span in entry.SymbolClassSpans)
            {
                temp[span.SpanStart].Add(new SymbolClassNode(span.SpanStart, span.SpanLength, span.SymbolClass));                
            }
            SymbolClassNode[][] temp2 = new SymbolClassNode[length][];
            for (int arrayize = 0; arrayize < length; arrayize++) temp2[arrayize] = temp[arrayize].ToArray();
            HashSet<SymbolClassNode> currentlyEnteredNodes = new HashSet<SymbolClassNode>();
            for (int startIndex = 0; startIndex < length; startIndex++)
            {
                foreach (SymbolClassNode node in temp2[startIndex])
                {
                    currentlyEnteredNodes.Add(node);
                }
                foreach (SymbolClassNode node in currentlyEnteredNodes)
                {

                }
            }
        }
        void CreateHierarchies(IEnumerable<GrammarExampleEntry> entries)
        {

        }
        public void Analyze(IEnumerable<GrammarExampleEntry> entries) {
            
        }
    }
}
