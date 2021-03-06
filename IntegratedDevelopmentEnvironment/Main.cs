﻿using System;
using System.Diagnostics;
using System.Windows.Forms;
using Parlex;
using WeifenLuo.WinFormsUI.Docking;

namespace IntegratedDevelopmentEnvironment {
    public partial class Main : Form {
        public static Main Instance { get; private set; }
        public Main() {
            InitializeComponent();
            Instance = this;
            var grammarEditor = GrammarEditor.ForGrammar(WirthSyntaxNotationGrammar.NfaGrammar.ToGrammar(), new WirthSyntaxNotation.Formatter());
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
                    if (grammarEditor != null) {
                        grammarEditor.Show(dockPanel1, DockState.Document);
                    }
                }
            }
        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e) {
            var editor = dockPanel1.ActiveDocument as GrammarEditor;
            Debug.Assert(editor != null, "editor != null");
            if (String.IsNullOrEmpty(editor.FilePathName)) {
                saveAsToolStripMenuItem_Click(sender, e);
            } else {
                editor.SaveToDisk();
            }
        }

        private IMetaSyntax getGrammarFormatterForTypeIndex(int index) {
            switch (index) {
                case 1:
                    return new WirthSyntaxNotation.Formatter();
                default:
                    return new WirthSyntaxNotation.Formatter();
            }
        }

        private void saveAsToolStripMenuItem_Click(object sender, EventArgs e) {
            var editor = dockPanel1.ActiveDocument as GrammarEditor;

            Debug.Assert(editor != null, "editor != null");
            saveFileDialog.FileName = editor.FilePathName;
            if (saveFileDialog.ShowDialog(this) == DialogResult.OK) {
                editor.Formatter = getGrammarFormatterForTypeIndex(saveFileDialog.FilterIndex);
                editor.FilePathName = saveFileDialog.FileName;
                editor.SaveToDisk();
            }
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e) {
            Close();
        }
    }
}