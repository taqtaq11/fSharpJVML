using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Antlr.Runtime.Tree;
using fsharp_ss;

namespace fSharpJVML
{
    delegate IfsType InferNodeTypeDelegate(ITree node, fsScope scope);

    class fsTypeInferer
    {
        private const int VAR_INFO = 65;

        private ITree tree;
        private Dictionary<int, InferNodeTypeDelegate> inferenceFunctions;

        private List<string> enteredFunctionsNames;

        //info of last called function
        private fsDerFuncInfo derFunction;
        //infos for all defined functions
        private Dictionary<string, fsDerFuncInfo> functionsInfos;
        //table matches func vars with func infos
        private Dictionary<string, fsDerFuncInfo> varDerFuncTable;
        private int uniqueFuctionId = 0;


        public fsTypeInferer(ITree tree, string outFileName)
        {
            this.tree = tree;
            functionsInfos = new Dictionary<string, fsDerFuncInfo>();
            varDerFuncTable = new Dictionary<string, fsDerFuncInfo>();
            enteredFunctionsNames = new List<string>();
            enteredFunctionsNames.Add(outFileName);

            inferenceFunctions = new Dictionary<int, InferNodeTypeDelegate>();
            inferenceFunctions.Add(fsharp_ssParser.PROGRAM, InferProgramType);
            inferenceFunctions.Add(fsharp_ssParser.PLUS, InferPlusType);
            inferenceFunctions.Add(fsharp_ssParser.MINUS, InferMinusType);
            inferenceFunctions.Add(fsharp_ssParser.MULT, InferMultType);
            inferenceFunctions.Add(fsharp_ssParser.DIV, InferDivideType);
            inferenceFunctions.Add(fsharp_ssParser.EQ, InferEqNeqOperType);
            inferenceFunctions.Add(fsharp_ssParser.NEQ, InferEqNeqOperType);
            inferenceFunctions.Add(fsharp_ssParser.GT, InferCompareOperType);
            inferenceFunctions.Add(fsharp_ssParser.GE, InferCompareOperType);
            inferenceFunctions.Add(fsharp_ssParser.LT, InferCompareOperType);
            inferenceFunctions.Add(fsharp_ssParser.LE, InferCompareOperType);
            inferenceFunctions.Add(fsharp_ssParser.ID, InferIDType);
            inferenceFunctions.Add(fsharp_ssParser.INT, InferIntType);
            inferenceFunctions.Add(fsharp_ssParser.DOUBLE, InferDoubleType);
            inferenceFunctions.Add(fsharp_ssParser.CHAR, InferCharType);
            inferenceFunctions.Add(fsharp_ssParser.TRUE, InferBoolType);
            inferenceFunctions.Add(fsharp_ssParser.FALSE, InferBoolType);
            inferenceFunctions.Add(fsharp_ssParser.STRING, InferStringType);
            inferenceFunctions.Add(fsharp_ssParser.BODY, InferBodyType);
            inferenceFunctions.Add(fsharp_ssParser.IF, InferIfClauseType);
            inferenceFunctions.Add(fsharp_ssParser.ELIF, InferElifClauseType);
            inferenceFunctions.Add(fsharp_ssParser.FUNCTION_DEFN, InferFunctionDefnType);
            inferenceFunctions.Add(fsharp_ssParser.FUNCTION_CALL, InferFuncCallType);
            inferenceFunctions.Add(fsharp_ssParser.VALUE_DEFN, InferValueDefnType);
        }

        public ITree Infer()
        {
            fsScope defaultScope = new fsScope(null);
            defaultScope.AddFunction("printf", fsType.GetFunctionType(new List<IfsType>() {
                                                                                            fsType.GetStringType(),
                                                                                            new fsTypeVar(),
                                                                                            fsType.GetFunctionType(null)
                                                                                          }));
            defaultScope.SetVarInfo("printf", ScopePositionType.functionClass);
            functionsInfos.Add("printf", new fsDerFuncInfo("printf", new List<string>(), 0, 0, ""));
            Analyse(tree, defaultScope);
            return tree;
        }

        private IfsType Analyse(ITree node, fsScope scope)
        {
            IfsType nodeType = null;

            if (inferenceFunctions.ContainsKey(node.Type))
            {
                nodeType = inferenceFunctions[node.Type](node, scope);
                ITree childTypeNode = GetChildByType(node, fsharp_ssParser.TYPE);

                if (childTypeNode != null)
                {
                    node.DeleteChild(GetChildIndexByType(node, fsharp_ssParser.TYPE));
                }

                fsTreeNode typeNameNode = new fsTreeNode(nodeType);
                fsScope.ScopeVarOrFuncTypeChanged += typeNameNode.ScopeVarOrFuncTypeChangedHandler;
                node.AddChild(typeNameNode);
            }
            else
            {
                throw new Exception($"Can`t infer type for node: {node.Text}");
            }

            return nodeType;
        }

        private IfsType Prune(IfsType type)
        {
            fsTypeVar a;
            if ((a = type as fsTypeVar)?.Instance != null)
            {
                a.Instance = Prune(a.Instance);
                return a.Instance;
            }

            return type;
        }

        private bool OccursInType(IfsType t1, IfsType t2)
        {
            t2 = Prune(t2);

            if (t2.Equals(t1))
            {
                return true;
            }
            else if (t2 is fsType)
            {
                return OccursInTypeArray(t1, (t2 as fsType).Types);
            }

            return false;
        }

        private bool OccursInTypeArray(IfsType type, List<IfsType> types)
        {
            foreach (var t in types)
            {
                if (OccursInType(type, t))
                {
                    return true;
                }
            }

            return false;
        }

        private void UnifyWrapper(ref IfsType t1, ref IfsType t2, fsScope scope)
        {
            bool isT1FromScope = false;
            bool isT2FromScope = false;
            string t1ScopeName = "";
            string t1NameBeforeUnification = t1.Name;
            string t2ScopeName = "";
            string t2NameBeforeUnification = t2.Name;

            if (t1 is fsTypeVar)
            {
                isT1FromScope = (t1 as fsTypeVar).IsFromScope;
                t1ScopeName = (t1 as fsTypeVar).ScopeVarName;
            }

            if (t2 is fsTypeVar)
            {
                isT2FromScope = (t2 as fsTypeVar).IsFromScope;
                t2ScopeName = (t2 as fsTypeVar).ScopeVarName;
            }

            Unify(ref t1, ref t2, scope);

            if (isT1FromScope)
            {
                scope.ChangeVarOrFuncType(t1ScopeName, t1NameBeforeUnification, t1);
            }

            if (isT2FromScope)
            {
                scope.ChangeVarOrFuncType(t2ScopeName, t2NameBeforeUnification, t2);
            }
        }

        private void Unify(ref IfsType t1, ref IfsType t2, fsScope scope)
        {
            if (t1.Name == "identity")
            {
                t1 = t2;
                return;
            }

            if (t2.Name == "identity")
            {
                t2 = t1;
                return;
            }

            IfsType t1Pruned = Prune(t1);
            IfsType t2Pruned = Prune(t2);

            if (t1Pruned.Name != "composite")
            {
                t1 = t1Pruned;
            }

            if (t2Pruned.Name != "composite")
            {
                t2 = t2Pruned;
            }

            if (t1 is fsTypeVar)
            {
                if (!t1.Equals(t2))
                {
                    if (OccursInType(t1, t2))
                    {
                        throw new Exception("Recursive unification");
                    }

                    if (t2.Name == "composite")
                    {
                        (t1 as fsTypeVar).Instance = t2;
                    }
                    else
                    {
                        t1 = t2;
                    }                    
                }
            }
            else if(t1 is fsType && t2 is fsTypeVar)
            {
                UnifyWrapper(ref t2, ref t1, scope);
            }
            else if (t1 is fsType && t2 is fsType)
            {                
                fsType type1 = t1 as fsType;
                fsType type2 = t2 as fsType;

                if (type1.Name == "composite")
                {
                    if (type2.Name != "composite")
                    {
                        UnifyWrapper(ref t2, ref t1, scope);
                        return;
                    }

                    List<IfsType> commonTypes = new List<IfsType>();
                    for (int i = 0; i < type1.Types.Count; i++)
                    {
                        for (int j = 0; j < type2.Types.Count; j++)
                        {
                            if (type1.Types[i].Equals(type2.Types[j]))
                            {
                                commonTypes.Add(type1.Types[i]);
                            }
                        }
                    }

                    if (commonTypes.Count < 1)
                    {
                        throw new Exception($"Cannot unify types {type1.Name} and {type2.Name}");
                    }
                    else if (commonTypes.Count == 0)
                    {
                        t1 = t2 = commonTypes[0];
                    }
                    else
                    {
                        fsTypeVar commonType = new fsTypeVar();
                        commonType.Instance = fsType.GetCompositeType(commonTypes);
                        t1 = t2 = commonType;
                    }

                    return;
                }

                if (type2.Name == "composite")
                {
                    for (int i = 0; i < type2.Types.Count; i++)
                    {
                        if (type1.Equals(type2.Types[i]))
                        {
                            t1 = t2 = type1;
                            return;
                        }
                    }

                    throw new Exception($"Cannot unify types {type1.Name} and {type2.Name}");
                }

                if (!type1.Equals(type2))
                {
                    throw new Exception($"Cannot unify types {type1.Name} and {type2.Name}");
                }

                if (type1.Name == "function")
                {
                    if (type1.Types.Count == type2.Types.Count)
                    {
                        IfsType returningType = type2.Types[type2.Types.Count - 1];
                        for (int i = 0; i < type1.Types.Count - 1; i++)
                        {
                            IfsType t1Child = type1.Types[i];
                            IfsType t2Child = type2.Types[i];
                            string t2NameBeforeUnification = t2Child.Name;
                            //Changed !!!!!!!
                            UnifyWrapper(ref t2Child, ref t1Child, scope);
                            type1.Types[i] = t1Child;
                            type2.Types[i] = t2Child;

                            if (returningType.Name == t2NameBeforeUnification)
                            {
                                UnifyWrapper(ref returningType, ref t1Child, scope);
                                type1.Types[i] = returningType;
                                type2.Types[type2.Types.Count - 1] = t2Child;
                            }
                        }                        
                    }
                    else if (type1.Types.Count < type2.Types.Count)
                    {
                        int difference = type2.Types.Count - type1.Types.Count;

                        for (int i = 0; i < type1.Types.Count - 1; i++)
                        {
                            IfsType t1Child = type1.Types[i];
                            IfsType t2Child = type2.Types[i];
                            string t2NameBeforeUnification = t2Child.Name;
                            //Changed !!!!!!!
                            UnifyWrapper(ref t2Child, ref t1Child, scope);
                            type1.Types[i] = t1Child;
                            type2.Types[i] = t2Child;

                            for (int j = difference; j < type2.Types.Count; j++)
                            {
                                IfsType bufType = type2.Types[j];
                                if (bufType.Name == t2NameBeforeUnification)
                                {
                                    UnifyWrapper(ref t1Child, ref bufType, scope);
                                    type1.Types[i] = bufType;
                                    type2.Types[j] = t2Child;
                                }                               
                            }
                        }

                        List<IfsType> rest = type2.Types.GetRange(type1.Types.Count - 1, difference + 1);
                        type1.Types[type1.Types.Count - 1] = fsType.GetFunctionType(rest);
                        t1 = t2 = type1;
                    }
                    else
                    {
                        throw new Exception($"Cannot unify types {type1.Name} and {type2.Name}");
                    }
                }                
            }
            else
            {
                throw new Exception($"Cannot unify types {t1.Name} and {t2.Name}");
            }
        }

        private IfsType InferProgramType(ITree node, fsScope scope)
        {
            if (scope == null)
            {
                scope = new fsScope(null);
            }

            IfsType exprType;
            for (int i = 0; i < node.ChildCount; i++)
            {
                ITree childNode = node.GetChild(i);
                exprType = Analyse(childNode, scope);

                if (childNode.Type == fsharp_ssParser.FUNCTION_DEFN)
                {
                    scope.AddFunction(GetChildByType(childNode, fsharp_ssParser.NAME).GetChild(0).Text, exprType);
                    scope.SetVarInfo(GetChildByType(childNode, fsharp_ssParser.NAME).GetChild(0).Text, ScopePositionType.functionClass);
                }
                else if (childNode.Type == fsharp_ssParser.VALUE_DEFN)
                {
                    string varName = GetChildByType(childNode, fsharp_ssParser.NAME).GetChild(0).Text;
                    scope.AddVar(varName, exprType);
                    scope.SetVarInfo(varName, ScopePositionType.local);
                }
            }

            return fsType.GetProgramType();
        }

        private IfsType InferFunctionDefnType(ITree node, fsScope scope)
        {
            fsScope innerScope = new fsScope(scope);

            List<IfsType> functionTypes = new List<IfsType>();

            string funcName = GetChildByType(node, fsharp_ssParser.NAME).GetChild(0).Text;

            ITree args = GetChildByType(node, fsharp_ssParser.ARGS);
            List<string> argsNames = new List<string>();

            for (int i = 0; i < args.ChildCount; i++)
            {
                ITree arg = args.GetChild(i);

                IfsType argType;
                if (arg.ChildCount > 0)
                {
                    ITree annotatedArgTypeNode = arg.GetChild(0);
                    argType = new fsType(annotatedArgTypeNode.Text, null);                    
                }          
                else
                {
                    argType = new fsTypeVar();
                }

                argsNames.Add(arg.Text);
                innerScope.AddVar(arg.Text, argType);
                innerScope.SetVarInfo(arg.Text, ScopePositionType.functionArg);
            }

            fsDerFuncInfo funcInfo = new fsDerFuncInfo(funcName, argsNames, 0, 0, 
                enteredFunctionsNames[enteredFunctionsNames.Count - 1]);
            if (funcName != "main")
            {
                enteredFunctionsNames.Add(funcName);
            }
            functionsInfos.Add(funcName, funcInfo);
            node.AddChild(new fsTreeNode(funcInfo));
            if (GetChildByType(node, fsharp_ssParser.REC) != null)
            {
                innerScope.AddFunction(funcName, fsType.GetIdentityType(innerScope, argsNames));
                innerScope.SetVarInfo(funcName, ScopePositionType.functionClass);
            }

            IfsType bodyType = Analyse(GetChildByType(node, fsharp_ssParser.BODY), innerScope);

            ITree annotatedReturningTypeNode = GetChildByType(node, fsharp_ssParser.TYPE);
            if (annotatedReturningTypeNode != null && annotatedReturningTypeNode.ChildCount > 0)
            {
                IfsType annotatedReturningType = new fsType(annotatedReturningTypeNode.GetChild(0).Text, null);
                UnifyWrapper(ref bodyType, ref annotatedReturningType, innerScope);
            }

            for (int i = 0; i < argsNames.Count; i++)
            {
                functionTypes.Add(innerScope.GetVarType(argsNames[i]));
            }
            functionTypes.Add(bodyType);
            fsType functionType = fsType.GetFunctionType(functionTypes);

            enteredFunctionsNames.RemoveAt(enteredFunctionsNames.Count - 1);
            return functionType;
        }

        private IfsType InferValueDefnType(ITree node, fsScope scope)
        {
            derFunction = null;
            IfsType bodyType = Analyse(GetChildByType(node, fsharp_ssParser.BODY), scope);

            ITree annotatedReturningTypeNode = GetChildByType(node, fsharp_ssParser.TYPE);
            if (annotatedReturningTypeNode != null && annotatedReturningTypeNode.ChildCount > 0)
            {
                IfsType annotatedReturningType = new fsType(annotatedReturningTypeNode.GetChild(0).Text, null);
                UnifyWrapper(ref bodyType, ref annotatedReturningType, scope);
            }

            if (bodyType.Name == "function")
            {
                node.AddChild(new fsTreeNode(derFunction));
                varDerFuncTable.Add(GetChildByType(node, fsharp_ssParser.NAME).GetChild(0).Text, derFunction);
            }

            return bodyType;
        }

        private IfsType InferBodyType(ITree node, fsScope scope)
        {
            IfsType exprType = null;

            for (int i = 0; i < node.GetChild(0).ChildCount; i++)
            {
                ITree childNode = node.GetChild(0).Text == "elif" ? node.GetChild(0) : node.GetChild(0).GetChild(i);
                ITree funcNameNode = null;
                bool isLambda = false;

                if (childNode.Type == fsharp_ssParser.FUNCTION_DEFN)
                {
                    funcNameNode = GetChildByType(childNode, fsharp_ssParser.NAME);
                    if (funcNameNode == null)
                    {
                        isLambda = true;
                        funcNameNode = new fsTreeNode("NAME", fsharp_ssParser.NAME);
                        funcNameNode.AddChild(new fsTreeNode(GetUniqueFunctionName()));
                        childNode.AddChild(funcNameNode);
                    }
                }

                exprType = Analyse(childNode, scope);

                if (childNode.Type == fsharp_ssParser.FUNCTION_DEFN)
                {
                    scope.AddFunction(funcNameNode.GetChild(0).Text, exprType);
                    scope.SetVarInfo(funcNameNode.GetChild(0).Text, ScopePositionType.functionClass);

                    if (isLambda)
                    {
                        fsTreeNode lambdaCall = new fsTreeNode("FUNCTION_CALL", fsharp_ssParser.FUNCTION_CALL);
                        fsTreeNode lambdaCallArgs = new fsTreeNode("ARGS", fsharp_ssParser.ARGS);
                        lambdaCall.AddChild(funcNameNode);
                        lambdaCall.AddChild(lambdaCallArgs);
                        node.GetChild(0).AddChild(lambdaCall);
                    }
                }
                else if (childNode.Type == fsharp_ssParser.VALUE_DEFN)
                {
                    string varName = GetChildByType(childNode, fsharp_ssParser.NAME).GetChild(0).Text;
                    scope.AddVar(varName, exprType);
                    scope.SetVarInfo(varName, ScopePositionType.local);
                }
            }

            if (exprType == null)
            {
                throw new Exception("Cannot recognize body type");
            }

            return exprType;
        }

        private IfsType InferFuncCallType(ITree node, fsScope scope)
        {

            List<IfsType> factualArgsTypes = new List<IfsType>();

            string callFunctionName = GetChildByType(node, fsharp_ssParser.NAME).GetChild(0).Text;
            ITree args = GetChildByType(node, fsharp_ssParser.ARGS);

            for (int i = 0; i < args.ChildCount; i++)
            {
                ITree currentArg = args.GetChild(i);
                IfsType argType = Analyse(currentArg, scope);

                //if (argType is fsTypeVar)
                //{
                //    if (!(currentArg is fsTreeNode))
                //    {
                //        currentArg = new fsTreeNode(argType);
                //    }
                //    fsScope.ScopeVarOrFuncTypeChanged += (args.GetChild(i) as fsTreeNode).ScopeVarOrFuncTypeChangedHandler;
                //}

                factualArgsTypes.Add(argType);
            }

            if (scope.GetVarInfo(callFunctionName, false).PositionInScopeType == ScopePositionType.functionArg)
            {
                fsTreeNode typeFANode = new fsTreeNode(scope.GetVarInfo(callFunctionName, false));
                node.AddChild(typeFANode);
                List<string> argsNames = new List<string>();
                for (int i = 0; i < args.ChildCount; i++)
                {
                    argsNames.Add(args.GetChild(i).Text);
                }
                fsDerFuncInfo faDerFuncInfo = new fsDerFuncInfo(callFunctionName, argsNames, 
                    0, argsNames.Count, null);
                node.AddChild(new fsTreeNode(faDerFuncInfo));
                return scope.GetVarType(callFunctionName);
            }

            if (callFunctionName.Length >= "printf".Length &&
                callFunctionName.Substring(0, "printf".Length) == "printf")
            {
                callFunctionName += $"_{uniqueFuctionId++}";
                functionsInfos.Add(callFunctionName, functionsInfos["printf"]);
                scope.AddFunction(callFunctionName, fsType.GetFunctionType(factualArgsTypes));
                scope.SetVarInfo(callFunctionName, ScopePositionType.functionClass);
            }

            IfsType formalFunctionType = scope.GetFunctionType(callFunctionName);
            if (formalFunctionType == null)
            {
                formalFunctionType = scope.GetVarType(callFunctionName);
            }

            if (formalFunctionType == null)
            {
                throw new Exception($"Undeclared function: {callFunctionName}");
            }

            IfsType returningType = (formalFunctionType as fsType).Types[(formalFunctionType as fsType).Types.Count - 1];
            factualArgsTypes.Add(returningType);
            IfsType factualFunctionType = fsType.GetFunctionType(factualArgsTypes);

            UnifyWrapper(ref factualFunctionType, ref formalFunctionType, scope);
            returningType = (formalFunctionType as fsType).Types[(formalFunctionType as fsType).Types.Count - 1];

            fsDerFuncInfo oldFuncInfo;
            if (functionsInfos.ContainsKey(callFunctionName))
            {
                oldFuncInfo = functionsInfos[callFunctionName];
            }
            else
            {
                oldFuncInfo = varDerFuncTable[callFunctionName];
            }

            derFunction = new fsDerFuncInfo(oldFuncInfo.Name,
                                            new List<string>(oldFuncInfo.ArgsNames),
                                            oldFuncInfo.BeforePassedArgsNum,
                                            oldFuncInfo.AfterPassedArgsNum,
                                            oldFuncInfo.ContextFuncName);

            if (callFunctionName.Length < "printf".Length ||
                callFunctionName.Substring(0, "printf".Length) != "printf")
            {
                derFunction.BeforePassedArgsNum = derFunction.AfterPassedArgsNum;
                derFunction.AfterPassedArgsNum += args.ChildCount;
            }

            node.AddChild(new fsTreeNode(derFunction));

            fsTreeNode typeNode = new fsTreeNode(scope.GetVarInfo(callFunctionName, false));
            node.AddChild(typeNode);            

            return returningType;
        }

        private IfsType InferIfClauseType(ITree node, fsScope scope)
        {
            ITree logicExprNode = node.GetChild(0);
            Analyse(logicExprNode, scope);

            IfsType previousExprBlockType = Analyse(node.GetChild(1), scope);
            for (int i = 2; i < node.ChildCount; i++)
            {
                IfsType currentExprBlockType = Analyse(node.GetChild(i), scope);
                UnifyWrapper(ref previousExprBlockType, ref currentExprBlockType, scope);
            }

            return previousExprBlockType;
        }

        private IfsType InferElifClauseType(ITree node, fsScope scope)
        {
            ITree logicExprNode = node.GetChild(0);
            Analyse(logicExprNode, scope);
            return Analyse(node.GetChild(1).GetChild(0), scope);
        }

        private IfsType InferBinaryOpType(ITree node, fsScope scope, IfsType availableType)
        {
            IfsType leftOperandType = Analyse(node.GetChild(0), scope);
            IfsType rightOperandType = Analyse(node.GetChild(1), scope);

            UnifyWrapper(ref leftOperandType, ref availableType, scope);
            UnifyWrapper(ref rightOperandType, ref availableType, scope);
            UnifyWrapper(ref leftOperandType, ref rightOperandType, scope);

            return leftOperandType;
        }

        private IfsType InferPlusType(ITree node, fsScope scope)
        {
            IfsType availablePlusType = fsType.GetCompositeType(
                new List<IfsType>() {
                                        fsType.GetIntType(),
                                        fsType.GetDoubleType(),
                                        fsType.GetStringType(),
                                        fsType.GetCharType()
                                    }
                                                               );

            return InferBinaryOpType(node, scope, availablePlusType);
        }

        private IfsType InferMinusType(ITree node, fsScope scope)
        {
            IfsType availableMinusType = fsType.GetCompositeType(
                new List<IfsType>() {
                                        fsType.GetIntType(),
                                        fsType.GetDoubleType()
                                    }
                                                               );

            return InferBinaryOpType(node, scope, availableMinusType);
        }

        private IfsType InferMultType(ITree node, fsScope scope)
        {
            IfsType availableMultType = fsType.GetCompositeType(
                new List<IfsType>() {
                                        fsType.GetIntType(),
                                        fsType.GetDoubleType()
                                    }
                                                               );

            return InferBinaryOpType(node, scope, availableMultType);
        }

        private IfsType InferDivideType(ITree node, fsScope scope)
        {
            IfsType availableDivideType = fsType.GetCompositeType(
                new List<IfsType>() {
                                        fsType.GetIntType(),
                                        fsType.GetDoubleType()
                                    }
                                                               );

            return InferBinaryOpType(node, scope, availableDivideType);
        }

        private IfsType InferEqNeqOperType(ITree node, fsScope scope)
        {
            IfsType availableEqNeqType = fsType.GetCompositeType(
                new List<IfsType>() {
                                        fsType.GetIntType(),
                                        fsType.GetDoubleType(),
                                        fsType.GetStringType(),
                                        fsType.GetCharType()
                                    }
                                                               );

            InferBinaryOpType(node, scope, availableEqNeqType);
            return fsType.GetBoolType();
        }

        private IfsType InferCompareOperType(ITree node, fsScope scope)
        {
            IfsType availableEqNeqType = fsType.GetCompositeType(
                new List<IfsType>() {
                                        fsType.GetIntType(),
                                        fsType.GetDoubleType()
                                    }
                                                               );

            InferBinaryOpType(node, scope, availableEqNeqType);
            return fsType.GetBoolType();
        }

        private IfsType InferIDType(ITree node, fsScope scope)
        {           
            IfsType type = scope.GetFunctionType(node.Text) ?? scope.GetVarType(node.Text);
            if (type == null)
            {
                throw new Exception($"Undeclared variable: {node.Text}");
            }

            if (type is fsTypeVar)
            {
                (type as fsTypeVar).IsFromScope = true;
                (type as fsTypeVar).ScopeVarName = node.Text;
            }

            if (GetChildByType(node, VAR_INFO) == null)
            {
                fsTreeNode typeNode = new fsTreeNode(scope.GetVarInfo(node.Text, false));
                node.AddChild(typeNode);
            }

            return type;
        }

        private IfsType InferIntType(ITree node, fsScope scope)
        {
            return fsType.GetIntType();
        }

        private IfsType InferDoubleType(ITree node, fsScope scope)
        {
            return fsType.GetDoubleType();
        }

        private IfsType InferStringType(ITree node, fsScope scope)
        {
            return fsType.GetStringType();
        }

        private IfsType InferCharType(ITree node, fsScope scope)
        {
            return fsType.GetCharType();
        }

        private IfsType InferBoolType(ITree node, fsScope scope)
        {
            return fsType.GetBoolType();
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

        private int GetChildIndexByType(ITree parent, int type)
        {
            for (int i = 0; i < parent.ChildCount; i++)
            {
                if (parent.GetChild(i).Type == type)
                {
                    return i;
                }
            }

            return -1;
        }

        private string GetUniqueFunctionName()
        {
            return $"$lambda_{this.uniqueFuctionId}";
        }
    }
}
