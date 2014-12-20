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

namespace IntegratedDevelopmentEnvironment {
    public partial class GrammarTester : Form {
        private GrammarEditor _grammarEditor;
        private Grammar _grammar;

        public GrammarTester(GrammarEditor grammarEditor) {
            _grammarEditor = grammarEditor;
            InitializeComponent();
        }

        private void Evaluate() {
            
        }
    }
}
