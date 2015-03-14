namespace Parlex {
    public class TerminalSyntaxNode : SyntaxNode {
        private readonly ITerminal _terminal;
        public ITerminal Terminal {
            get { return _terminal; }
        }
        public TerminalSyntaxNode(ITerminal terminal) {
            _terminal = terminal;
        }

        public override void Start() {
            if (!_terminal.Matches(Engine.CodePoints, Position)) {
                return;
            }
            Position += _terminal.Length;
            Accept();
        }

        public string Name { get { return _terminal.Name; } }
        public bool IsGreedy { get { return false; } }
        public SyntaxNode Create() {
            return this;
        }
    }
}