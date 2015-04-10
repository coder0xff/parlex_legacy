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
        private IMetaSyntax _formatter;
        private Grammar _grammar;
        private NfaGrammar _cachedNfaGrammar;
        private bool _hasUnsavedChanges;

        public event Action<Object, GrammarChangedEventArgs> GrammarChanged = delegate { };

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

        public IMetaSyntax Formatter {
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
            _grammar = _formatter.Parse(s);
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
            var s = File.Open(filePathName, FileMode.Create);
            _formatter.Generate(s, _grammar);
            s.Close();
        }

        public bool HasUnsavedChanges {
            get { return _hasUnsavedChanges; }
        }

        public Grammar Grammar {
            get { return _grammar; }
            set {
                _grammar = value;
                UpdateTitle();
                Populate_treeView();
                OnGrammarChanged();
            }
        }

        public NfaGrammar NfaGrammar {
            get {
                if (_cachedNfaGrammar == null) {
                    _cachedNfaGrammar = Grammar.ToNfaGrammar();
                }
                return _cachedNfaGrammar;
            }
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

        protected void OnGrammarChanged() {
            _cachedNfaGrammar = null;
            _hasUnsavedChanges = true;
            UpdateTitle();
            GrammarChanged(this, new GrammarChangedEventArgs());
        }

        public static GrammarEditor ForFile(String filePathName, IMetaSyntax formatter) {
            var s = File.Open(filePathName, FileMode.OpenOrCreate);
            Grammar grammar;
            try {
                grammar = formatter.Parse(s);
            } catch (UndefinedProductionException exc) {
                MessageBox.Show(Application.OpenForms[0], "The grammar contains an error. The production \"" + exc.ProductionName + "\" was referenced, but it is not defined.", "Unable to load grammar");
                return null;
            }
            s.Close();
            var result = new GrammarEditor(grammar) { _formatter = formatter, _filePathName = filePathName, _hasUnsavedChanges = false };
            result.UpdateTitle();
            return result;
        }

        public static TreeNode BuildTreeNode(ChoiceBehavior choiceBehaviorNode) {
            return new TreeNode("ChoiceBehavior", 0, 0, choiceBehaviorNode.Children.Select(BuildTreeNode).ToArray());
        }

        public static TreeNode BuildTreeNode(Optional optionalNode) {
            return new TreeNode("Optional", 0, 0, new[] { BuildTreeNode(optionalNode.Child) });
        }

        public static TreeNode BuildTreeNode(RepetitionBehavior repetitionBehaviorNode) {
            return new TreeNode("RepetitionBehavior", 0, 0, new[] { BuildTreeNode(repetitionBehaviorNode.Child) });
        }

        public static TreeNode BuildTreeNode(SequenceBehavior sequenceBehaviorNode) {
            return new TreeNode("SequenceBehavior", 0, 0, sequenceBehaviorNode.Children.Select(BuildTreeNode).ToArray());
        }

        public static TreeNode BuildTreeNode(BehaviorLeaf behaviorLeafNode) {
            return new TreeNode("recognizerDefinition: " + behaviorLeafNode.Recognizer.Name);
        }

        public static void DeleteBehaviorNodeChild(ChoiceBehavior parentNode, BehaviorNode childBehaviorNode) {
            parentNode.Children.Remove(childBehaviorNode);
        }

        public static void DeleteBehaviorNodeChild(Optional parentNode, BehaviorNode childBehaviorNode) {
            Debug.Assert(parentNode.Child == childBehaviorNode);
            parentNode.Child = null;
        }

        public static void DeleteBehaviorNodeChild(RepetitionBehavior parentNode, BehaviorNode childBehaviorNode) {
            Debug.Assert(parentNode.Child == childBehaviorNode);
            parentNode.Child = null;
        }

        public static void DeleteBehaviorNodeChild(SequenceBehavior parentNode, BehaviorNode childBehaviorNode) {
            parentNode.Children.Remove(childBehaviorNode);
        }

        private static DynamicDispatcher _deleteTreeNodeChildDynamicDispatcher;
        public static void DeleteBehaviorNodeChild(BehaviorNode parentBehaviorNode, BehaviorNode childBehaviorNode) {
            if (_deleteTreeNodeChildDynamicDispatcher == null) {
                _deleteTreeNodeChildDynamicDispatcher = new DynamicDispatcher();
            }
            _deleteTreeNodeChildDynamicDispatcher.Dispatch<Object>(null, parentBehaviorNode, childBehaviorNode);
        }

        public static void AddBehaviorNodeChild(ChoiceBehavior parentNode, BehaviorNode childBehaviorNode) {
            parentNode.Children.Add(childBehaviorNode);
        }

        public static void AddBehaviorNodeChild(Optional parentNode, BehaviorNode childBehaviorNode) {
            Debug.Assert(parentNode.Child == null);
            parentNode.Child = childBehaviorNode;
        }

        public static void AddBehaviorNodeChild(RepetitionBehavior parentNode, BehaviorNode childBehaviorNode) {
            Debug.Assert(parentNode.Child == null);
            parentNode.Child = childBehaviorNode;
        }

        public static void AddBehaviorNodeChild(SequenceBehavior parentNode, BehaviorNode childBehaviorNode) {
            parentNode.Children.Add(childBehaviorNode);
        }

        private static DynamicDispatcher _addTreeNodeChildDynamicDispatcher;
        public static void AddBehaviorNodeChild(BehaviorNode parentBehaviorNode, BehaviorNode childBehaviorNode) {
            if (_addTreeNodeChildDynamicDispatcher == null) {
                _addTreeNodeChildDynamicDispatcher = new DynamicDispatcher();
            }
            _addTreeNodeChildDynamicDispatcher.Dispatch<TreeNode>(null, parentBehaviorNode, childBehaviorNode);
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
        private static TreeNode BuildTreeNode(BehaviorNode behaviorBehaviorNode) {
            if (_buildTreeNodeDynamicDispatcher == null) {
                _buildTreeNodeDynamicDispatcher = new DynamicDispatcher();
            }
            var result = _buildTreeNodeDynamicDispatcher.Dispatch<TreeNode>(null, behaviorBehaviorNode);
            SetTagValue(result, "behavior", behaviorBehaviorNode);
            return result;
        }

        private void Populate_treeView() {
            treeView.Nodes.Clear();
            foreach (Production production in _grammar.Productions.OrderBy(x => x.Name)) {
                TreeNode treeNode = BuildTreeNode(production.Behavior.Root);
                treeNode.Text = production.Name + " " + treeNode.Text;
                SetTagValue(treeNode, "production", production);
                SetTagValue(treeNode, "greedy", production.IsGreedy);
                SetTagValue(treeNode, "tree", production.Behavior);
                treeView.Nodes.Add(treeNode);
            }
        }

        private void AddViewNodeChild(TreeNode parent, TreeNode child) {
            var node = GetTagValue<BehaviorNode>(treeView.SelectedNode, "behavior");
            if (node is SequenceBehavior || node is ChoiceBehavior) {
                parent.Nodes.Add(child);
            } else if (node is Optional || node is RepetitionBehavior) {
                Debug.Assert(treeView.SelectedNode.Nodes.Count == 0);
                parent.Nodes.Add(child);
            }
            parent.Expand();
            treeView_AfterSelect(null, null);
        }

        private String GetUnnamedName() {
            String unnamedName = "Unnamed";
            int nameCounter = 2;
            var currentNames = _grammar.Productions.Select(production => production.Name).ToArray();
            while (currentNames.Contains(unnamedName)) {
                unnamedName = "Unnamed (" + (nameCounter++) + ")";
            }
            return unnamedName;
        }

        private void CreateProduction<T>() where T : BehaviorNode, new() {
            var unnamedName = GetUnnamedName();
            var viewNode = new TreeNode(unnamedName + ": " + typeof(T).Name);
            var behaviorNode = new T();
            var production = new Production(unnamedName) { Behavior = new BehaviorTree { Root = behaviorNode }};
            SetTagValue(viewNode, "production", production);
            SetTagValue(viewNode, "behavior", behaviorNode);
            SetTagValue(viewNode, "greedy", false);
            treeView.Nodes.Add(viewNode);
            treeView.SelectedNode = viewNode;
            _grammar.Productions.Add(production);
            OnGrammarChanged();
        }

        private void CreateChild<T>() where T : BehaviorNode, new() {
            var parentViewNode = treeView.SelectedNode;
            var parentBehaviorNode = GetTagValue<BehaviorNode>(parentViewNode, "behavior");
            var viewNode = new TreeNode(typeof(T).Name);
            var behaviorNode = new T();
            SetTagValue(viewNode, "behavior", behaviorNode);
            AddViewNodeChild(parentViewNode, viewNode);
            AddBehaviorNodeChild(parentBehaviorNode, behaviorNode);
            treeView.SelectedNode = viewNode;
            OnGrammarChanged();
        }

        private void Create<T>() where T : BehaviorNode, new() {
            if (treeView.SelectedNode == null) {
                CreateProduction<T>();
            } else {
                CreateChild<T>();
            }
        }

        private void toolStripButtonSequence_Click(object sender, EventArgs e) {
            Create<SequenceBehavior>();
        }

        private void toolStripButtonRepetition_Click(object sender, EventArgs e) {
            Create<RepetitionBehavior>();
        }

        private void toolStripButtonChoice_Click(object sender, EventArgs e) {
            Create<ChoiceBehavior>();
        }

        private void toolStripButtonOptional_Click(object sender, EventArgs e) {
            Create<Optional>();
        }

        //must be private because it does not call OnGrammarChanged
        void RemoveDependenciesOnProduction(Production production, TreeNode viewNode) {
            var behaviorNode = GetTagValue<BehaviorNode>(viewNode, "BehaviorNode");
            var asLeaf = behaviorNode as BehaviorLeaf;
            if (asLeaf != null && asLeaf.Recognizer == production) {
                asLeaf.Recognizer = new StringTerminal(production.Name);
                viewNode.Text = "recognizer: " + asLeaf.Recognizer.Name;
            }
            foreach (var _node in viewNode.Nodes) {
                var node = _node as TreeNode;
                RemoveDependenciesOnProduction(production, node);
            }
        }

        //must be private because it does not all OnGrammarChanged
        void RemoveProduction(TreeNode viewNode, Production production) {
            _grammar.Productions.Remove(production);
            RemoveDependenciesOnProduction(production, viewNode);
            viewNode.Remove();
        }

        private void treeView_KeyDown(object sender, KeyEventArgs e) {
            if (e.KeyCode == Keys.Delete) {
                var viewNode = treeView.SelectedNode;
                if (viewNode != null) {
                    Production production;
                    if (TryGetTagValue(viewNode, "production", out production)) {
                        RemoveProduction(viewNode, production);
                    } else {
                        var parent = viewNode.Parent;
                        var parentBehaviorNode = GetTagValue<BehaviorNode>(parent, "behavior");
                        var nodeBehaviorNode = GetTagValue<BehaviorNode>(viewNode, "behavior");
                        DeleteBehaviorNodeChild(parentBehaviorNode, nodeBehaviorNode);
                        viewNode.Remove();
                    }
                    OnGrammarChanged();
                }
            }
        }

        private void treeView_AfterSelect(object sender, TreeViewEventArgs e) {
            if (treeView.SelectedNode != null) {
                var tag = GetTagValue<BehaviorNode>(treeView.SelectedNode, "behavior");
                if (tag is SequenceBehavior || tag is ChoiceBehavior) {
                    toolStripButtonSequence.Enabled = true;
                    toolStripButtonChoice.Enabled = true;
                    toolStripButtonRepetition.Enabled = true;
                    toolStripButtonOptional.Enabled = true;
                    toolStripButtonLeaf.Enabled = true;
                } else if (tag is Optional || tag is RepetitionBehavior) {
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
            var parentViewNode = treeView.SelectedNode;
            var parentBehaviorNode = GetTagValue<BehaviorNode>(parentViewNode, "behavior");
            var viewNode = new TreeNode(StandardSymbols.Any.Name);
            var behaviorNode = new BehaviorLeaf(StandardSymbols.Any);
            SetTagValue(viewNode, "behavior", behaviorNode);
            AddViewNodeChild(parentViewNode, viewNode);
            AddBehaviorNodeChild(parentBehaviorNode, behaviorNode);
            treeView.SelectedNode = viewNode;
            OnGrammarChanged();
        }

        private void treeView_BeforeLabelEdit(object sender, NodeLabelEditEventArgs e) {
            var editText = treeView.SelectedNode.Text;
            var behaviorNode = GetTagValue<BehaviorNode>(treeView.SelectedNode, "behavior");
            var leaf = behaviorNode as BehaviorLeaf;
            if (leaf != null) {
                editText = leaf.Recognizer.Name;
                if (leaf.Recognizer is StringTerminal) {
                    editText = leaf.Recognizer.ToString();
                    Recognizer recognizer;
                    if (StandardSymbols.TryGetBuiltInISymbolByName(editText, out recognizer) || _grammar.GetProduction(editText) != null) {
                        editText = "\"" + editText + "\"";
                    }
                }
            } else {
                Production production;
                if (TryGetTagValue(treeView.SelectedNode, "production", out production)) {
                    editText = production.Name;
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
            var unchanged = e.Label == e.Node.Text || e.Label == null;
            Recognizer recognizer = null;
            var editText = e.Label ?? e.Node.Text;
            var behaviorNode = GetTagValue<BehaviorNode>(e.Node, "behavior");
            Production production;
            var dirtied = false;
            if (TryGetTagValue(e.Node, "production", out production)) {                
                if (StandardSymbols.TryGetBuiltInISymbolByName(editText, out recognizer) || !unchanged && _grammar.GetProduction(editText) != null) {
                    MessageBox.Show(this, "A built in recognizerDefinition or a production in this grammar already uses this name.");
                    e.CancelEdit = true;
                    e.Node.BeginEdit();
                    return;
                }
                if (production.Name != editText) {
                    production.SetName(editText);
                    dirtied = true;
                }
                if ((editText.ToLower() == "main" || editText.ToLower() == "syntax")) {
                    if (_grammar.Main != production) {
                        _grammar.Main = production;
                        dirtied = true;
                    }
                } else if (_grammar.Main == production) {
                    _grammar.Main = null;
                    dirtied = true;
                }
                editText = editText + ": " + behaviorNode.GetType().Name;
            } else if (behaviorNode is BehaviorLeaf) {
                var leaf = behaviorNode as BehaviorLeaf;
                var asUtf32 = editText.GetUtf32CodePoints();
                var asStringLiteral = Utilities.ProcessStringLiteral(asUtf32, 0, asUtf32.Length);
                if (asStringLiteral == null) {
                    if (!StandardSymbols.TryGetBuiltInISymbolByName(editText, out recognizer)) {
                        recognizer = _grammar.GetProduction(editText);
                    }
                }
                if (recognizer == null) {
                    recognizer = new StringTerminal(editText);
                }
                editText = "recognizer: " + recognizer.Name;
                leaf.Recognizer = recognizer;
                dirtied = true;
            }
            e.CancelEdit = true;
            e.Node.Text = editText;
            if (dirtied) {
                OnGrammarChanged();
            }
        }

        private TreeNode _contextMenuNode;
        private void treeView_MouseUp(object sender, MouseEventArgs e) {
            var node = treeView.GetNodeAt(e.Location);
            if (e.Button == MouseButtons.Right) {
                Production production;
                if (TryGetTagValue(node, "production", out production)) {
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
                formatter.Generate(File.Open(exportCSharpSaveFileDialog.FileName, FileMode.Create, FileAccess.Write), _grammar);
            }
        }

        private void testToolStripMenuItem_Click(object sender, EventArgs e) {
            Production production;
            TryGetTagValue(_contextMenuNode, "production", out production);
            Debug.Assert(production != null);
            var tester = new GrammarTester(this, production.Name);
            tester.Show();
        }

        private void generateCParserToolStripMenuItem_Click(object sender, EventArgs e) {
            if (folderBrowserDialog1.ShowDialog(this) == DialogResult.OK) {
                bool errors;
                var generator = new CSharpParserGenerator("Generated");
                generator.Generate(folderBrowserDialog1.SelectedPath, NfaGrammar, "Parser");
            }
        }
    }

    public class GrammarChangedEventArgs {}
}