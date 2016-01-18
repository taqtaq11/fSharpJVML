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
        const int EXAMPLE_NUM = 2;

        static void Main(string[] args)
        {            
            fsCompiler compiler = new fsCompiler(ExamplesLoader.LoadExample(EXAMPLE_NUM));
            compiler.Compile(@"D:\fsharp_compiler\output\classes", "test_1");
            var tree = compiler.SourceTree;
            AstNodePrinter.Print(tree);
        }
    }
}
