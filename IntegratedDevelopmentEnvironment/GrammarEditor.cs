using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Windows.Forms;
using Common;
using Parlex;
using WeifenLuo.WinFormsUI.Docking;

namespace IntegratedDevelopmentEnvironment {
    public partial class GrammarEditor : DockContent, IDocumentView {
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

        private void BehaviorsDirtied() {
            SetHasUnsavedChanges();
            _grammar = null;
        }

        private Grammar BehaviorsToGrammar(out bool errors) {
            errors = false;
            Grammar grammar;
            grammar = new Grammar();
            foreach (var node in treeView.Nodes) {
                var tree = GetTagValue<BehaviorTree>((TreeNode)node, "tree");
                var name = GetTagValue<String>((TreeNode)node, "name");
                var greedy = GetTagValue<bool>((TreeNode)node, "greedy");
                Automata.Nfa<Grammar.ISymbol> nfa;
                try {
                    nfa = tree.ToNfa();
                } catch (InvalidBehaviorTreeException) {
                    errors = true;
                    continue;
                }
                var production = new Grammar.Production(name, greedy, nfa);
                grammar.Productions.Add(production);
            }
            grammar.MainProduction = grammar.GetRecognizerByName("SYNTAX");
            return grammar;
        }

        private bool TrySyncBehaviorsToGrammar() {
            bool errors = false;
            if (_grammar == null) {
                _grammar = BehaviorsToGrammar(out errors);
            }
            return !errors;
        }

        public void SaveCopy(string filePathName) {
            if (!TrySyncBehaviorsToGrammar()) {
                if (MessageBox.Show(this, "Errors", "The behavior tree is incomplete. If you save, data will be lost. Save anyway?", MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation, MessageBoxDefaultButton.Button2) != DialogResult.Yes) {
                    return;
                }
            }
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
            var result = new GrammarEditor(grammar) { _formatter = formatter, _filePathName = filePathName, _hasUnsavedChanges = false };
            result.UpdateTitle();
            return result;
        }

        public static TreeNode BuildTreeNode(BehaviorTree.Choice choiceNode) {
            return new TreeNode("Choice", 0, 0, choiceNode.Children.Select(BuildTreeNode).ToArray());
        }

        public static TreeNode BuildTreeNode(BehaviorTree.Optional optionalNode) {
            return new TreeNode("Optional", 0, 0, new[] { BuildTreeNode(optionalNode.Child) });
        }

        public static TreeNode BuildTreeNode(BehaviorTree.Repetition repetitionNode) {
            return new TreeNode("Repetition", 0, 0, new[] { BuildTreeNode(repetitionNode.Child) });
        }

        public static TreeNode BuildTreeNode(BehaviorTree.Sequence sequenceNode) {
            return new TreeNode("Sequence", 0, 0, sequenceNode.Children.Select(BuildTreeNode).ToArray());
        }

        public static TreeNode BuildTreeNode(BehaviorTree.Leaf leafNode) {
            return new TreeNode("Symbol: " + leafNode.Symbol.Name);
        }

        public static void DeleteTreeNodeChild(BehaviorTree.Choice parentNode, BehaviorTree.Node childNode) {
            parentNode.Children.Remove(childNode);
        }

        public static void DeleteTreeNodeChild(BehaviorTree.Optional parentNode, BehaviorTree.Node childNode) {
            Debug.Assert(parentNode.Child == childNode);
            parentNode.Child = null;
        }

        public static void DeleteTreeNodeChild(BehaviorTree.Repetition parentNode, BehaviorTree.Node childNode) {
            Debug.Assert(parentNode.Child == childNode);
            parentNode.Child = null;
        }

        public static void DeleteTreeNodeChild(BehaviorTree.Sequence parentNode, BehaviorTree.Node childNode) {
            parentNode.Children.Remove(childNode);
        }

        private static DynamicDispatcher _deleteTreeNodeChildDynamicDispatcher;
        public static void DeleteTreeNodeChild(BehaviorTree.Node parentNode, BehaviorTree.Node childNode) {
            if (_deleteTreeNodeChildDynamicDispatcher == null) {
                _deleteTreeNodeChildDynamicDispatcher = new DynamicDispatcher();
            }
            _deleteTreeNodeChildDynamicDispatcher.Dispatch<TreeNode>(null, parentNode, childNode);
        }

        public static void AddTreeNodeChild(BehaviorTree.Choice parentNode, BehaviorTree.Node childNode) {
            parentNode.Children.Add(childNode);
        }

        public static void AddTreeNodeChild(BehaviorTree.Optional parentNode, BehaviorTree.Node childNode) {
            Debug.Assert(parentNode.Child == null);
            parentNode.Child = childNode;
        }

        public static void AddTreeNodeChild(BehaviorTree.Repetition parentNode, BehaviorTree.Node childNode) {
            Debug.Assert(parentNode.Child == null);
            parentNode.Child = childNode;
        }

        public static void AddTreeNodeChild(BehaviorTree.Sequence parentNode, BehaviorTree.Node childNode) {
            parentNode.Children.Add(childNode);
        }

        private static DynamicDispatcher _addTreeNodeChildDynamicDispatcher;
        public static void AddTreeNodeChild(BehaviorTree.Node parentNode, BehaviorTree.Node childNode) {
            if (_addTreeNodeChildDynamicDispatcher == null) {
                _addTreeNodeChildDynamicDispatcher = new DynamicDispatcher();
            }
            _addTreeNodeChildDynamicDispatcher.Dispatch<TreeNode>(null, parentNode, childNode);
        }

        static void SetTagValue(TreeNode node, String key, Object value) {
            var table = (Dictionary<String, Object>)node.Tag;
            if (table == null) {
                node.Tag = table = new Dictionary<string, object>();
            }
            table[key] = value;
        }

        static bool TryGetTagValue<T>(TreeNode node, String key, out T value) {
            value = default(T);
            var table = (Dictionary<String, Object>)node.Tag;
            if (table == null) {
                node.Tag = table = new Dictionary<string, object>();
            }
            Object temp;
            if (table.TryGetValue(key, out temp)) {
                if (temp is T) {
                    value = (T)temp;
                    return true;
                }
            }
            return false;
        }

        static T GetTagValue<T>(TreeNode node, String key) {
            T temp;
            if (TryGetTagValue(node, key, out temp)) {
                return temp;
            }
            return default(T);
        }

        private static DynamicDispatcher _buildTreeNodeDynamicDispatcher;
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static TreeNode BuildTreeNode(BehaviorTree.Node behaviorNode) {
            if (_buildTreeNodeDynamicDispatcher == null) {
                _buildTreeNodeDynamicDispatcher = new DynamicDispatcher();
            }
            var result = _buildTreeNodeDynamicDispatcher.Dispatch<TreeNode>(null, behaviorNode);
            SetTagValue(result, "behavior", behaviorNode);
            return result;
        }

        private void Populate_treeView() {
            treeView.Nodes.Clear();
            foreach (Grammar.Production production in _grammar.Productions.OrderBy(x => x.Name)) {
                var behavior = new BehaviorTree(production);
                TreeNode treeNode = BuildTreeNode(behavior.Root);
                treeNode.Text = production.Name + " " + treeNode.Text;
                SetTagValue(treeNode, "name", production.Name);
                SetTagValue(treeNode, "greedy", production.Greedy);
                SetTagValue(treeNode, "tree", behavior);
                treeView.Nodes.Add(treeNode);
            }
        }

        private void AddTreeChild(TreeNode child) {
            var node = GetTagValue<BehaviorTree.Node>(treeView.SelectedNode, "behavior");
            if (node is BehaviorTree.Sequence || node is BehaviorTree.Choice) {
                treeView.SelectedNode.Nodes.Add(child);
            } else if (node is BehaviorTree.Optional || node is BehaviorTree.Repetition) {
                Debug.Assert(treeView.SelectedNode.Nodes.Count == 0);
                treeView.SelectedNode.Nodes.Add(child);
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
                var sequence = new BehaviorTree.Sequence();
                SetTagValue(treeNode, "behavior", sequence);
                treeView.Nodes.Add(treeNode);
                AddTreeNodeChild(GetTagValue<BehaviorTree.Node>(treeView.SelectedNode, "behavior"), sequence);
            } else {
                var treeNode = new TreeNode("Sequence");
                SetTagValue(treeNode, "behavior", new BehaviorTree.Sequence());
                AddTreeChild(treeNode);
            }
            BehaviorsDirtied();
        }

        private void toolStripButtonRepetition_Click(object sender, EventArgs e) {
            if (treeView.SelectedNode == null) {
                String unnamedName = "Unnamed";
                int nameCounter = 2;
                while (_grammar.GetRecognizerByName(unnamedName) != null) {
                    unnamedName = "Unnamed (" + (nameCounter++) + ")";
                }
                var treeNode = new TreeNode(unnamedName + ": Repetition");
                var repetition = new BehaviorTree.Repetition();
                SetTagValue(treeNode, "behavior", repetition);
                treeView.Nodes.Add(treeNode);
                AddTreeNodeChild(GetTagValue<BehaviorTree.Node>(treeView.SelectedNode, "behavior"), repetition);
            } else {
                var treeNode = new TreeNode("Repetition");
                SetTagValue(treeNode, "behavior", new BehaviorTree.Repetition());
                AddTreeChild(treeNode);
            }
            BehaviorsDirtied();
        }

        private void toolStripButtonChoice_Click(object sender, EventArgs e) {
            if (treeView.SelectedNode == null) {
                String unnamedName = "Unnamed";
                int nameCounter = 2;
                while (_grammar.GetRecognizerByName(unnamedName) != null) {
                    unnamedName = "Unnamed (" + (nameCounter++) + ")";
                }
                var treeNode = new TreeNode(unnamedName + ": Choice");
                var choice = new BehaviorTree.Choice();
                SetTagValue(treeNode, "behavior", choice);
                treeView.Nodes.Add(treeNode);
                AddTreeNodeChild(GetTagValue<BehaviorTree.Node>(treeView.SelectedNode, "behavior"), choice);
            } else {
                var treeNode = new TreeNode("Choice");
                SetTagValue(treeNode, "behavior", new BehaviorTree.Choice());
                AddTreeChild(treeNode);
            }
            BehaviorsDirtied();
        }

        private void toolStripButtonOptional_Click(object sender, EventArgs e) {
            if (treeView.SelectedNode == null) {
                String unnamedName = "Unnamed";
                int nameCounter = 2;
                while (_grammar.GetRecognizerByName(unnamedName) != null) {
                    unnamedName = "Unnamed (" + (nameCounter++) + ")";
                }
                var treeNode = new TreeNode(unnamedName + ": Optional");
                var optional = new BehaviorTree.Optional();
                SetTagValue(treeNode, "behavior", optional);
                treeView.Nodes.Add(treeNode);
                AddTreeNodeChild(GetTagValue<BehaviorTree.Node>(treeView.SelectedNode, "behavior"), optional);
            } else {
                var treeNode = new TreeNode("Optional");
                SetTagValue(treeNode, "behavior", new BehaviorTree.Optional());
                AddTreeChild(treeNode);
            }
            BehaviorsDirtied();
        }

        private void treeView_KeyDown(object sender, KeyEventArgs e) {
            if (e.KeyCode == Keys.Delete) {
                var node = treeView.SelectedNode;
                if (node != null) {
                    if (node.Level > 0) {
                        var parent = node.Parent;
                        var parentBehaviorNode = GetTagValue<BehaviorTree.Node>(parent, "behavior");
                        var nodeBehaviorNode = GetTagValue<BehaviorTree.Node>(node, "behavior");
                        if (!(parentBehaviorNode is BehaviorTree.Leaf)) {
                            DeleteTreeNodeChild(parentBehaviorNode, nodeBehaviorNode);
                        }
                    }
                    treeView.SelectedNode.Remove();
                    BehaviorsDirtied();
                }
            }
        }

        private void treeView_AfterSelect(object sender, TreeViewEventArgs e) {
            if (treeView.SelectedNode != null) {
                var tag = GetTagValue<BehaviorTree.Node>(treeView.SelectedNode, "behavior");
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
            var leaf = new BehaviorTree.Leaf(Grammar.CharacterTerminal);
            SetTagValue(treeNode, "behavior", leaf);
            AddTreeChild(treeNode);
            AddTreeNodeChild(GetTagValue<BehaviorTree.Node>(treeView.SelectedNode, "behavior"), leaf);
            BehaviorsDirtied();
        }

        private void treeView_BeforeLabelEdit(object sender, NodeLabelEditEventArgs e) {
            var editText = treeView.SelectedNode.Text;
            var behaviorNode = GetTagValue<BehaviorTree.Node>(treeView.SelectedNode, "behavior");
            var leaf = behaviorNode as BehaviorTree.Leaf;
            if (leaf != null) {
                editText = leaf.Symbol.Name;
                if (leaf.Symbol is Grammar.StringTerminal) {
                    editText = leaf.Symbol.ToString();
                    if (_grammar.GetSymbol(editText) != null) {
                        editText = "\"" + editText + "\"";
                    }
                }
            } else {
                String name;
                if (TryGetTagValue(treeView.SelectedNode, "name", out name)) {
                    editText = name;
                } else {
                    e.CancelEdit = true;
                }
            }
            if (editText != treeView.SelectedNode.Text) {
                e.CancelEdit = true;
                treeView.SelectedNode.Text = editText;
                MethodInvoker m = treeView.SelectedNode.BeginEdit;
                ThreadPool.QueueUserWorkItem(doNotCare => Invoke(m));
            }
        }

        private void treeView_AfterLabelEdit(object sender, NodeLabelEditEventArgs e) {
            Grammar.ISymbol symbol = null;
            var editText = e.Label ?? e.Node.Text;
            String name;
            if (TryGetTagValue(e.Node, "name", out name)) {
                SetTagValue(e.Node, "name", editText);
                BehaviorsDirtied();
                editText = editText + " " + GetTagValue<BehaviorTree.Node>(e.Node, "behavior").GetType().Name;
            } else {
                var asUtf32 = editText.GetUtf32CodePoints();
                String asStringLiteral = Grammar.ProcessStringLiteral(asUtf32, 0, asUtf32.Length);
                if (asStringLiteral == null) {
                    bool doNotCare;
                    var tempGrammar = BehaviorsToGrammar(out doNotCare);
                    symbol = tempGrammar.GetSymbol(editText);
                }
                if (symbol == null) {
                    symbol = new Grammar.StringTerminal(editText);
                }
                editText = "Symbol: " + symbol.Name;
                GetTagValue<BehaviorTree.Leaf>(e.Node, "behavior").Symbol = symbol;
                BehaviorsDirtied();
            }
            e.CancelEdit = true;
            e.Node.Text = editText;
        }

        private void treeView_MouseUp(object sender, MouseEventArgs e) {
            if (treeView.GetNodeAt(e.Location) == null) {
                treeView.SelectedNode = null;
                treeView_AfterSelect(null, null);
            }
        }
    }
}