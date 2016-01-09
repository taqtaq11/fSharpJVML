using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Antlr.Runtime;
using Antlr.Runtime.Tree;
using fsharp_ss;

namespace fSharpJVML
{
    public class fsCompiler
    {
        private string source;
        private ITree sourceTree;


        public ITree SourceTree
        {
            get
            {
                return sourceTree;
            }
        }


        public fsCompiler() { }

        public fsCompiler(string source)
        {
            this.source = source;
        }


        public void CreateTree()
        {
            fsharp_ssLexer lexer = new fsharp_ssLexer(new ANTLRStringStream(source));
            var tokens = new CommonTokenStream(lexer);
            fsharp_ssParser parser = new fsharp_ssParser(tokens);
            sourceTree = (ITree)parser.execute().Tree;
        }

        public void InferTypes()
        {
            fsTypeInferer ti = new fsTypeInferer(SourceTree);
            sourceTree = ti.Infer();
        }

        public void GenerateCode(string outputFilesPath, string outputFileName)
        {
            fsCodeGenerator cg = new fsCodeGenerator(outputFilesPath, outputFileName);
            cg.GenerateClassFiles(SourceTree);
        }
    }
}
