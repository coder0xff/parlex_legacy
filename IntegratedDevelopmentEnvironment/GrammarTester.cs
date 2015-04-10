using System;
using System.Text;
using System.Windows.Forms;
using Parlex;
using WeifenLuo.WinFormsUI.Docking;

namespace IntegratedDevelopmentEnvironment {
    public partial class GrammarTester : DockContent {
        private GrammarEditor _grammarEditor;
        private String _overridenMainName = null;
        private bool _grammarChangedInBackground;

        public GrammarTester(GrammarEditor grammarEditor, String overridenMainName = null) {
            _grammarEditor = grammarEditor;
            _overridenMainName = overridenMainName;
            _grammarEditor.GrammarChanged += _grammarEditor_GrammarChanged;
            InitializeComponent();
            Show(Main.Instance.dockPanel1, DockState.Document);
        }

        void _grammarEditor_GrammarChanged(Object sender, GrammarChangedEventArgs args) {
            if (Main.Instance.ActiveMdiChild != this) {
                _grammarChangedInBackground = true;
            } else {
                ScheduleParse();
            }
        }

        class ErrorInfo {
            public readonly int Position;
            public readonly Recognizer Symbol;
            public ErrorInfo(MatchCategory error) {
                Position = error.Position;
                //recognizerDefinition = error.recognizerDefinition;
            }

            public override string ToString() {
                return "Expected " + Symbol + " at " + Position;
            }
        }

        private void Evaluate() {
            toolStripStatusLabel1.Text = "Analyzing grammar";
            Application.DoEvents();
            var nfaGrammar = _grammarEditor.NfaGrammar;
            var parser = new Parser(nfaGrammar);
            NfaProduction selectedMainProduction = nfaGrammar.Main;
            if (_overridenMainName != null) {
                var temp = nfaGrammar.GetProduction(_overridenMainName);
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
            PopulateStructureTreeView(job.AbstractSyntaxGraph);
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

        private void GrammarTester_Activated(object sender, EventArgs e) {
            if (_grammarChangedInBackground) {
                _grammarChangedInBackground = false;
                ScheduleParse();
            }
        }

        private TreeNode PopulateStructureTreeView(AbstractSyntaxGraph asg, Match match) {
            var result = new TreeNode("Match");
            foreach (var matchClass in match.Children) {
                result.Nodes.Add(PopulateStructureTreeView(asg, matchClass));
            }
            return result;
        }

        private TreeNode PopulateStructureTreeView(AbstractSyntaxGraph asg, MatchClass matchClass) {
            var builder = new StringBuilder();
            builder.Append(matchClass.Recognizer.Name);
            builder.Append(" ");
            builder.Append(matchClass.Position);
            builder.Append(" ");
            builder.Append(matchClass.Length.ToString());
            builder.Append(" : ");
            var text = matchClass.Engine.Document.Utf32Substring(matchClass.Position, matchClass.Length);
            builder.AppendLine(Utilities.QuoteStringLiteral(text));
            var result = new TreeNode(builder.ToString());
            foreach (var match in asg.NodeTable[matchClass]) {
                result.Nodes.Add(PopulateStructureTreeView(asg, match));
            }
            return result;
        }

        public void PopulateStructureTreeView(AbstractSyntaxGraph asg) {
            treeViewStructure.Nodes.Clear();
            if (!asg.IsEmpty) {
                treeViewStructure.Nodes.Add(PopulateStructureTreeView(asg, asg.Root));
            }
        }

    }
}
