using System;
using System.Text.RegularExpressions;

namespace Parlex
{
	// http://standards.iso.org/ittf/PubliclyAvailableStandards/s026153_ISO_IEC_14977_1996(E).zip
	internal static class ExtendedBackusNaurForm
	{
		static Regex _nameValidation = new Regex ("^[a-zA-Z0-9]+$");

		internal class SyntaxRule {
			internal interface RewriteRuleNode {
			}

			internal class OrNode : RewriteRuleNode {
				internal RewriteRuleNode left;
				internal RewriteRuleNode right;
			}

			internal class AndNode : RewriteRuleNode {
				internal RewriteRuleNode left;
				internal RewriteRuleNode right;
			}

            internal class ConcatenationNode : RewriteRuleNode
            {
				internal RewriteRuleNode left;
				internal RewriteRuleNode right;
			}

            internal class OptionalNode : RewriteRuleNode
            {
				internal RewriteRuleNode contents;
			}

            internal class RepetitionNode : RewriteRuleNode
            {
				internal RewriteRuleNode contents;
			}

			internal class SyntacticFactorNode : RewriteRuleNode {
				internal int count;
				internal RewriteRuleNode contents;
			}

			internal class TerminalNode : RewriteRuleNode {
				internal Grammar.StringTerminal terminalString;
			}

			internal class NonterminalNode : RewriteRuleNode {
				internal SyntaxRule productionSymbol;
			}

			internal class CommentNode : RewriteRuleNode {
				internal String text;
			}

			internal class SpecialSequenceNode : RewriteRuleNode {
				internal String text;
			}

			internal String metaIdentifier;
			//RewriteRuleNode definition;
		}
	}
}

