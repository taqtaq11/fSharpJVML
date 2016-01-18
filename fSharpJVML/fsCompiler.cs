using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Antlr.Runtime;
using Antlr.Runtime.Tree;
using fsharp_ss;
using System.Diagnostics;
using System.IO;
using System.Threading;

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


        public void Compile(string outputFilesPath, string outputFileName)
        {
            CreateTree();
            InferTypes();
            GenerateCode(outputFilesPath, outputFileName);
            CreateJar(outputFilesPath, outputFileName);
        }

        private void CreateTree()
        {
            fsharp_ssLexer lexer = new fsharp_ssLexer(new ANTLRStringStream(source));
            var tokens = new CommonTokenStream(lexer);
            fsharp_ssParser parser = new fsharp_ssParser(tokens);
            sourceTree = (ITree)parser.execute().Tree;
        }

        private void InferTypes()
        {
            fsTypeInferer ti = new fsTypeInferer(SourceTree);
            sourceTree = ti.Infer();
        }

        private void GenerateCode(string outputFilesPath, string outputFileName)
        {
            fsCodeGenerator cg = new fsCodeGenerator(outputFilesPath, outputFileName);
            cg.GenerateClassFiles(SourceTree);
        }

        private void CreateJar(string outputFilesPath, string outputFileName)
        {
            string[] jFiles = Directory.GetFiles(outputFilesPath, "*.j")
                                        .Select(file => file.Split('\\').Last())
                                        .ToArray();
            Process process = new Process();

            process.StartInfo.FileName = "java";
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.WorkingDirectory = outputFilesPath;

            process.StartInfo.Arguments = $"jasmin.Main -d {outputFilesPath}";
            for (int i = 0; i < jFiles.Length; i++)
            {
                process.StartInfo.Arguments += $" {jFiles[i]}";
            }
            process.Start();

            Thread.Sleep(500);

            process.StartInfo.FileName = "jar";
            process.StartInfo.Arguments = $"-cvmf manifest.txt {outputFileName}.jar";
            for (int i = 0; i < jFiles.Length; i++)
            {
                process.StartInfo.Arguments += $" {jFiles[i].Split('.')[0]}.class";
            }
            process.Start();
        }
    }
}
