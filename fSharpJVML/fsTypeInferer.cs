using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Antlr.Runtime.Tree;
using fsharp_ss;

namespace fSharpJVML
{
    delegate IfsType InferNodeTypeDelegate(ITree node, TypesScope scope);

    class fsTypeInferer
    {
        private ITree tree;
        private Dictionary<int, InferNodeTypeDelegate> inferenceFunctions;


        public fsTypeInferer(ITree tree)
        {
            this.tree = tree;
            inferenceFunctions = new Dictionary<int, InferNodeTypeDelegate>();
            inferenceFunctions.Add(fsharp_ssParser.PROGRAM, InferProgramType);
            inferenceFunctions.Add(fsharp_ssParser.PLUS, InferPlusType);
            inferenceFunctions.Add(fsharp_ssParser.MINUS, InferMinusType);
            inferenceFunctions.Add(fsharp_ssParser.MULT, InferMultType);
            inferenceFunctions.Add(fsharp_ssParser.DIV, InferDivideType);
            inferenceFunctions.Add(fsharp_ssParser.ID, InferIDType);
            inferenceFunctions.Add(fsharp_ssParser.INT, InferIntType);
            inferenceFunctions.Add(fsharp_ssParser.DOUBLE, InferDoubleType);
            inferenceFunctions.Add(fsharp_ssParser.CHAR, InferCharType);
            inferenceFunctions.Add(fsharp_ssParser.TRUE, InferBoolType);
            inferenceFunctions.Add(fsharp_ssParser.FALSE, InferBoolType);
            inferenceFunctions.Add(fsharp_ssParser.STRING, InferStringType);
            inferenceFunctions.Add(fsharp_ssParser.BODY, InferBodyType);
            inferenceFunctions.Add(fsharp_ssParser.FUNCTION_DEFN, InferFunctionDefnType);
            inferenceFunctions.Add(fsharp_ssParser.FUNCTION_CALL, InferFuncCallType);
        }

        public ITree Infer()
        {
            Analyse(tree, new TypesScope(null));
            return tree;
        }

        private IfsType Analyse(ITree node, TypesScope scope)
        {
            IfsType nodeType = null;

            if (inferenceFunctions.ContainsKey(node.Type))
            {
                nodeType = inferenceFunctions[node.Type](node, scope);
                ITree childTypeNode = GetChildByType(node, fsharp_ssParser.TYPE);

                if (childTypeNode == null)
                {
                    childTypeNode = new TypeNode("TYPE");
                    node.AddChild(childTypeNode);
                }

                childTypeNode.AddChild(new TypeNode(nodeType));
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

        private void Unify(ref IfsType t1, ref IfsType t2, TypesScope scope)
        {
            bool isT1FromScope = false;
            bool isT2FromScope = false;
            string t1ScopeName = "";
            string t2ScopeName = "";

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

            Unify(ref t1, ref t2);

            if (isT1FromScope)
            {
                scope.ChangeVarType(t1ScopeName, t1);
            }

            if (isT2FromScope)
            {
                scope.ChangeVarType(t2ScopeName, t2);
            }
        }

        private void Unify(ref IfsType t1, ref IfsType t2)
        {
            t1 = Prune(t1);
            t2 = Prune(t2);

            if (t1 is fsTypeVar)
            {
                if (!t1.Equals(t2))
                {
                    if (OccursInType(t1, t2))
                    {
                        throw new Exception("Recursive unification");
                    }

                    (t1 as fsTypeVar).Instance = t2;
                }
            }
            else if(t1 is fsType && t2 is fsTypeVar)
            {
                Unify(ref t2, ref t1);
            }
            else if (t1 is fsType && t2 is fsType)
            {                
                fsType type1 = t1 as fsType;
                fsType type2 = t2 as fsType;

                if (type1.Name == "composite")
                {
                    if (type2.Name != "composite")
                    {
                        Unify(ref t2, ref t1);
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
                    if (type1.Types.Count != type2.Types.Count)
                    {
                        throw new Exception($"Cannot unify types {type1.Name} and {type2.Name}");
                    }

                    for (int i = 0; i < type1.Types.Count; i++)
                    {
                        IfsType t1Child = type1.Types[i];
                        IfsType t2Child = type2.Types[i];
                        Unify(ref t1Child, ref t2Child);
                    }
                }                
            }
            else
            {
                throw new Exception($"Cannot unify types {t1.Name} and {t2.Name}");
            }
        }

        private IfsType InferProgramType(ITree node, TypesScope scope)
        {
            if (scope == null)
            {
                scope = new TypesScope(null);
            }

            IfsType exprType;
            for (int i = 0; i < node.ChildCount; i++)
            {
                ITree childNode = node.GetChild(i);
                exprType = Analyse(childNode, scope);

                if (childNode.Type == fsharp_ssParser.FUNCTION_DEFN)
                {
                    scope.AddFunction(GetChildByType(childNode, fsharp_ssParser.NAME).GetChild(0).Text, exprType);
                }
                else if (childNode.Type == fsharp_ssParser.VALUE_DEFN)
                {
                    scope.AddVar(GetChildByType(childNode, fsharp_ssParser.NAME).GetChild(0).Text, exprType);
                }
            }

            return fsType.GetProgramType();
        }

        private IfsType InferFunctionDefnType(ITree node, TypesScope scope)
        {
            TypesScope innerScope = new TypesScope(scope);

            List<IfsType> functionTypes = new List<IfsType>();

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
            }

            IfsType bodyType = Analyse(GetChildByType(node, fsharp_ssParser.BODY), innerScope);

            ITree annotatedReturningTypeNode = GetChildByType(node, fsharp_ssParser.TYPE);
            if (annotatedReturningTypeNode != null && annotatedReturningTypeNode.ChildCount > 0)
            {
                IfsType annotatedReturningType = new fsType(annotatedReturningTypeNode.GetChild(0).Text, null);
                Unify(ref bodyType, ref annotatedReturningType, innerScope);
            }

            for (int i = 0; i < argsNames.Count; i++)
            {
                functionTypes.Add(innerScope.GetVarType(argsNames[i]));
            }
            functionTypes.Add(bodyType);
            fsType functionType = fsType.GetFunctionType(functionTypes);
            return functionType;
        }

        private IfsType InferBodyType(ITree node, TypesScope scope)
        {
            IfsType exprType = null;

            for (int i = 0; i < node.ChildCount; i++)
            {
                ITree childNode = node.GetChild(0).GetChild(i);
                exprType = Analyse(childNode, scope);

                if (childNode.Type == fsharp_ssParser.FUNCTION_DEFN)
                {
                    scope.AddFunction(GetChildByType(childNode, fsharp_ssParser.NAME).GetChild(0).Text, exprType);
                }
                else if (childNode.Type == fsharp_ssParser.VALUE_DEFN)
                {
                    scope.AddVar(GetChildByType(childNode, fsharp_ssParser.NAME).GetChild(0).Text, exprType);
                }
            }

            if (exprType == null)
            {
                throw new Exception("Cannot recognize body type");
            }

            return exprType;
        }

        private IfsType InferFuncCallType(ITree node, TypesScope scope)
        {
            List<IfsType> factualArgsTypes = new List<IfsType>();

            string callFunctionName = GetChildByType(node, fsharp_ssParser.NAME).GetChild(0).Text;
            ITree arg = GetChildByType(node, fsharp_ssParser.ARGS);

            for (int i = 0; i < arg.ChildCount; i++)
            {
                factualArgsTypes.Add(Analyse(arg.GetChild(i), scope));
            }

            IfsType formalFunctionType = scope.GetFunctionType(callFunctionName);

            if (formalFunctionType == null)
            {
                throw new Exception($"Undeclared function: {callFunctionName}");
            }

            factualArgsTypes.Add((formalFunctionType as fsType).Types[(formalFunctionType as fsType).Types.Count - 1]);
            IfsType factualFunctionType = fsType.GetFunctionType(factualArgsTypes);

            Unify(ref factualFunctionType, ref formalFunctionType);

            return factualFunctionType;
        }

        private IfsType InferIfClauseType(ITree node, TypesScope scope)
        {

        }

        private IfsType InferBinaryOpType(ITree node, TypesScope scope, IfsType availableType)
        {
            IfsType leftOperandType = Analyse(node.GetChild(0), scope);
            IfsType rightOperandType = Analyse(node.GetChild(1), scope);

            Unify(ref leftOperandType, ref availableType, scope);
            Unify(ref rightOperandType, ref availableType, scope);
            Unify(ref leftOperandType, ref rightOperandType, scope);

            return leftOperandType;
        }

        private IfsType InferPlusType(ITree node, TypesScope scope)
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

        private IfsType InferMinusType(ITree node, TypesScope scope)
        {
            IfsType availableMinusType = fsType.GetCompositeType(
                new List<IfsType>() {
                                        fsType.GetIntType(),
                                        fsType.GetDoubleType()
                                    }
                                                               );

            return InferBinaryOpType(node, scope, availableMinusType);
        }

        private IfsType InferMultType(ITree node, TypesScope scope)
        {
            IfsType availableMultType = fsType.GetCompositeType(
                new List<IfsType>() {
                                        fsType.GetIntType(),
                                        fsType.GetDoubleType()
                                    }
                                                               );

            return InferBinaryOpType(node, scope, availableMultType);
        }

        private IfsType InferDivideType(ITree node, TypesScope scope)
        {
            IfsType availableDivideType = fsType.GetCompositeType(
                new List<IfsType>() {
                                        fsType.GetIntType(),
                                        fsType.GetDoubleType()
                                    }
                                                               );

            return InferBinaryOpType(node, scope, availableDivideType);
        }

        private IfsType InferLogicOperType(ITree node, TypesScope scope)
        {
            IfsType availableLogicType = fsType.GetCompositeType(
                new List<IfsType>() {
                                        fsType.GetIntType(),
                                        fsType.GetDoubleType()
                                    }
                                                               );

            IfsType leftOperandType = Analyse(node.GetChild(0), scope);
            IfsType rightOperandType = Analyse(node.GetChild(1), scope);

            Unify(ref leftOperandType, ref availableLogicType, scope);
            Unify(ref rightOperandType, ref availableLogicType, scope);
            Unify(ref leftOperandType, ref rightOperandType, scope);

            return leftOperandType;
        }

        private IfsType InferIDType(ITree node, TypesScope scope)
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
            return type;
        }

        private IfsType InferIntType(ITree node, TypesScope scope)
        {
            return fsType.GetIntType();
        }

        private IfsType InferDoubleType(ITree node, TypesScope scope)
        {
            return fsType.GetDoubleType();
        }

        private IfsType InferStringType(ITree node, TypesScope scope)
        {
            return fsType.GetStringType();
        }

        private IfsType InferCharType(ITree node, TypesScope scope)
        {
            return fsType.GetCharType();
        }

        private IfsType InferBoolType(ITree node, TypesScope scope)
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
    }
}
