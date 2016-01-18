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
        private const int DER_FUNC_INFO = 66;

        private string outputFilesPath;
        private string outputFileName;
        private Dictionary<int, CodeGenDelegate> generationFunctions;
        private int localVarsCounter = 0;
        private int maxStackCounter = 0;
        private int stackCounter = 0;
        private int labelNum = 0;
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
            generationFunctions.Add(fsharp_ssParser.ID, GenerateID);
            generationFunctions.Add(fsharp_ssParser.INT, GenerateInt);
            generationFunctions.Add(fsharp_ssParser.DOUBLE, GenerateDouble);
            //inferenceFunctions.Add(fsharp_ssParser.CHAR, InferCharType);
            //inferenceFunctions.Add(fsharp_ssParser.TRUE, InferBoolType);
            //inferenceFunctions.Add(fsharp_ssParser.FALSE, InferBoolType);
            generationFunctions.Add(fsharp_ssParser.STRING, GenerateString);
            generationFunctions.Add(fsharp_ssParser.BODY, GenerateBody);
            generationFunctions.Add(fsharp_ssParser.IF, GenerateIfClause);
            generationFunctions.Add(fsharp_ssParser.ELIF, GenerateElifClause);
            generationFunctions.Add(fsharp_ssParser.EQ, GenerateEqOper);
            generationFunctions.Add(fsharp_ssParser.NEQ, GenerateNeqOper);
            generationFunctions.Add(fsharp_ssParser.GT, GenerateGTOper);
            generationFunctions.Add(fsharp_ssParser.GE, GenerateGEOper);
            generationFunctions.Add(fsharp_ssParser.LT, GenerateLTOper);
            generationFunctions.Add(fsharp_ssParser.LE, GenerateLEOper);
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
                    outputFile.Add("fadd");
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
                    outputFile.Add("fsub");
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
                    outputFile.Add("fmul");
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
                    outputFile.Add("fdiv");
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

        private void GenerateString(ITree node, List<string> outputFile)
        {
            outputFile.Add($"ldc {node.Text}");
            RecalculateStack(true);
        }

        private void GenerateBody(ITree node, List<string> outputFile)
        {
            if (node.GetChild(0).Text == "elif")
            {
                Generate(node.GetChild(0), outputFile);
            }
            else
            {
                for (int i = 0; i < node.GetChild(0).ChildCount; i++)
                {
                    Generate(node.GetChild(0).GetChild(i), outputFile);
                }
            }
        }

        private void GenerateIfClause(ITree node, List<string> outputFile)
        {
            ITree conditionStatement = node.GetChild(0);
            Generate(conditionStatement, outputFile);

            int escapeLabelNum = labelNum + node.ChildCount - 3;
            bool isElseBlockExists = node.GetChild(node.ChildCount - 2).GetChild(0).Text != "elif";

            for (int i = 1; i < node.ChildCount; i++)
            {
                if (node.GetChild(i).Type == fsharp_ssParser.BODY)
                {
                    if (i > 1)
                        outputFile.Add($"label_{labelNum++}:");

                    Generate(node.GetChild(i), outputFile);

                    if (i < node.ChildCount - 2)
                    {
                        outputFile.Add($"goto label_{escapeLabelNum}");
                    }
                }
            }

            outputFile.Add($"label_{labelNum++}:");
        }

        private void GenerateElifClause(ITree node, List<string> outputFile)
        {
            Generate(node.GetChild(0), outputFile);

            for (int i = 0; i < node.GetChild(1).ChildCount; i++)
            {
                Generate(node.GetChild(1).GetChild(i), outputFile);
            }
        }

        private void GenerateEqOper(ITree node, List<string> outputFile)
        {
            Generate(node.GetChild(0), outputFile);
            Generate(node.GetChild(1), outputFile);

            switch ((GetChildByType(node.GetChild(0), fsharp_ssParser.TYPE) as fsTreeNode).NodeType.Name)
            {
                case "int":
                    outputFile.Add($"if_icmpne label_{labelNum}");
                    break;
                case "double":
                    outputFile.Add($"dcmpl");
                    outputFile.Add($"ifne label_{labelNum}");
                    break;
                default:
                    break;
            }
        }

        private void GenerateNeqOper(ITree node, List<string> outputFile)
        {
            Generate(node.GetChild(0), outputFile);
            Generate(node.GetChild(1), outputFile);

            switch ((GetChildByType(node.GetChild(0), fsharp_ssParser.TYPE) as fsTreeNode).NodeType.Name)
            {
                case "int":
                    outputFile.Add($"if_icmpeq label_{labelNum}");
                    break;
                case "double":
                    outputFile.Add($"dcmpl");
                    outputFile.Add($"ifeq label_{labelNum}");
                    break;
                default:
                    break;
            }
        }

        private void GenerateGEOper(ITree node, List<string> outputFile)
        {
            Generate(node.GetChild(0), outputFile);
            Generate(node.GetChild(1), outputFile);

            switch ((GetChildByType(node.GetChild(0), fsharp_ssParser.TYPE) as fsTreeNode).NodeType.Name)
            {
                case "int":
                    outputFile.Add($"if_icmplt label_{labelNum}");
                    break;
                case "double":
                    outputFile.Add($"dcmpl");
                    outputFile.Add($"iflt label_{labelNum}");
                    break;
                default:
                    break;
            }
        }

        private void GenerateGTOper(ITree node, List<string> outputFile)
        {
            Generate(node.GetChild(0), outputFile);
            Generate(node.GetChild(1), outputFile);

            switch ((GetChildByType(node.GetChild(0), fsharp_ssParser.TYPE) as fsTreeNode).NodeType.Name)
            {
                case "int":
                    outputFile.Add($"if_icmple label_{labelNum}");
                    break;
                case "double":
                    outputFile.Add($"dcmpl");
                    outputFile.Add($"ifle label_{labelNum}");
                    break;
                default:
                    break;
            }
        }

        private void GenerateLEOper(ITree node, List<string> outputFile)
        {
            Generate(node.GetChild(0), outputFile);
            Generate(node.GetChild(1), outputFile);

            switch ((GetChildByType(node.GetChild(0), fsharp_ssParser.TYPE) as fsTreeNode).NodeType.Name)
            {
                case "int":
                    outputFile.Add($"if_icmpgt label_{labelNum}");
                    break;
                case "double":
                    outputFile.Add($"dcmpl");
                    outputFile.Add($"ifgt label_{labelNum}");
                    break;
                default:
                    break;
            }
        }

        private void GenerateLTOper(ITree node, List<string> outputFile)
        {
            Generate(node.GetChild(0), outputFile);
            Generate(node.GetChild(1), outputFile);

            switch ((GetChildByType(node.GetChild(0), fsharp_ssParser.TYPE) as fsTreeNode).NodeType.Name)
            {
                case "int":
                    outputFile.Add($"if_icmpge label_{labelNum}");
                    break;
                case "double":
                    outputFile.Add($"dcmpl");
                    outputFile.Add($"ifge label_{labelNum}");
                    break;
                default:
                    break;
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
                outputFile.Add(".method public <init>()V");
                outputFile.Add(".limit stack 1");
                outputFile.Add("aload_0");
                RecalculateStack(true);
                outputFile.Add("invokespecial java/lang/Object/<init>()V");
                outputFile.Add("return");
                outputFile.Add(".end method");
                outputFile.Add(".method public static main([Ljava/lang/String;)V");
                outputFile.Add(".limit stack ");
                outputFile.Add(".limit locals 2");
                currentArgsPosition.Push(outputFile.Count - 9);
                Generate(GetChildByType(node, fsharp_ssParser.BODY), outputFile);
                for (int i = 0; i < outputFile.Count; i++)
                {
                    if (outputFile[i] == ".limit stack ")
                    {
                        outputFile[i] += maxStackCounter;
                    }
                }
                outputFile.Add("pop");
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
                ITree typeNode = GetChildByType(node, fsharp_ssParser.TYPE);
                List<IfsType> argTypes = ((typeNode as fsTreeNode).NodeType as fsType).Types;
                fsDerFuncInfo funcInfo = (GetChildByType(node, DER_FUNC_INFO) as fsTreeNode).DerFuncInfo;

                for (int i = 0; i < argsNode.ChildCount; i++)
                {                    
                    string argTypeFuncName = argTypes[i].Name == "function" ? argsNode.GetChild(i).Text : null;
                    string staticSpec = enteredFunctionsNames[enteredFunctionsNames.Count - 1] == outputFileName ? "static" : "";
                    funcClassFile.Add($".field public {staticSpec} _{funcInfo.ArgsNames[i]} {GetLowLevelTypeName(argTypes[i].Name, argTypeFuncName)}");
                }

                if (enteredFunctionsNames.Count > 1)
                {
                    string staticSpec = enteredFunctionsNames[enteredFunctionsNames.Count - 1] == outputFileName ? "static" : "";
                    funcClassFile.Add($".field public {staticSpec} ___context L{enteredFunctionsNames[enteredFunctionsNames.Count - 2]};");
                }

                currentArgsPosition.Push(funcClassFile.Count - 1);

                funcClassFile.Add(".method public <init>()V");
                funcClassFile.Add(".limit stack 1");
                funcClassFile.Add("aload_0");
                RecalculateStack(true);
                funcClassFile.Add("invokespecial java/lang/Object/<init>()V");
                funcClassFile.Add("return");
                funcClassFile.Add(".end method");                

                fsType returningNode = (GetChildByType(node, fsharp_ssParser.TYPE) as fsTreeNode).NodeType as fsType;
                string returningStatement = GetReturningStatementByType(returningNode.Types[returningNode.Types.Count - 1].Name);
                funcClassFile.Add($".method public invoke(){GetLowLevelTypeName(argTypes[argTypes.Count - 1].Name, null)}");
                funcClassFile.Add(".limit stack ");
                funcClassFile.Add(".limit locals 2");
                Generate(GetChildByType(node, fsharp_ssParser.BODY), funcClassFile);
                funcClassFile.Add(returningStatement);
                for (int i = 0; i < funcClassFile.Count; i++)
                {
                    if (funcClassFile[i] == ".limit stack ")
                    {
                        funcClassFile[i] += maxStackCounter;
                    }
                }
                funcClassFile.Add(".end method");

                SaveToFile(funcClassFile, funcName);
                currentArgsPosition.Pop();
            }

            enteredFunctionsNames.RemoveAt(enteredFunctionsNames.Count - 1);
        }

        private void GenerateFuncCall(ITree node, List<string> outputFile)
        {
            string funcName = GetChildByType(node, fsharp_ssParser.NAME).GetChild(0).Text;
            ITree args = GetChildByType(node, fsharp_ssParser.ARGS);

            if (funcName.Substring(0, "printf".Length) == "printf")
            {
                ITree firstArg = args.GetChild(0);
                IfsType argType = (GetChildByType(args.GetChild(1), fsharp_ssParser.TYPE) as fsTreeNode).NodeType;

                outputFile.Add("getstatic java/lang/System/out Ljava/io/PrintStream;");
                Generate(args.GetChild(1), outputFile);
                string printfType = GetTypeByPrintfArg(firstArg.Text);
                outputFile.Add($"invokevirtual java/io/PrintStream/println({printfType})V");
                RecalculateStack(false);
                RecalculateStack(false);
                return;
            }

            IfsType returningType = (GetChildByType(node, fsharp_ssParser.TYPE) as fsTreeNode).NodeType;
            fsVariableInfo varInfo = (GetChildByType(node, VAR_INFO) as fsTreeNode).VarInfo;
            IfsType varType = (GetChildByType(node, fsharp_ssParser.TYPE) as fsTreeNode).NodeType;
            fsDerFuncInfo funcInfo = (GetChildByType(node, DER_FUNC_INFO) as fsTreeNode).DerFuncInfo;
            string argTypeFuncName = funcInfo.Name;

            if (varInfo.PositionInParentScopeType == ScopePositionType.functionClass)
            {
                outputFile.Add($"new {funcName}");
                RecalculateStack(true);
                outputFile.Add("dup");
                RecalculateStack(true);
                outputFile.Add($"invokespecial {funcName}/<init>()V");
            }
            else
            {
                switch (varInfo.PositionInScopeType)
                {
                    //recursive function defenition
                    case ScopePositionType.functionClass:
                        outputFile.Add($"new {funcName}");
                        RecalculateStack(true);
                        outputFile.Add("dup");
                        RecalculateStack(true);
                        outputFile.Add($"invokespecial {funcName}/<init>()V");
                        break;
                    case ScopePositionType.functionArg:
                        if (enteredFunctionsNames[enteredFunctionsNames.Count - 1] != outputFileName)
                        {
                            outputFile.Add("aload_0");
                            RecalculateStack(true);
                            outputFile.Add($"getfield {enteredFunctionsNames[enteredFunctionsNames.Count - 1]}/_{funcName} {GetLowLevelTypeName("function", argTypeFuncName)}");
                        }
                        else
                        {
                            outputFile.Add($"getstatic {enteredFunctionsNames[enteredFunctionsNames.Count - 1]}/_{funcName} {GetLowLevelTypeName("function", argTypeFuncName)}");
                        }
                        break;
                    case ScopePositionType.local:
                        if (enteredFunctionsNames[enteredFunctionsNames.Count - 1] != outputFileName)
                        {
                            outputFile.Add("aload_0");
                            RecalculateStack(true);
                            outputFile.Add($"getfield {enteredFunctionsNames[enteredFunctionsNames.Count - 1]}/__{funcName} {GetLowLevelTypeName("function", argTypeFuncName)}");
                        }
                        else
                        {
                            outputFile.Add($"getstatic {enteredFunctionsNames[enteredFunctionsNames.Count - 1]}/__{funcName} {GetLowLevelTypeName("function", argTypeFuncName)}");
                        }
                        break;
                    case ScopePositionType.outer:
                        if (enteredFunctionsNames[enteredFunctionsNames.Count - 1] != outputFileName)
                        {
                            outputFile.Add("aload_0");
                        }
                        RecalculateStack(true);
                        int i;
                        for (i = 1; i <= varInfo.ScopeNestingDepth; i++)
                        {
                            outputFile.Add($"getfield {enteredFunctionsNames[enteredFunctionsNames.Count - i]}/___context L{enteredFunctionsNames[enteredFunctionsNames.Count - i - 1]};");
                        }

                        switch (varInfo.PositionInParentScopeType)
                        {
                            case ScopePositionType.functionArg:
                                outputFile.Add($"getfield {enteredFunctionsNames[enteredFunctionsNames.Count - i]}/_{funcName} {GetLowLevelTypeName("function", argTypeFuncName)}");
                                break;
                            case ScopePositionType.local:
                                outputFile.Add($"getfield {enteredFunctionsNames[enteredFunctionsNames.Count - i]}/__{funcName} {GetLowLevelTypeName("function", argTypeFuncName)}");
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
                RecalculateStack(true);
                Generate(args.GetChild(i), outputFile);
                outputFile.Add($"putfield {funcInfo.Name}/_{funcInfo.ArgsNames[funcInfo.BeforePassedArgsNum + i]} {GetLowLevelTypeName(argType.Name, null)}");
            }

            outputFile.Add($"aload_1");

            if (returningType.Name != "function")
            {
                outputFile.Add($"invokevirtual {funcInfo.Name}/invoke(){GetLowLevelTypeName(returningType.Name, null)}");
                RecalculateStack(true);
            }
        }

        private string GetTypeByPrintfArg(string arg)
        {
            arg = arg.Substring(1, 2);
            switch (arg)
            {
                case "%d":
                    return "F";
                case "%i":
                    return "I";
                case "%s":
                    return "Ljava/lang/String;";
                default:
                    throw new Exception("Invalid printf type");
            }
        }

        private void GenerateValueDefn(ITree node, List<string> outputFile)
        {
            string varName = GetChildByType(node, fsharp_ssParser.NAME).GetChild(0).Text;
            string varType = (GetChildByType(node, fsharp_ssParser.TYPE) as fsTreeNode).NodeType.Name;

            string argTypeFuncName = null;
            if (varType == "function")
            {
                fsDerFuncInfo funcInfo = (GetChildByType(node, DER_FUNC_INFO) as fsTreeNode).DerFuncInfo;
                argTypeFuncName = funcInfo.Name;
            }

            string staticSpec = enteredFunctionsNames[enteredFunctionsNames.Count - 1] == outputFileName ? "static" : "";
            outputFile.Insert(currentArgsPosition.Peek(), $".field {staticSpec} public __{varName} {GetLowLevelTypeName(varType, argTypeFuncName)}");
            ITree bodyNode = GetChildByType(node, fsharp_ssParser.BODY);
            if (enteredFunctionsNames[enteredFunctionsNames.Count - 1] != outputFileName)
            {
                outputFile.Add("aload_0");
                RecalculateStack(true);
                Generate(bodyNode, outputFile);
                outputFile.Add($"putfield {enteredFunctionsNames[enteredFunctionsNames.Count - 1]}/__{varName} {GetLowLevelTypeName(varType, argTypeFuncName)}");
            }
            else
            {
                Generate(bodyNode, outputFile);
                outputFile.Add($"putstatic {enteredFunctionsNames[enteredFunctionsNames.Count - 1]}/__{varName} {GetLowLevelTypeName(varType, argTypeFuncName)}");
            }
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
                    if (enteredFunctionsNames[enteredFunctionsNames.Count - 1] != outputFileName)
                    {
                        outputFile.Add("aload_0");
                        RecalculateStack(true);
                        outputFile.Add($"getfield {enteredFunctionsNames[enteredFunctionsNames.Count - 1]}/_{node.Text} {GetLowLevelTypeName(varType.Name, null)}");
                    }
                    else
                    {
                        outputFile.Add($"getstatic {enteredFunctionsNames[enteredFunctionsNames.Count - 1]}/_{node.Text} {GetLowLevelTypeName(varType.Name, null)}");
                    }
                    break;
                case ScopePositionType.local:
                    if (enteredFunctionsNames[enteredFunctionsNames.Count - 1] != outputFileName)
                    {
                        outputFile.Add("aload_0");
                        RecalculateStack(true);
                        outputFile.Add($"getfield {enteredFunctionsNames[enteredFunctionsNames.Count - 1]}/__{node.Text} {GetLowLevelTypeName(varType.Name, null)}");
                    }
                    else
                    {
                        outputFile.Add($"getstatic {enteredFunctionsNames[enteredFunctionsNames.Count - 1]}/__{node.Text} {GetLowLevelTypeName(varType.Name, null)}");
                    }
                    break;
                case ScopePositionType.outer:
                    if (enteredFunctionsNames[enteredFunctionsNames.Count - 1] != outputFileName)
                    {
                        outputFile.Add("aload_0");
                    }
                    RecalculateStack(true);
                    int i;
                    for (i = 1; i <= varInfo.ScopeNestingDepth; i++)
                    {
                        outputFile.Add($"getfield {enteredFunctionsNames[enteredFunctionsNames.Count - i]}/___context L{enteredFunctionsNames[enteredFunctionsNames.Count - i - 1]}");
                    }

                    switch (varInfo.PositionInParentScopeType)
                    {
                        case ScopePositionType.functionClass:
                            //TODO
                            break;
                        case ScopePositionType.functionArg:
                            outputFile.Add($"getfield {enteredFunctionsNames[enteredFunctionsNames.Count - i]}/_{node.Text} {GetLowLevelTypeName(varType.Name, null)}");
                            break;
                        case ScopePositionType.local:
                            outputFile.Add($"getfield {enteredFunctionsNames[enteredFunctionsNames.Count - i]}/__{node.Text} {GetLowLevelTypeName(varType.Name, null)}");
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
            outputFile.Add($".super Ljava/lang/Object;");
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
                    return "freturn";
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
                    return "F";
                case "string":
                    return "Ljava/lang/String;";
                case "function":
                    return $"L{funcName};";
                default:
                    if (inputName[inputName.Length - 1] == '\'')
                        return "Ljava/lang/Object;";
                    else
                        throw new Exception($"Type {inputName} not presented in JVM");
            }
        }

        private void SaveToFile(List<string> code, string fileName)
        {
            TextWriter file = null;
            if (!File.Exists($"{outputFilesPath}/{fileName}.j"))
            {
                file = new StreamWriter(File.Create($"{outputFilesPath}/{fileName}.j"));
            }
            else
            {
                file = new StreamWriter($"{outputFilesPath}/{fileName}.j");
            }

            foreach (var line in code)
            {
                file.WriteLine(line);
            }
            file.Close();
        }
    }
}
