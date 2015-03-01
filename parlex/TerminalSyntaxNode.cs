namespace Parlex {
    public class TerminalSyntaxNode : SyntaxNode {
        private readonly Grammar.ITerminal _terminal;
        public Grammar.ITerminal Terminal {
            get { return _terminal; }
        }
        public TerminalSyntaxNode(Grammar.ITerminal terminal) {
            _terminal = terminal;
        }

        public override void Start() {
            if (!_terminal.Matches(Engine.CodePoints, Position)) {
                return;
            }
            Position += _terminal.Length;
            Accept();
        }

        public override void OnCompletion(NodeParseResult result) {
        }

        public string Name { get { return _terminal.Name; } }
        public bool IsGreedy { get { return false; } }
        public SyntaxNode Create() {
            return this;
        }
    }
}