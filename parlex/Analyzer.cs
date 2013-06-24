using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace parlex
{
    class Analyzer
    {
        //The NFA subset created for a particular example of a particular product.
        //While the sub-products it utilizes will be NFAs all their own, this one follows
        //a particular sequence, and each element must be satisfied
        public class NfaSequence
        {
            public int SpanStart;
            public readonly List<Product>[] RelationBranches;
            public Product OwnerProduct;

            public NfaSequence(int spanStart, int spanLength, Product ownerProduct)
            {
                SpanStart = spanStart;
                RelationBranches = new List<Product>[spanLength + 1]; //+1 to find what comes after this
                for (int initBranches = 0; initBranches < spanLength + 1; initBranches++)
                {
                    RelationBranches[initBranches] = new List<Product>();
                }
                OwnerProduct = ownerProduct;
            }
        }

        void CreateRelations(Exemplar entry)
        {
            int length = entry.Text.Length;
            var sequencesByStartIndex = new List<NfaSequence>[length];
            for (int init = 0; init < length; init++) sequencesByStartIndex[init] = new List<NfaSequence>();
            foreach (ProductSpan span in entry.ProductSpans)
            {
                var sequence = new NfaSequence(span.SpanStart, span.SpanLength,
                                                                         span.Product);
                sequencesByStartIndex[span.SpanStart].Add(sequence);
                span.Product.Sequences.Add(sequence);
            }
            var currentlyEnteredSequences = new HashSet<NfaSequence>();
            for (int startIndex = 0; startIndex < length; startIndex++)
            {
                foreach (NfaSequence node in sequencesByStartIndex[startIndex])
                {
                    currentlyEnteredSequences.Add(node);
                }
                var toRemove = new HashSet<NfaSequence>();
                foreach (NfaSequence node in currentlyEnteredSequences)
                {
                    if (node.SpanStart + node.RelationBranches.Length < startIndex)
                    {
                        toRemove.Add(node);
                    }
                }
                foreach (NfaSequence node in toRemove)
                {
                    currentlyEnteredSequences.Remove(node);
                }
                foreach (NfaSequence node in currentlyEnteredSequences)
                {
                    foreach (NfaSequence sequence in sequencesByStartIndex[startIndex])
                    {
                        node.RelationBranches[startIndex - node.SpanStart].Add(sequence.OwnerProduct);
                    }
                }
            }
        }

        void CreateRelations(IEnumerable<Exemplar> entries)
        {
            foreach (Exemplar entry in entries)
            {
                CreateRelations(entry);
            }
        }

        public void Analyze(IEnumerable<Exemplar> entries) {
            var products = entries.SelectMany(x => x.ProductSpans).Select(x => x.Product).ToList();
            foreach (Product product in products)
            {
                product.Sequences.Clear();
            }
            CreateRelations(entries);
        }
    }
}
