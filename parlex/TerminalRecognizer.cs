namespace Parlex {
    public class TerminalRecognizer : Recognizer {
        private readonly TerminalDefinition _terminalDefinition;
        public TerminalDefinition TerminalDefinition {
            get { return _terminalDefinition; }
        }
        public TerminalRecognizer(TerminalDefinition terminalDefinition) {
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
        public Recognizer Create() {
            return this;
        }
    }
}