namespace Parlex {
    public class TerminalParseNode : ParseNode {
        private readonly TerminalDefinition _terminalDefinition;
        public TerminalDefinition TerminalDefinition {
            get { return _terminalDefinition; }
        }
        public TerminalParseNode(TerminalDefinition terminalDefinition) {
            _terminalDefinition = terminalDefinition;
        }

        public override void Start() {
            if (!_terminalDefinition.Matches(Context.Value.Engine.CodePoints, Position)) {
                return;
            }
            Position += _terminalDefinition.Length;
            Accept();
        }

        public string Name { get { return _terminalDefinition.Name; } }
        public bool IsGreedy { get { return false; } }
        public ParseNode Create() {
            return this;
        }
    }
}