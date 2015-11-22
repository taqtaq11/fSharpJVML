using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

using fSharpJVML;

namespace test_fSharpJVML_ConsoleApp
{
    class Program
    {
        const int EXAMPLE_NUM = 5;

        static void Main(string[] args)
        {            
            fsCompiler compiler = new fsCompiler(ExamplesLoader.LoadExample(EXAMPLE_NUM));
            compiler.CreateTree();
            var tree = compiler.SourceTree;
            AstNodePrinter.Print(tree);
        }
    }
}
