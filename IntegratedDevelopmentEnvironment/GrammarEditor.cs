using System;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Forms;
using Common;
using Parlex;
using WeifenLuo.WinFormsUI.Docking;

namespace IntegratedDevelopmentEnvironment {
    public partial class GrammarEditor : DockContent, IDocumentView {
        private static DynamicDispatcher _buildTreeNodeDynamicDispatcher;
        private string _filePathName;
        private IGrammarFormatter _formatter;
        private Grammar _grammar;
        private bool _hasUnsavedChanges;

        public GrammarEditor() {
            InitializeComponent();
            _grammar = new Grammar();
            FilePathName = null;
            treeView_AfterSelect(null, null);
        }

        public GrammarEditor(Grammar grammar)
            : this() {
            _grammar = grammar;
            Populate_treeView();
            UpdateTitle();
        }

        public IGrammarFormatter Formatter {
            get { return _formatter; }
        }

        public string FilePathName {
            get { return _filePathName; }
            set {
                _filePathName = value;
                _hasUnsavedChanges = true;
            }
        }

        public void LoadFromDisk() {
            FileStream s = File.Open(_filePathName, FileMode.OpenOrCreate);
            _grammar = _formatter.Deserialize(s);
            s.Close();
            Populate_treeView();
            UpdateTitle();
        }

        public void SaveToDisk() {
            SaveCopy(_filePathName);
            _hasUnsavedChanges = false;
            UpdateTitle();
        }

        public void SaveCopy(string filePathName) {
            FileStream s = File.Open(filePathName, FileMode.Create);
            _formatter.Serialize(s, _grammar);
            s.Close();
        }

        public bool HasUnsavedChanges {
            get { return _hasUnsavedChanges; }
        }

        private void UpdateTitle() {
            if (String.IsNullOrEmpty(_filePathName)) {
                Text = "*Unnamed";
            } else {
                Text = Path.GetFileName(_filePathName);
                if (_hasUnsavedChanges) {
                    Text = "*" + Text;
                }
            }
        }

        public void SetHasUnsavedChanges() {
            _hasUnsavedChanges = true;
            UpdateTitle();
        }

        public static GrammarEditor ForFile(String filePathName, IGrammarFormatter formatter) {
            FileStream s = File.Open(filePathName, FileMode.OpenOrCreate);
            Grammar grammar = formatter.Deserialize(s);
            s.Close();
            var result = new GrammarEditor(grammar) {_formatter = formatter, _filePathName = filePathName, _hasUnsavedChanges = false};
            result.UpdateTitle();
            return result;
        }

        public static TreeNode BuildTreeNode(BehaviorTree.Choice choiceNode) {
            return new TreeNode("Choice", 0, 0, choiceNode.Children.Select(BuildTreeNode).ToArray());
        }

        public static TreeNode BuildTreeNode(BehaviorTree.Optional optionalNode) {
            return new TreeNode("Optional", 0, 0, new[] {BuildTreeNode(optionalNode.Child)});
        }

        public static TreeNode BuildTreeNode(BehaviorTree.Repetition repetitionNode) {
            return new TreeNode("Repetition", 0, 0, new[] {BuildTreeNode(repetitionNode.Child)});
        }

        public static TreeNode BuildTreeNode(BehaviorTree.Sequence sequenceNode) {
            return new TreeNode("Sequence", 0, 0, sequenceNode.Children.Select(BuildTreeNode).ToArray());
        }

        public static TreeNode BuildTreeNode(BehaviorTree.Leaf leafNode) {
            return new TreeNode("Symbol: " + leafNode.Symbol.Name);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static TreeNode BuildTreeNode(BehaviorTree.Node behaviorNode) {
            if (_buildTreeNodeDynamicDispatcher == null) {
                _buildTreeNodeDynamicDispatcher = new DynamicDispatcher();
            }
            var result = _buildTreeNodeDynamicDispatcher.Dispatch<TreeNode>(null, behaviorNode);
            result.Tag = behaviorNode;
            return result;
        }


        private void Populate_treeView() {
            treeView.Nodes.Clear();
            foreach (Grammar.Recognizer production in _grammar.Productions.OrderBy(x => x.Name)) {
                var behavior = new BehaviorTree(production);
                TreeNode treeNode = BuildTreeNode(behavior.Root);
                treeNode.Text = production.Name + " " + treeNode.Text;
                treeView.Nodes.Add(treeNode);
            }
        }

        private void AddTreeChild(TreeNode child) {
            var selectedTag = treeView.SelectedNode.Tag as BehaviorTree.Node;
            if (selectedTag is BehaviorTree.Sequence || selectedTag is BehaviorTree.Choice) {
                treeView.SelectedNode.Nodes.Add(child);
            } else if (selectedTag is BehaviorTree.Optional || selectedTag is BehaviorTree.Repetition) {
                if (treeView.SelectedNode.Nodes.Count == 0) {
                    treeView.SelectedNode.Nodes.Add(child);
                }
            }
            treeView.SelectedNode.Expand();
            treeView_AfterSelect(null, null);
        }

        private void toolStripButtonSequence_Click(object sender, EventArgs e) {
            if (treeView.SelectedNode == null) {
                String unnamedName = "Unnamed";
                int nameCounter = 2;
                while (_grammar.GetRecognizerByName(unnamedName) != null) {
                    unnamedName = "Unnamed (" + (nameCounter++) + ")";
                }
                var treeNode = new TreeNode(unnamedName + ": Sequence");
                treeNode.Tag = new BehaviorTree.Sequence();
                treeView.Nodes.Add(treeNode);
            } else {
                var treeNode = new TreeNode("Sequence");
                treeNode.Tag = new BehaviorTree.Sequence();
                AddTreeChild(treeNode);
            }
        }

        private void toolStripButtonRepetition_Click(object sender, EventArgs e) {
            if (treeView.SelectedNode == null) {
                String unnamedName = "Unnamed";
                int nameCounter = 2;
                while (_grammar.GetRecognizerByName(unnamedName) != null) {
                    unnamedName = "Unnamed (" + (nameCounter++) + ")";
                }
                var treeNode = new TreeNode(unnamedName + ": Repetition");
                treeNode.Tag = new BehaviorTree.Repetition();
                treeView.Nodes.Add(treeNode);
            } else {
                var treeNode = new TreeNode("Repetition");
                treeNode.Tag = new BehaviorTree.Repetition();
                AddTreeChild(treeNode);
            }
        }

        private void toolStripButtonChoice_Click(object sender, EventArgs e) {
            if (treeView.SelectedNode == null) {
                String unnamedName = "Unnamed";
                int nameCounter = 2;
                while (_grammar.GetRecognizerByName(unnamedName) != null) {
                    unnamedName = "Unnamed (" + (nameCounter++) + ")";
                }
                var treeNode = new TreeNode(unnamedName + ": Choice");
                treeNode.Tag = new BehaviorTree.Choice();
                treeView.Nodes.Add(treeNode);
            } else {
                var treeNode = new TreeNode("Choice");
                treeNode.Tag = new BehaviorTree.Choice();
                AddTreeChild(treeNode);
            }
        }

        private void toolStripButtonOptional_Click(object sender, EventArgs e) {
            if (treeView.SelectedNode == null) {
                String unnamedName = "Unnamed";
                int nameCounter = 2;
                while (_grammar.GetRecognizerByName(unnamedName) != null) {
                    unnamedName = "Unnamed (" + (nameCounter++) + ")";
                }
                var treeNode = new TreeNode(unnamedName + ": Optional");
                treeNode.Tag = new BehaviorTree.Optional();
                treeView.Nodes.Add(treeNode);
            } else {
                var treeNode = new TreeNode("Optional");
                treeNode.Tag = new BehaviorTree.Optional();
                AddTreeChild(treeNode);
            }
        }

        private void treeView_KeyDown(object sender, KeyEventArgs e) {
            if (e.KeyCode == Keys.Delete) {
                if (treeView.SelectedNode != null) {
                    treeView.SelectedNode.Remove();
                }
            }
        }

        private void treeView_AfterSelect(object sender, TreeViewEventArgs e) {
            if (treeView.SelectedNode != null) {
                object tag = treeView.SelectedNode.Tag;
                if (tag is BehaviorTree.Sequence || tag is BehaviorTree.Choice) {
                    toolStripButtonSequence.Enabled = true;
                    toolStripButtonChoice.Enabled = true;
                    toolStripButtonRepetition.Enabled = true;
                    toolStripButtonOptional.Enabled = true;
                    toolStripButtonLeaf.Enabled = true;
                } else if (tag is BehaviorTree.Optional || tag is BehaviorTree.Repetition) {
                    bool canAdd = treeView.SelectedNode.Nodes.Count == 0;
                    toolStripButtonSequence.Enabled = canAdd;
                    toolStripButtonChoice.Enabled = canAdd;
                    toolStripButtonRepetition.Enabled = false;
                    toolStripButtonOptional.Enabled = false;
                    toolStripButtonLeaf.Enabled = canAdd;
                } else {
                    toolStripButtonSequence.Enabled = false;
                    toolStripButtonChoice.Enabled = false;
                    toolStripButtonRepetition.Enabled = false;
                    toolStripButtonOptional.Enabled = false;
                    toolStripButtonLeaf.Enabled = false;
                }
            } else {
                toolStripButtonSequence.Enabled = true;
                toolStripButtonChoice.Enabled = true;
                toolStripButtonRepetition.Enabled = true;
                toolStripButtonOptional.Enabled = true;
                toolStripButtonLeaf.Enabled = false;
            }
        }

        private void toolStripButtonLeaf_Click(object sender, EventArgs e) {
            var treeNode = new TreeNode(Grammar.CharacterTerminal.Name);
            treeNode.Tag = new BehaviorTree.Leaf(Grammar.CharacterTerminal);
            AddTreeChild(treeNode);
        }

        private void treeView_BeforeLabelEdit(object sender, NodeLabelEditEventArgs e) {
            if (treeView.SelectedNode.Tag is BehaviorTree.Leaf) {
                treeView.SelectedNode.Text = (treeView.SelectedNode.Tag as BehaviorTree.Leaf).Symbol.Name;
            } else {
                e.CancelEdit = true;
            }
        }

        private void treeView_AfterLabelEdit(object sender, NodeLabelEditEventArgs e) {
            Grammar.ISymbol symbol;
            if (Grammar.TryGetBuiltinISymbolByName(e.Label, out symbol)) {
                treeView.SelectedNode.Text = symbol.Name;
                (treeView.SelectedNode.Tag as BehaviorTree.Leaf).Symbol = symbol;
            } else {
                symbol = _grammar.GetRecognizerByName(e.Label);
                if (symbol == null) {
                    symbol = new Grammar.StringTerminal(e.Label);
                }
                treeView.SelectedNode.Text = symbol.Name;
                (treeView.SelectedNode.Tag as BehaviorTree.Leaf).Symbol = symbol;
            }
        }
    }
}