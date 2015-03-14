using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Parlex;
using WeifenLuo.WinFormsUI.Docking;

namespace IntegratedDevelopmentEnvironment {
    public partial class GrammarTester : DockContent {
        private GrammarEditor _grammarEditor;
        private Grammar _grammar;
        private String _main;

        public GrammarTester(GrammarEditor grammarEditor, String main = null) {
            _grammarEditor = grammarEditor;
            _main = main;
            InitializeComponent();
            Show(Main.Instance.dockPanel1, DockState.Document);
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
            bool errors;
            var grammar = _grammarEditor.GetGrammar(out errors);
            var parser = new Parser(grammar);
            NfaProduction m = grammar.MainProduction;
            if (_main != null) {
                m = grammar.GetRecognizerByName(_main);
            }
            DateTime start = DateTime.Now;
            var job = parser.Parse(textBoxDocument.Text, 0, -1, m);
            job.Join();
            var seconds = (DateTime.Now - start).TotalSeconds;
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

        private void textBoxDocument_TextChanged(object sender, EventArgs e) {
            timer1.Stop();
            timer1.Start();
            toolStripStatusLabel1.Text = "Parse pending...";
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
