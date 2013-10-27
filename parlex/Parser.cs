using System;
using System.Collections.Concurrent;
using System.Collections.Concurrent.More;
using System.Collections.Generic;
using System.Linq;

namespace parlex {
    class Parser {
        public struct AbstractSyntaxReference {
            public bool Equals(AbstractSyntaxReference other) {
                return Equals(Production, other.Production) && Length == other.Length;
            }

            public override bool Equals(object obj) {
                if (ReferenceEquals(null, obj)) return false;
                return obj is AbstractSyntaxReference && Equals((AbstractSyntaxReference) obj);
            }

            public override int GetHashCode() {
                unchecked {
                    return ((Production != null ? Production.GetHashCode() : 0)*397) ^ Length;
                }
            }

            public static bool operator ==(AbstractSyntaxReference left, AbstractSyntaxReference right) {
                return left.Equals(right);
            }

            public static bool operator !=(AbstractSyntaxReference left, AbstractSyntaxReference right) {
                return !left.Equals(right);
            }

            public readonly Production Production;
            public readonly int Length;

            public AbstractSyntaxReference(Production production, int length) {
                Production = production;
                Length = length;
            }
        }

        public struct AbstractSyntaxSequence {
            public readonly System.Collections.ObjectModel.ReadOnlyCollection<AbstractSyntaxReference> Items;

            public AbstractSyntaxSequence(IEnumerable<AbstractSyntaxReference> items) {
                Items = Array.AsReadOnly(items.ToArray());
            }
        }

        interface IDependent {
            void DepenencyFulfilled(AbstractSyntaxReference match);
        }

        class ParseLocation {
            private JaggedAutoDictionary<Production, int, ConcurrentSet<AbstractSyntaxSequence>> ProductionMatches = new JaggedAutoDictionary<Production, int, ConcurrentSet<AbstractSyntaxSequence>>((a, b) => new ConcurrentSet<AbstractSyntaxSequence>());
            private AutoDictionary<Production, ConcurrentBag<IDependent>> Dependents = new AutoDictionary<Production, ConcurrentBag<IDependent>>(_ => new ConcurrentBag<IDependent>());
        }
    }
}
