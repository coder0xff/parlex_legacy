using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace parlex
{
    class Document
    {
        public class ExemplarSource
        {
            public String Text;
            public class ProductDeclaration
            {
                public String Name;
                public int StartPosition;
                public int Length;

                public ProductDeclaration(String name, int startPosition, int length)
                {
                    Name = name;
                    StartPosition = startPosition;
                    Length = length;
                }
            }

            public List<ProductDeclaration> ProductDeclarations = new List<ProductDeclaration>();
        }

        public List<ExemplarSource> ExemplarSources = new List<ExemplarSource>();

        public static Document FromText(String source)
        {
            var result = new Document();
            var lines = Regex.Split(source, "\r\n|\r|\n");
            ExemplarSource currentExemplarSource = null;
            foreach (var line in lines)
            {
                if (line.Trim().Length == 0)
                {
                    currentExemplarSource = null;
                }
                else
                {
                    if (currentExemplarSource == null)
                    {
                        currentExemplarSource = new ExemplarSource { Text = line };
                        result.ExemplarSources.Add(currentExemplarSource);
                    }
                    else
                    {
                        var productDeclarationParts = line.Split(':');
                        int startPosition = productDeclarationParts[0].IndexOf('|');
                        int length = productDeclarationParts[0].LastIndexOf('|') - startPosition + 1;
                        currentExemplarSource.ProductDeclarations.Add(
                            new ExemplarSource.ProductDeclaration(
                                productDeclarationParts[1], startPosition, length
                            )
                         );
                    }
                }
            }
            return result;
        }

        public IEnumerable<Exemplar> GetExemplars()
        {
            var products = new Dictionary<string, Product>();
            var results = new List<Exemplar>();
            foreach (ExemplarSource exemplarSource in ExemplarSources)
            {
                var result = new Exemplar(exemplarSource.Text);
                results.Add(result);
                foreach (ExemplarSource.ProductDeclaration productDeclaration in exemplarSource.ProductDeclarations)
                {
                    if (!products.ContainsKey(productDeclaration.Name))
                    {
                        products.Add(productDeclaration.Name, new Product(productDeclaration.Name));
                    }
                    result.ProductSpans.Add(new ProductSpan(
                        products[productDeclaration.Name],
                        productDeclaration.StartPosition,
                        productDeclaration.Length)
                    );
                }
            }
            return results;
        }
    }
}
