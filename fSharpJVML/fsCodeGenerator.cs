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
        private const int VAR_INFO = 65;

        private string outputFilesPath;
        private string outputFileName;
        private Dictionary<int, CodeGenDelegate> generationFunctions;
        private int localVarsCounter = 0;
        private int maxStackCounter = 0;
        private int stackCounter = 0;
        private List<string> enteredFunctionsNames;
        private Stack<int> currentArgsPosition;

        public fsCodeGenerator(string outputFilesPath, string outputFileName)
        {
            this.outputFilesPath = outputFilesPath;
            this.outputFileName = outputFileName;
            this.enteredFunctionsNames = new List<string>();
            currentArgsPosition = new Stack<int>();

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
            generationFunctions.Add(fsharp_ssParser.ID, GenerateID);
            generationFunctions.Add(fsharp_ssParser.INT, GenerateInt);
            generationFunctions.Add(fsharp_ssParser.DOUBLE, GenerateDouble);
            //inferenceFunctions.Add(fsharp_ssParser.CHAR, InferCharType);
            //inferenceFunctions.Add(fsharp_ssParser.TRUE, InferBoolType);
            //inferenceFunctions.Add(fsharp_ssParser.FALSE, InferBoolType);
            //inferenceFunctions.Add(fsharp_ssParser.STRING, InferStringType);
            generationFunctions.Add(fsharp_ssParser.BODY, GenerateBody);
            generationFunctions.Add(fsharp_ssParser.IF, GenerateIfClause);
            generationFunctions.Add(fsharp_ssParser.FUNCTION_DEFN, GenerateFunctionDefn);
            generationFunctions.Add(fsharp_ssParser.FUNCTION_CALL, GenerateFuncCall);
            generationFunctions.Add(fsharp_ssParser.VALUE_DEFN, GenerateValueDefn);
        }        

        public void GenerateClassFiles(ITree sourceAST)
        {
            List<string> mainFile = new List<string>();
            Generate(sourceAST, mainFile);
            SaveToFile(mainFile, outputFileName);
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

            IfsType operandsType = (GetChildByType(node.GetChild(0), fsharp_ssParser.TYPE) as fsTreeNode).NodeType;
            switch (operandsType.Name)
            {
                case "int":
                    outputFile.Add("iadd");
                    RecalculateStack(false);
                    break;
                case "double":
                    outputFile.Add("dadd");
                    RecalculateStack(false);
                    break;
                case "string":
                    outputFile.Add($"astore {localVarsCounter++}");
                    RecalculateStack(false);
                    outputFile.Add($"astore {localVarsCounter++}");
                    RecalculateStack(false);
                    outputFile.Add("new java/lang/StringBuilder");
                    RecalculateStack(true);
                    outputFile.Add("dup");
                    RecalculateStack(true);
                    outputFile.Add("invokespecial java/lang/StringBuilder/<init>()V");
                    RecalculateStack(false);
                    outputFile.Add($"aload {localVarsCounter--}");
                    RecalculateStack(true);
                    outputFile.Add("invokevirtual java/lang/StringBuilder/append(Ljava/lang/String;)Ljava/lang/StringBuilder");
                    RecalculateStack(false);
                    outputFile.Add($"aload {localVarsCounter--}");
                    RecalculateStack(true);
                    outputFile.Add("invokevirtual java/lang/StringBuilder/append(Ljava/lang/String;)Ljava/lang/StringBuilder");
                    RecalculateStack(false);
                    break;
                case "char":
                    break;
                default:
                    break;
            }
        }

        private void GenerateMinus(ITree node, List<string> outputFile)
        {
            Generate(node.GetChild(0), outputFile);
            Generate(node.GetChild(1), outputFile);

            IfsType operandsType = (GetChildByType(node.GetChild(0), fsharp_ssParser.TYPE) as fsTreeNode).NodeType;
            switch (operandsType.Name)
            {
                case "int":
                    outputFile.Add("isub");
                    RecalculateStack(false);
                    break;
                case "double":
                    outputFile.Add("dsub");
                    RecalculateStack(false);
                    break;
                case "char":
                    break;
                default:
                    break;
            }
        }

        private void GenerateMult(ITree node, List<string> outputFile)
        {
            Generate(node.GetChild(0), outputFile);
            Generate(node.GetChild(1), outputFile);

            IfsType operandsType = (GetChildByType(node.GetChild(0), fsharp_ssParser.TYPE) as fsTreeNode).NodeType;
            switch (operandsType.Name)
            {
                case "int":
                    outputFile.Add("imul");
                    RecalculateStack(false);
                    break;
                case "double":
                    outputFile.Add("dmul");
                    RecalculateStack(false);
                    break;
                case "char":
                    break;
                default:
                    break;
            }
        }

        private void GenerateDivide(ITree node, List<string> outputFile)
        {
            Generate(node.GetChild(0), outputFile);
            Generate(node.GetChild(1), outputFile);

            IfsType operandsType = (GetChildByType(node.GetChild(0), fsharp_ssParser.TYPE) as fsTreeNode).NodeType;
            switch (operandsType.Name)
            {
                case "int":
                    outputFile.Add("idiv");
                    RecalculateStack(false);
                    break;
                case "double":
                    outputFile.Add("ddiv");
                    RecalculateStack(false);
                    break;
                case "char":
                    break;
                default:
                    break;
            }
        }

        private void GenerateInt(ITree node, List<string> outputFile)
        {
            outputFile.Add($"ldc {node.Text}");
            RecalculateStack(true);
        }

        private void GenerateDouble(ITree node, List<string> outputFile)
        {
            outputFile.Add($"ldc {node.Text}");
            RecalculateStack(true);
        }

        private void GenerateBody(ITree node, List<string> outputFile)
        {
            for (int i = 0; i < node.GetChild(0).ChildCount; i++)
            {
                Generate(node.GetChild(0).GetChild(i), outputFile);
            }
        }

        private void GenerateIfClause(ITree node, List<string> outputFile)
        {
            ITree conditionStatement = node.GetChild(0);
            for (int i = 1; i < node.ChildCount; i++)
            {
                //outputFile.Add();
            }
        }

        private void GenerateFunctionDefn(ITree node, List<string> outputFile)
        {
            enteredFunctionsNames.Add(outputFileName);
            maxStackCounter = 0;
            stackCounter = 0;
            string funcName = GetChildByType(node, fsharp_ssParser.NAME).GetChild(0).Text;

            if (funcName == "main")
            {
                outputFile.Add(".method public static main([Ljava/lang/String;)V");
                outputFile.Add(".limit stack ");
                currentArgsPosition.Push(outputFile.Count - 2);
                Generate(GetChildByType(node, fsharp_ssParser.BODY), outputFile);
                for (int i = 0; i < outputFile.Count; i++)
                {
                    if (outputFile[i] == ".limit stack ")
                    {
                        outputFile[i] += maxStackCounter;
                    }
                }
                outputFile.Add("return");
                outputFile.Add(".end method");
                currentArgsPosition.Pop();
            }
            else
            {
                enteredFunctionsNames.Add(funcName);
                List<string> funcClassFile = new List<string>();

                GenerateFileHeader(funcClassFile, funcName);

                ITree argsNode = GetChildByType(node, fsharp_ssParser.ARGS);

                for (int i = 0; i < argsNode.ChildCount; i++)
                {
                    ITree typeNode = GetChildByType(node, fsharp_ssParser.TYPE);
                    List <IfsType> argTypes = ((typeNode as fsTreeNode).NodeType as fsType).Types;
                    string argTypeFuncName = argTypes[i].Name == "function" ? argsNode.GetChild(i).Text : null;
                    funcClassFile.Add($".field public _{argsNode.GetChild(i).Text} {GetLowLevelTypeName(argTypes[i].Name, argTypeFuncName)};");
                }

                if (enteredFunctionsNames.Count > 1)
                { 
                    funcClassFile.Add($"field public ___context {enteredFunctionsNames[enteredFunctionsNames.Count - 2]};");
                }

                currentArgsPosition.Push(funcClassFile.Count - 1);

                funcClassFile.Add(".method public <init>()V");
                funcClassFile.Add(".limit stack 1");
                funcClassFile.Add("aload_0");
                funcClassFile.Add("invokespecial java/lang/Object/<init>()V");
                funcClassFile.Add("return");
                funcClassFile.Add(".end method");                

                IfsType returningNode = (GetChildByType(node, fsharp_ssParser.TYPE) as fsTreeNode).NodeType;
                string returningStatement = GetReturningStatementByType(returningNode.Name);
                funcClassFile.Add(".method public invoke()V");
                funcClassFile.Add(".limit stack ");
                Generate(GetChildByType(node, fsharp_ssParser.BODY), funcClassFile);
                funcClassFile.Add(returningStatement);
                funcClassFile.Add(".end method");

                SaveToFile(funcClassFile, funcName);
                //outputFile.Add($".field public __f_{funcName} {funcName};");
                currentArgsPosition.Pop();
            }

            enteredFunctionsNames.RemoveAt(enteredFunctionsNames.Count - 1);
        }

        private void GenerateFuncCall(ITree node, List<string> outputFile)
        {
            string funcName = GetChildByType(node, fsharp_ssParser.NAME).GetChild(0).Text;
            ITree args = GetChildByType(node, fsharp_ssParser.ARGS);

            if (funcName == "printf")
            {
                ITree firstArg = args.GetChild(0);
                ITree secondArg = args.GetChild(1);
                IfsType argType = (GetChildByType(args.GetChild(1), fsharp_ssParser.TYPE) as fsTreeNode).NodeType;

                string printfType = GetTypeByPrintfArg(firstArg.Text);
                string loadConstInstruction = "ldc";
                if (printfType == "D")
                {
                    loadConstInstruction += "_w";
                }

                outputFile.Add("getstatic java/lang/System/out Ljava/io/PrintStream;");
                outputFile.Add($"{loadConstInstruction} {secondArg.Text}");
                outputFile.Add($"invokevirtual java/io/PrintStream/println({printfType};)V");
            }

            IfsType returningType = (GetChildByType(node, fsharp_ssParser.TYPE) as fsTreeNode).NodeType;
            fsVariableInfo varInfo = (GetChildByType(node, VAR_INFO) as fsTreeNode).VarInfo;
            IfsType varType = (GetChildByType(node, fsharp_ssParser.TYPE) as fsTreeNode).NodeType;

            if (varInfo.PositionInParentScopeType == ScopePositionType.functionClass)
            {
                outputFile.Add($"new {funcName}");
                outputFile.Add("dup");
                outputFile.Add($"invokespecial {funcName}/<init>()V");
            }
            else
            {
                switch (varInfo.PositionInScopeType)
                {
                    case ScopePositionType.functionArg:
                        outputFile.Add("aload_0");
                        outputFile.Add($"getfield {enteredFunctionsNames[enteredFunctionsNames.Count - 1]}/_{node.Text} {varType.Name};");
                        break;
                    case ScopePositionType.local:
                        outputFile.Add("aload_0");
                        outputFile.Add($"getfield {enteredFunctionsNames[enteredFunctionsNames.Count - 1]}/__{node.Text} {varType.Name};");
                        break;
                    case ScopePositionType.outer:
                        outputFile.Add("aload_0");
                        int i;
                        for (i = 1; i <= varInfo.ScopeNestingDepth; i++)
                        {
                            outputFile.Add($"getfield {enteredFunctionsNames[enteredFunctionsNames.Count - i]}/___context {enteredFunctionsNames[enteredFunctionsNames.Count - i - 1]};");
                        }

                        switch (varInfo.PositionInParentScopeType)
                        {
                            case ScopePositionType.functionArg:
                                outputFile.Add($"getfield {enteredFunctionsNames[enteredFunctionsNames.Count - i]}/_{node.Text} {varType.Name};");
                                break;
                            case ScopePositionType.local:
                                outputFile.Add($"getfield {enteredFunctionsNames[enteredFunctionsNames.Count - i]}/__{node.Text} {varType.Name};");
                                break;
                            default:
                                break;
                        }
                        break;
                    default:
                        break;
                }
            }
            
            outputFile.Add($"astore_1");

            for (int i = 0; i < args.ChildCount; i++)
            {
                IfsType argType = (GetChildByType(args.GetChild(i), fsharp_ssParser.TYPE) as fsTreeNode).NodeType;
                outputFile.Add($"aload_1");
                Generate(args.GetChild(i), outputFile);
                outputFile.Add($"putfield {funcName}/{args.GetChild(i).Text} {GetLowLevelTypeName(argType.Name, null)};");
            }

            outputFile.Add($"aload_1");

            if (returningType.Name != "function")
            {
                outputFile.Add($"invokevirtual {funcName}/invoke()V");
            }
        }

        private string GetTypeByPrintfArg(string arg)
        {
            switch (arg)
            {
                case "%d":
                    return "D";
                case "%i":
                    return "I";
                case "%s":
                    return "Ljava/lang/String";
                default:
                    throw new Exception("Invalid printf type");
            }
        }

        private void GenerateValueDefn(ITree node, List<string> outputFile)
        {
            string varName = GetChildByType(node, fsharp_ssParser.NAME).GetChild(0).Text;
            string varType = (GetChildByType(node, fsharp_ssParser.TYPE) as fsTreeNode).NodeType.Name;
            outputFile.Insert(currentArgsPosition.Peek(), $".field public __{varName} {GetLowLevelTypeName(varType, null)};");
            ITree bodyNode = GetChildByType(node, fsharp_ssParser.BODY);
            outputFile.Add($"aload_0");
            Generate(bodyNode, outputFile);
            outputFile.Add($"putfield {enteredFunctionsNames[enteredFunctionsNames.Count - 1]}/__{varName} {GetLowLevelTypeName(varType, null)};");
        }

        private void GenerateID(ITree node, List<string> outputFile)
        {            
            fsVariableInfo varInfo = (GetChildByType(node, VAR_INFO) as fsTreeNode).VarInfo;
            IfsType varType = (GetChildByType(node, fsharp_ssParser.TYPE) as fsTreeNode).NodeType;

            switch (varInfo.PositionInScopeType)
            {
                case ScopePositionType.functionClass:
                    outputFile.Add($"new {node.Text}");
                    break;
                case ScopePositionType.functionArg:
                    outputFile.Add("aload_0");
                    outputFile.Add($"getfield {enteredFunctionsNames[enteredFunctionsNames.Count - 1]}/_{node.Text} {varType.Name};");
                    break;
                case ScopePositionType.local:
                    outputFile.Add("aload_0");
                    outputFile.Add($"getfield {enteredFunctionsNames[enteredFunctionsNames.Count - 1]}/__{node.Text} {varType.Name};");
                    break;
                case ScopePositionType.outer:
                    outputFile.Add("aload_0");
                    int i;
                    for (i = 1; i <= varInfo.ScopeNestingDepth; i++)
                    {
                        outputFile.Add($"getfield {enteredFunctionsNames[enteredFunctionsNames.Count - i]}/___context {enteredFunctionsNames[enteredFunctionsNames.Count - i - 1]};");
                    }

                    switch (varInfo.PositionInParentScopeType)
                    {
                        case ScopePositionType.functionClass:
                            //TODO
                            break;
                        case ScopePositionType.functionArg:
                            outputFile.Add($"getfield {enteredFunctionsNames[enteredFunctionsNames.Count - i]}/_{node.Text} {varType.Name};");
                            break;
                        case ScopePositionType.local:
                            outputFile.Add($"getfield {enteredFunctionsNames[enteredFunctionsNames.Count - i]}/__{node.Text} {varType.Name};");
                            break;
                        default:
                            break;
                    }
                    break;
                default:
                    break;
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

        private void RecalculateStack(bool increaseSize)
        {
            if (increaseSize)
            {
                stackCounter++;
                if (stackCounter > maxStackCounter)
                {
                    maxStackCounter = stackCounter;
                }
            }
            else
            {
                stackCounter--;
            }
        }

        private string GetReturningStatementByType(string typeName)
        {
            switch (typeName)
            {
                case "int":
                    return "ireturn";
                case "double":
                    return "dreturn";
                default:
                    return "areturn";
            }
        }

        private string GetLowLevelTypeName(string inputName, string funcName)
        {
            switch (inputName)
            {
                case "int":
                    return "I";
                case "double":
                    return "D";
                case "string":
                    return "Ljava/lang/String";
                case "function":
                    return funcName;
                default:
                    if (inputName[inputName.Length - 1] == '\'')
                        return "java/lang/Object";
                    else
                        throw new Exception($"Type {inputName} not presented in JVM");
            }
        }

        private void SaveToFile(List<string> code, string fileName)
        {
            if (!File.Exists($"{outputFilesPath}/{fileName}.j"))
            {
                File.Create($"{outputFilesPath}/{fileName}.j");
            }
            
            TextWriter file = new StreamWriter($"{outputFilesPath}/{fileName}.j");
            foreach (var line in code)
            {
                file.WriteLine(line);
            }
            file.Close();
        }
    }
}
