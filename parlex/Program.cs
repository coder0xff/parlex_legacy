using System;
using System.IO;

namespace parlex {
    static class Program {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main() {
            string testFile = File.ReadAllText(@"C:\Users\Brent\Dropbox\parlex\test.ple");
            var testDocument = Document.FromText(testFile);
            var analyzer = new Analyzer(testDocument);
            var parser = new Parser();
            string toParseFile = File.ReadAllText(@"C:\Users\Brent\Dropbox\parlex\parse_test.txt");
            parser.Parse(toParseFile, analyzer.CodePointProducts, analyzer.AllProducts.Values);
            //             Application.EnableVisualStyles();
            //             Application.SetCompatibleTextRenderingDefault(false);
            //             Application.Run(new Form1());
        }
    }
}
