using System.Linq;
using parlex;

namespace IDE {
    public static class CompiledGrammarExtensions {
        public static GrammarDocument ToGrammarDocument(this CompiledGrammar compiledGrammar) {
            var result = new GrammarDocument();
            var allProducts = compiledGrammar.GetAllProducts();
            foreach (var product in allProducts) {
                if (CompiledGrammar.IsBuiltInProductName(product.Key)) {
                    //don't do built in products
                } else {
                    var characterProduct = product.Value as CharacterClassCharacterProduct;
                    if (characterProduct != null) {
                        result.CharacterSetSources.Add(characterProduct.Source);
                    } else {
                        var subGrammarDocument = product.Value.ToGrammarDocument(allProducts);
                        result.ExemplarSources.AddRange(subGrammarDocument.ExemplarSources);
                        foreach (var isASource in subGrammarDocument.IsASources) {
                            result.IsASources.Add(isASource);
                        }
                    }
                }
            }
            foreach (var result1 in compiledGrammar.Precedences.Select(t => new GrammarDocument.Precedes(t.Item1.Title, t.Item2.Title))) {
                result.PrecedesSources.Add(result1);
            }
            return result;
        }
    }
}
