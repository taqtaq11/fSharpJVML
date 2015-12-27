using Antlr.Runtime.Tree;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using fsharp_ss;

namespace fSharpJVML
{
    delegate void CodeGenDelegate(ITree node, List<string> outputFile);

    class fsCodeGenerator
    {
        private string outputFilesPath;
        private string outputFileName;
        private Dictionary<int, CodeGenDelegate> generationFunctions;
        private int localVarsCounter = 0;
        private int maxStackCounter = 0;

        public fsCodeGenerator(string outputFilesPath, string outputFileName)
        {
            this.outputFilesPath = outputFilesPath;
            this.outputFileName = outputFileName;

            generationFunctions = new Dictionary<int, CodeGenDelegate>();
            generationFunctions.Add(fsharp_ssParser.PROGRAM, GenerateProgram);
            generationFunctions.Add(fsharp_ssParser.PLUS, GeneratePlus);
            generationFunctions.Add(fsharp_ssParser.MINUS, GenerateMinus);
            generationFunctions.Add(fsharp_ssParser.MULT, GenerateMult);
            generationFunctions.Add(fsharp_ssParser.DIV, GenerateDivide);
            //inferenceFunctions.Add(fsharp_ssParser.EQ, InferEqNeqOperType);
            //inferenceFunctions.Add(fsharp_ssParser.NEQ, InferEqNeqOperType);
            //inferenceFunctions.Add(fsharp_ssParser.GT, InferCompareOperType);
            //inferenceFunctions.Add(fsharp_ssParser.GE, InferCompareOperType);
            //inferenceFunctions.Add(fsharp_ssParser.LT, InferCompareOperType);
            //inferenceFunctions.Add(fsharp_ssParser.LE, InferCompareOperType);
            //inferenceFunctions.Add(fsharp_ssParser.ID, InferIDType);
            generationFunctions.Add(fsharp_ssParser.INT, GenerateInt);
            generationFunctions.Add(fsharp_ssParser.DOUBLE, GenerateDouble);
            //inferenceFunctions.Add(fsharp_ssParser.CHAR, InferCharType);
            //inferenceFunctions.Add(fsharp_ssParser.TRUE, InferBoolType);
            //inferenceFunctions.Add(fsharp_ssParser.FALSE, InferBoolType);
            //inferenceFunctions.Add(fsharp_ssParser.STRING, InferStringType);
            generationFunctions.Add(fsharp_ssParser.BODY, GenerateBody);
            //inferenceFunctions.Add(fsharp_ssParser.IF, InferIfClauseType);
            generationFunctions.Add(fsharp_ssParser.FUNCTION_DEFN, GenerateFunctionDefn);
            //inferenceFunctions.Add(fsharp_ssParser.FUNCTION_CALL, InferFuncCallType);
            //inferenceFunctions.Add(fsharp_ssParser.VALUE_DEFN, InferValueDefnType);
        }

        public void GenerateClassFiles(ITree sourceAST)
        {
            List<string> mainFile = new List<string>();
            Generate(sourceAST, mainFile);

            //File.Create($"{outputFilesPath}/{outputFileName}.j");
            StreamWriter file = new StreamWriter($"{outputFilesPath}/{outputFileName}.j");
            foreach (var line in mainFile)
            {
                file.WriteLine(line);
            }
            file.Close();
        }

        private void Generate(ITree node, List<string> outputFile)
        {
            generationFunctions[node.Type](node, outputFile);
        }

        private void GenerateProgram(ITree node, List<string> outputFile)
        {
            GenerateFileHeader(outputFile, outputFileName);
            for (int i = 0; i < node.ChildCount; i++)
            {
                if (node.GetChild(i).Text != "TYPE")
                {
                    Generate(node.GetChild(i), outputFile);
                }               
            }
        }

        private void GeneratePlus(ITree node, List<string> outputFile)
        {
            Generate(node.GetChild(0), outputFile);
            Generate(node.GetChild(1), outputFile);

            switch (node.GetChild(0).Type)
            {
                case fsharp_ssParser.INT:
                    outputFile.Add("iadd");
                    break;
                case fsharp_ssParser.DOUBLE:
                    outputFile.Add("dadd");
                    break;
                case fsharp_ssParser.STRING:
                    outputFile.Add($"astore {localVarsCounter++}");
                    outputFile.Add($"astore {localVarsCounter++}");
                    outputFile.Add("new java/lang/StringBuilder");
                    outputFile.Add("dup");
                    outputFile.Add("invokespecial java/lang/StringBuilder/<init>()V");
                    outputFile.Add($"aload {localVarsCounter--}");
                    outputFile.Add("invokevirtual java/lang/StringBuilder/append(Ljava/lang/String;)Ljava/lang/StringBuilder");
                    outputFile.Add($"aload {localVarsCounter--}");
                    outputFile.Add("invokevirtual java/lang/StringBuilder/append(Ljava/lang/String;)Ljava/lang/StringBuilder");
                    break;
                case fsharp_ssParser.CHAR:
                    break;
                default:
                    break;
            }
        }

        private void GenerateMinus(ITree node, List<string> outputFile)
        {
            Generate(node.GetChild(0), outputFile);
            Generate(node.GetChild(1), outputFile);

            switch (node.GetChild(0).Type)
            {
                case fsharp_ssParser.INT:
                    outputFile.Add("isub");
                    break;
                case fsharp_ssParser.DOUBLE:
                    outputFile.Add("dsub");
                    break;
                default:
                    break;
            }
        }

        private void GenerateMult(ITree node, List<string> outputFile)
        {
            Generate(node.GetChild(0), outputFile);
            Generate(node.GetChild(1), outputFile);

            switch (node.GetChild(0).Type)
            {
                case fsharp_ssParser.INT:
                    outputFile.Add("imul");
                    break;
                case fsharp_ssParser.DOUBLE:
                    outputFile.Add("dmul");
                    break;
                default:
                    break;
            }
        }

        private void GenerateDivide(ITree node, List<string> outputFile)
        {
            Generate(node.GetChild(0), outputFile);
            Generate(node.GetChild(1), outputFile);

            switch (node.GetChild(0).Type)
            {
                case fsharp_ssParser.INT:
                    outputFile.Add("idiv");
                    break;
                case fsharp_ssParser.DOUBLE:
                    outputFile.Add("ddiv");
                    break;
                default:
                    break;
            }
        }

        private void GenerateInt(ITree node, List<string> outputFile)
        {
            outputFile.Add($"ldc {node.Text}");
        }

        private void GenerateDouble(ITree node, List<string> outputFile)
        {
            outputFile.Add($"ldc {node.Text}");
        }

        private void GenerateBody(ITree node, List<string> outputFile)
        {
            for (int i = 0; i < node.GetChild(0).ChildCount; i++)
            {
                Generate(node.GetChild(0).GetChild(i), outputFile);
            }
        }

        private void GenerateFunctionDefn(ITree node, List<string> outputFile)
        {
            if (GetChildByType(node, fsharp_ssParser.NAME).GetChild(0).Text == "main")
            {
                outputFile.Add(".method public static main([Ljava/lang/String;)V");
                Generate(GetChildByType(node, fsharp_ssParser.BODY), outputFile);
                outputFile.Add("return");
                outputFile.Add(".end method");
            }
        }

        private void GenerateFileHeader(List<string> outputFile, string fileName)
        {
            outputFile.Add($".source {fileName}.j");
            outputFile.Add($".class {fileName}");
            outputFile.Add($".super java/lang/Object");
        }

        private ITree GetChildByType(ITree parent, int type)
        {
            for (int i = 0; i < parent.ChildCount; i++)
            {
                if (parent.GetChild(i).Type == type)
                {
                    return parent.GetChild(i);
                }
            }

            return null;
        }
    }
}
