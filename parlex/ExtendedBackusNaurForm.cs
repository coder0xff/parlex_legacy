using System;
using System.Text;
using System.Text.RegularExpressions;

namespace Parlex
{
	// http://standards.iso.org/ittf/PubliclyAvailableStandards/s026153_ISO_IEC_14977_1996(E).zip
	public class ExtendedBackusNaurForm
	{
		static Regex nameValidation = new Regex ("^[a-zA-Z0-9]+$");

		public class SyntaxRule {
			public interface RewriteRuleNode {
			}

			public class OrNode : RewriteRuleNode {
				public RewriteRuleNode left;
				public RewriteRuleNode right;
			}

			public class AndNode : RewriteRuleNode {
				public RewriteRuleNode left;
				public RewriteRuleNode right;
			}

			public class ConcatenationNode : RewriteRuleNode {
				public RewriteRuleNode left;
				public RewriteRuleNode right;
			}

			public class OptionalNode : RewriteRuleNode {
				public RewriteRuleNode contents;
			}

			public class RepetitionNode : RewriteRuleNode {
				public RewriteRuleNode contents;
			}

			public class SyntacticFactorNode : RewriteRuleNode {
				public int count;
				public RewriteRuleNode contents;
			}

			public class TerminalNode : RewriteRuleNode {
				public TerminalString terminalString;
			}

			public class NonterminalNode : RewriteRuleNode {
				public SyntaxRule productionSymbol;
			}

			public class CommentNode : RewriteRuleNode {
				public String text;
			}

			public class SpecialSequenceNode : RewriteRuleNode {
				public String text;
			}

			public String metaIdentifier;
			//RewriteRuleNode definition;
		}
	}
}

