﻿using System;
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
            string toParseFile = File.ReadAllText(@"C:\Users\Brent\Dropbox\parlex\parse_test.txt");
            var parseResult = Parser.Parse(toParseFile, analyzer.BuiltInCharacterProducts.Values, analyzer.Products);
            foreach (var productMatchResult in parseResult) {
                System.Diagnostics.Debug.WriteLine(productMatchResult.Product.Title);
            }
            System.Diagnostics.Debug.WriteLine("All matched products printed");
            //             Application.EnableVisualStyles();
            //             Application.SetCompatibleTextRenderingDefault(false);
            //             Application.Run(new Form1());
        }
    }
}
