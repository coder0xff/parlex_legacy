using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;
using Parlex;
using WeifenLuo.WinFormsUI.Docking;

namespace IntegratedDevelopmentEnvironment {
    public partial class Main : Form {
        public static Main Instance { get; private set; }
        public Main() {
            InitializeComponent();
            Instance = this;
            GrammarEditor grammarEditor = GrammarEditor.ForFile("C:\\Users\\coder_000\\Dropbox\\Plange\\small.wsn",
                new WirthSyntaxNotation.Formatter());
            bool dontCare;
            var grammar = grammarEditor.GetGrammar(out dontCare);
            var formatter = new CSharpFormatter();
            var f = new FileStream("C:\\Users\\coder_000\\Desktop\\small.cs", FileMode.Create);
            formatter.Serialize(f, grammar);
            grammarEditor.Show(dockPanel1, DockState.Document);
        }

        private void grammarToolStripMenuItem_Click(object sender, EventArgs e) {
            var grammarEditor = new GrammarEditor(new Grammar());
            grammarEditor.Show(dockPanel1, DockState.Document);
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e) {
            if (openFileDialog.ShowDialog(this) == DialogResult.OK) {
                if (openFileDialog.FilterIndex == 1 || openFileDialog.FilterIndex == 2) {
                    GrammarEditor grammarEditor = GrammarEditor.ForFile(openFileDialog.FileName, new WirthSyntaxNotation.Formatter());
                    grammarEditor.Show(dockPanel1, DockState.Document);
                }
            }
        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e) {
            var editor = dockPanel1.ActiveDocument as IDocumentView;
            Debug.Assert(editor != null, "editor != null");
            if (String.IsNullOrEmpty(editor.FilePathName)) {
                saveAsToolStripMenuItem_Click(sender, e);
            } else {
                editor.SaveToDisk();
            }
        }

        private void saveAsToolStripMenuItem_Click(object sender, EventArgs e) {
            var editor = dockPanel1.ActiveDocument as IDocumentView;
            Debug.Assert(editor != null, "editor != null");
            saveFileDialog.FileName = editor.FilePathName;
            if (saveFileDialog.ShowDialog(this) == DialogResult.OK) {
                editor.FilePathName = saveFileDialog.FileName;
                editor.SaveToDisk();
            }
        }
    }
}