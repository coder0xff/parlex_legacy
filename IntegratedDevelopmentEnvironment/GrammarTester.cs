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

        public GrammarTester(GrammarEditor grammarEditor) {
            _grammarEditor = grammarEditor;
            InitializeComponent();
            Show(Main.Instance.dockPanel1, DockState.Document);
        }

        class ErrorInfo {
            public readonly int Position;
            public readonly Grammar.ISymbol Symbol;
            public ErrorInfo(Parser.MatchCategory error) {
                Position = error.Position;
                Symbol = error.Symbol;
            }

            public override string ToString() {
                return "Expected " + Symbol + " at " + Position;
            }
        }
        private void Evaluate() {
            bool errors;
            var grammar = _grammarEditor.GetGrammar(out errors);
            var parser = new Parser(grammar);
            var job = parser.Parse(textBoxDocument.Text);
            job.Join();
            if (job.AbstractSyntaxForest.IsEmpty) {
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
            listBoxErrors.Items.Clear();
            listBoxErrors.Items.AddRange(job.PossibleErrors.Select(x => new ErrorInfo(x)).ToArray<Object>());
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
