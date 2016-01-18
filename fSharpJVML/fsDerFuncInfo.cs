using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace fSharpJVML
{
    class fsDerFuncInfo
    {
        public string Name { get; set; }
        public List<string> ArgsNames { get; set; }
        public int BeforePassedArgsNum { get; set; }
        public int AfterPassedArgsNum { get; set; }

        public fsDerFuncInfo(string name, List<string> argsNames, int beforePassedArgsNum, int afterPassedArgsNum)
        {
            Name = name;
            ArgsNames = argsNames;
            BeforePassedArgsNum = beforePassedArgsNum;
            AfterPassedArgsNum = afterPassedArgsNum;
        }
    }
}
