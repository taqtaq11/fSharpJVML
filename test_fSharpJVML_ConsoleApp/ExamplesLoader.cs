using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace test_fSharpJVML_ConsoleApp
{
    class ExamplesLoader
    {
        public static string LoadExample(byte num)
        {
            string src = File.ReadAllText($@"../../../examples\{num}.fs");
            return src;
        }
    }
}
