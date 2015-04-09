using System;
using System.Text.RegularExpressions;

namespace Parlex {
    // http://standards.iso.org/ittf/PubliclyAvailableStandards/s026153_ISO_IEC_14977_1996(E).zip
    internal static class ExtendedBackusNaurForm {
        private static Regex _nameValidation = new Regex("^[a-zA-Z0-9]+$");

        internal class SyntaxRule {
            internal String metaIdentifier;

            internal class AndNode : RewriteRuleNode {
                internal RewriteRuleNode left;
                internal RewriteRuleNode right;
            }

            internal class CommentNode : RewriteRuleNode {
                internal String text;
            }

            internal class ConcatenationNode : RewriteRuleNode {
                internal RewriteRuleNode left;
                internal RewriteRuleNode right;
            }

            internal class NonterminalNode : RewriteRuleNode {
                internal SyntaxRule productionSymbol;
            }

            internal class OptionalNode : RewriteRuleNode {
                internal RewriteRuleNode contents;
            }

            internal class OrNode : RewriteRuleNode {
                internal RewriteRuleNode left;
                internal RewriteRuleNode right;
            }

            internal class RepetitionNode : RewriteRuleNode {
                internal RewriteRuleNode contents;
            }

            internal interface RewriteRuleNode {}

            internal class SpecialSequenceNode : RewriteRuleNode {
                internal String text;
            }

            internal class SyntacticFactorNode : RewriteRuleNode {
                internal RewriteRuleNode contents;
                internal int count;
            }

            internal class TerminalNode : RewriteRuleNode {
                internal StringTerminal TerminalString;
            }

            //RewriteRuleNode definition;
        }
    }
}