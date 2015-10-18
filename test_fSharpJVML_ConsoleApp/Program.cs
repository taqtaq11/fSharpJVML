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
        static void Main(string[] args)
        {
            
            fsCompiler compiler = new fsCompiler(ExamplesLoader.LoadExample(1));
            compiler.Compile();
            var tree = compiler.SourceTree;
        }
    }
}
