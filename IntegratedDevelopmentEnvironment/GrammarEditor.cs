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
            set { _formatter = value; }
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
            var grammar = new Grammar();
            foreach (var node in treeView.Nodes) {
                var tree = GetTagValue<BehaviorTree>((TreeNode)node, "tree");
                var name = GetTagValue<String>((TreeNode)node, "name");
                var greedy = GetTagValue<bool>((TreeNode)node, "greedy");
                Automata.Nfa<ISymbol> nfa;
                try {
                    nfa = tree.ToNfa();
                } catch (InvalidBehaviorTreeException) {
                    errors = true;
                    continue;
                }
                var production = new NfaProduction(name, greedy, nfa);
                grammar.Productions.Add(production);
            }
            grammar.MainProduction = grammar.GetRecognizerByName("SYNTAX");
            return grammar;
        }

        private bool TrySyncBehaviorsToGrammar() {
            bool errors = false;
            if (_grammar == null) {
                _grammar = BehaviorsToGrammar(out errors);
                if (errors) _grammar = null;
            }
            return !errors;
        }

        public Grammar GetGrammar(out bool errors) {
            errors = false;
            if (_grammar == null) {
                errors = TrySyncBehaviorsToGrammar();
            }
            return _grammar;
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

        public static void AddBehaviorTreeNodeChild(BehaviorTree.Choice parentNode, BehaviorTree.Node childNode) {
            parentNode.Children.Add(childNode);
        }

        public static void AddBehaviorTreeNodeChild(BehaviorTree.Optional parentNode, BehaviorTree.Node childNode) {
            Debug.Assert(parentNode.Child == null);
            parentNode.Child = childNode;
        }

        public static void AddBehaviorTreeNodeChild(BehaviorTree.Repetition parentNode, BehaviorTree.Node childNode) {
            Debug.Assert(parentNode.Child == null);
            parentNode.Child = childNode;
        }

        public static void AddBehaviorTreeNodeChild(BehaviorTree.Sequence parentNode, BehaviorTree.Node childNode) {
            parentNode.Children.Add(childNode);
        }

        private static DynamicDispatcher _addTreeNodeChildDynamicDispatcher;
        public static void AddBehaviorTreeNodeChild(BehaviorTree.Node parentNode, BehaviorTree.Node childNode) {
            if (_addTreeNodeChildDynamicDispatcher == null) {
                _addTreeNodeChildDynamicDispatcher = new DynamicDispatcher();
            }
            _addTreeNodeChildDynamicDispatcher.Dispatch<TreeNode>(null, parentNode, childNode);
        }

        static void SetTagValue(TreeNode node, String key, Object value) {
            var table = node.Tag as Dictionary<String, Object>;
            if (table == null) {
                node.Tag = table = new Dictionary<string, object>();
            }
            table[key] = value;
        }

        static bool TryGetTagValue<T>(TreeNode node, String key, out T value) {
            value = default(T);
            var table = node.Tag as Dictionary<String, Object>;
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
            foreach (NfaProduction production in _grammar.Productions.OrderBy(x => x.Name)) {
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
                var currentNames = treeView.Nodes.Cast<TreeNode>().Select(rootNode => GetTagValue<String>(rootNode, "name")).ToArray();
                while (currentNames.Contains(unnamedName)) {
                    unnamedName = "Unnamed (" + (nameCounter++) + ")";
                }
                var treeNode = new TreeNode(unnamedName + ": Sequence");
                var sequence = new BehaviorTree.Sequence();
                var behaviorTree = new BehaviorTree { Root = sequence };
                SetTagValue(treeNode, "tree", behaviorTree);
                SetTagValue(treeNode, "behavior", sequence);
                SetTagValue(treeNode, "name", "Unnamed");
                SetTagValue(treeNode, "greedy", false);
                treeView.Nodes.Add(treeNode);
                treeView.SelectedNode = treeNode;
            } else {
                var treeNode = new TreeNode("Sequence");
                var sequence = new BehaviorTree.Sequence();
                SetTagValue(treeNode, "behavior", sequence);
                AddTreeChild(treeNode);
                AddBehaviorTreeNodeChild(GetTagValue<BehaviorTree.Node>(treeView.SelectedNode, "behavior"), sequence);
                treeView.SelectedNode = treeNode;
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
                var behaviorTree = new BehaviorTree { Root = repetition };
                SetTagValue(treeNode, "tree", behaviorTree);
                SetTagValue(treeNode, "behavior", repetition);
                SetTagValue(treeNode, "name", "Unnamed");
                SetTagValue(treeNode, "greedy", false);
                treeView.Nodes.Add(treeNode);
                treeView.SelectedNode = treeNode;
            } else {
                var treeNode = new TreeNode("Repetition");
                var repetition = new BehaviorTree.Repetition();
                SetTagValue(treeNode, "behavior", repetition);
                AddTreeChild(treeNode);
                AddBehaviorTreeNodeChild(GetTagValue<BehaviorTree.Node>(treeView.SelectedNode, "behavior"), repetition);
                treeView.SelectedNode = treeNode;
            }
            BehaviorsDirtied();
        }

        private void toolStripButtonChoice_Click(object sender, EventArgs e) {
            if (treeView.SelectedNode == null) {
                String unnamedName = "Unnamed";
                int nameCounter = 2;
                TrySyncBehaviorsToGrammar();
                while (_grammar.GetRecognizerByName(unnamedName) != null) {
                    unnamedName = "Unnamed (" + (nameCounter++) + ")";
                }
                var treeNode = new TreeNode(unnamedName + ": Choice");
                var choice = new BehaviorTree.Choice();
                var behaviorTree = new BehaviorTree { Root = choice };
                SetTagValue(treeNode, "tree", behaviorTree);
                SetTagValue(treeNode, "behavior", choice);
                SetTagValue(treeNode, "name", "Unnamed");
                SetTagValue(treeNode, "greedy", false);
                treeView.Nodes.Add(treeNode);
                treeView.SelectedNode = treeNode;
            } else {
                var treeNode = new TreeNode("Choice");
                var choice = new BehaviorTree.Choice();
                SetTagValue(treeNode, "behavior", choice);
                AddTreeChild(treeNode);
                AddBehaviorTreeNodeChild(GetTagValue<BehaviorTree.Node>(treeView.SelectedNode, "behavior"), choice);
                treeView.SelectedNode = treeNode;
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
                var behaviorTree = new BehaviorTree { Root = optional };
                SetTagValue(treeNode, "tree", behaviorTree);
                SetTagValue(treeNode, "behavior", optional);
                SetTagValue(treeNode, "name", "Unnamed");
                SetTagValue(treeNode, "greedy", false);
                treeView.Nodes.Add(treeNode);
                treeView.SelectedNode = treeNode;
            } else {
                var treeNode = new TreeNode("Optional");
                var optional = new BehaviorTree.Optional();
                SetTagValue(treeNode, "behavior", optional);
                AddTreeChild(treeNode);
                AddBehaviorTreeNodeChild(GetTagValue<BehaviorTree.Node>(treeView.SelectedNode, "behavior"), optional);
                treeView.SelectedNode = treeNode;
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
            var treeNode = new TreeNode(StandardSymbols.CharacterTerminal.Name);
            var leaf = new BehaviorTree.Leaf(StandardSymbols.CharacterTerminal);
            SetTagValue(treeNode, "behavior", leaf);
            AddTreeChild(treeNode);
            AddBehaviorTreeNodeChild(GetTagValue<BehaviorTree.Node>(treeView.SelectedNode, "behavior"), leaf);
            treeView.SelectedNode = treeNode;
            BehaviorsDirtied();
        }

        private void treeView_BeforeLabelEdit(object sender, NodeLabelEditEventArgs e) {
            var editText = treeView.SelectedNode.Text;
            var behaviorNode = GetTagValue<BehaviorTree.Node>(treeView.SelectedNode, "behavior");
            var leaf = behaviorNode as BehaviorTree.Leaf;
            if (leaf != null) {
                editText = leaf.Symbol.Name;
                if (leaf.Symbol is StringTerminal) {
                    editText = leaf.Symbol.ToString();
                    TrySyncBehaviorsToGrammar();
                    if (_grammar != null && _grammar.GetSymbol(editText) != null) {
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
            ISymbol symbol = null;
            var editText = e.Label ?? e.Node.Text;
            String name;
            if (TryGetTagValue(e.Node, "name", out name)) {
                SetTagValue(e.Node, "name", editText);
                BehaviorsDirtied();
                editText = editText + " " + GetTagValue<BehaviorTree.Node>(e.Node, "behavior").GetType().Name;
            } else {
                var asUtf32 = editText.GetUtf32CodePoints();
                String asStringLiteral = Util.ProcessStringLiteral(asUtf32, 0, asUtf32.Length);
                if (asStringLiteral == null) {
                    bool doNotCare;
                    var tempGrammar = BehaviorsToGrammar(out doNotCare);
                    symbol = tempGrammar.GetSymbol(editText);
                }
                if (symbol == null) {
                    symbol = new StringTerminal(editText);
                }
                editText = "Symbol: " + symbol.Name;
                GetTagValue<BehaviorTree.Leaf>(e.Node, "behavior").Symbol = symbol;
                BehaviorsDirtied();
            }
            e.CancelEdit = true;
            e.Node.Text = editText;
        }

        private TreeNode _contextMenuNode;
        private void treeView_MouseUp(object sender, MouseEventArgs e) {
            var node = treeView.GetNodeAt(e.Location);
            if (e.Button == MouseButtons.Right) {
                String name;
                if (TryGetTagValue(node, "name", out name)) {
                    _contextMenuNode = node;
                    productionContextMenu.Show(treeView, e.Location);
                }
            }
            if (node == null) {
                treeView.SelectedNode = null;
                treeView_AfterSelect(null, null);
            }
        }

        private void toolStripButtonTester_Click(object sender, EventArgs e) {
            var tester = new GrammarTester(this);
            tester.Show();
        }

        public static GrammarEditor ForGrammar(Grammar grammar, WirthSyntaxNotation.Formatter formatter) {
            var result = new GrammarEditor(grammar) { _formatter = formatter, _filePathName = null, _hasUnsavedChanges = true };
            result.UpdateTitle();
            return result;
        }

        private void exportCSharpToolStripMenuItem_Click(object sender, EventArgs e) {
            if (exportCSharpSaveFileDialog.ShowDialog(Main.Instance) == DialogResult.OK) {
                var formatter = new CSharpFormatter();
                Grammar g;
                bool errors;
                g = GetGrammar(out errors);
                if (g == null) {
                    MessageBox.Show(Main.Instance, "Grammar error", "The grammar contains errors. Exporting could not be completed.");
                    return;
                }
                if (errors) {
                    if (MessageBox.Show(Main.Instance, "Grammar error", "The grammar appears to contain errors but was partially constructed. Export anyway?", MessageBoxButtons.YesNo) == DialogResult.No) {
                        return;
                    }
                }
                formatter.Serialize(File.Open(exportCSharpSaveFileDialog.FileName, FileMode.Create, FileAccess.Write), g);
            }
        }

        private void testToolStripMenuItem_Click(object sender, EventArgs e) {
            var tester = new GrammarTester(this, GetTagValue<String>(_contextMenuNode, "name"));
            tester.Show();
        }

        private void generateCParserToolStripMenuItem_Click(object sender, EventArgs e) {
            if (folderBrowserDialog1.ShowDialog(this) == DialogResult.OK) {
                Grammar g;
                bool errors;
                g = GetGrammar(out errors);
                if (g == null) {
                    MessageBox.Show(Main.Instance, "Grammar error", "The grammar contains errors. Exporting could not be completed.");
                    return;
                }
                if (errors) {
                    if (MessageBox.Show(Main.Instance, "Grammar error", "The grammar appears to contain errors but was partially constructed. Export anyway?", MessageBoxButtons.YesNo) == DialogResult.No) {
                        return;
                    }
                }
                var generator = new Parlex.CSharpParserGenerator("Generated");
                generator.Generate(folderBrowserDialog1.SelectedPath, g, "Parser");
            }
        }
    }
}