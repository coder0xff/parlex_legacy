using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Parlex {
    interface IParserGenerator {
        void Generate(String destinationDirectory, Grammar grammar);
    }
}
