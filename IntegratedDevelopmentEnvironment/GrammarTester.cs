using System;
using System.Windows.Forms;
using Parlex;
using WeifenLuo.WinFormsUI.Docking;

namespace IntegratedDevelopmentEnvironment {
    public partial class GrammarTester : DockContent {
        private GrammarEditor _grammarEditor;
        private NfaGrammar _cachedGrammar;
        private String _overridenMainName = null;
        public GrammarTester(GrammarEditor grammarEditor, String overridenMainName = null) {
            _grammarEditor = grammarEditor;
            _overridenMainName = overridenMainName;
            _grammarEditor.GrammarChanged += _grammarEditor_GrammarChanged;
            InitializeComponent();
            Show(Main.Instance.dockPanel1, DockState.Document);
        }

        void _grammarEditor_GrammarChanged(Grammar obj) {
            _cachedGrammar = null;
            ScheduleParse();
        }

        class ErrorInfo {
            public readonly int Position;
            public readonly ISymbol Symbol;
            public ErrorInfo(MatchCategory error) {
                Position = error.Position;
                //Symbol = error.Symbol;
            }

            public override string ToString() {
                return "Expected " + Symbol + " at " + Position;
            }
        }

        private void Evaluate() {
            if (_cachedGrammar == null) {
                toolStripStatusLabel1.Text = "Analyzing grammar";
                Application.DoEvents();
                _cachedGrammar = _grammarEditor.Grammar.ToNfaGrammar();
            }
            var parser = new Parser(_cachedGrammar);
            NfaProduction selectedMainProduction = _cachedGrammar.Main;
            if (_overridenMainName != null) {
                var temp = _cachedGrammar.GetProduction(_overridenMainName);
                if (temp == null) {
                    toolStripStatusLabel1.Text = "The production that this tester was made for (" + _overridenMainName + ") no longer exists.";
                    return;
                }
                selectedMainProduction = temp;
            }
            if (selectedMainProduction == null) {
                toolStripStatusLabel1.Text = "The grammar does not have a main production";
                return;
            }
            toolStripStatusLabel1.Text = "Parsing...";
            Application.DoEvents();
            DateTime start = DateTime.Now;
            var job = parser.Parse(textBoxDocument.Text, 0, -1, selectedMainProduction);
            job.Join();
            var seconds = (DateTime.Now - start).TotalSeconds;
            var errors = false; // todo: Parser generates error info
            if (job.AbstractSyntaxGraph.IsEmpty) {
                if (errors) {
                    toolStripStatusLabel1.Text = "The grammar contains errors, and this text could not be parsed.";
                } else {
                    toolStripStatusLabel1.Text = "The grammar does not recognize this text.";
                }
            } else {
                if (errors) {
                    toolStripStatusLabel1.Text = "The grammar contains errors, but still parsed this text.";
                } else {
                    toolStripStatusLabel1.Text = "The grammar parses this text.";
                }
            }
            toolStripStatusLabel1.Text += " (" + seconds + "s)";
            listBoxErrors.Items.Clear();
            //listBoxErrors.Items.AddRange(job.PossibleErrors.Select(x => new ErrorInfo(x)).ToArray<Object>());
        }

        private void timer1_Tick(object sender, EventArgs e) {
            timer1.Stop();
            Evaluate();
        }

        private void ScheduleParse() {
            timer1.Stop();
            timer1.Start();
            toolStripStatusLabel1.Text = "Parse pending...";
        }

        private void textBoxDocument_TextChanged(object sender, EventArgs e) {
            ScheduleParse();
        }

        private void listBoxErrors_SelectedIndexChanged(object sender, EventArgs e) {
            if (listBoxErrors.SelectedIndex != -1) {
                var error = (ErrorInfo)listBoxErrors.SelectedItem;
                textBoxDocument.SelectionStart = error.Position;
                textBoxDocument.SelectionLength = 1;
                textBoxDocument.ScrollToCaret();
            }
        }
    }
}
