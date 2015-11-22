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
            inferenceFunctions.Add(fsharp_ssParser.FUNCTION_DEFN, InferFunctionDefnType);
        }

        public ITree Infer()
        {
            return tree;
        }

        private void Analyse(ITree node, TypesScope scope)
        {
            IfsType nodeType;

            if (inferenceFunctions.ContainsKey(node.Type))
            {
                nodeType = inferenceFunctions[node.Type](node, scope);
                ITree childTypeNode = null;

                for (int i = 0; i < node.ChildCount; i++)
                {
                    if (node.GetChild(i).Type == fsharp_ssParser.TYPE)
                    {
                        childTypeNode = node.GetChild(i);
                        break;
                    }
                }

                if (childTypeNode == null)
                {
                    childTypeNode = new TypeNode("TYPE");
                    node.AddChild(childTypeNode);
                }

                childTypeNode.AddChild(new TypeNode(nodeType));
            }
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

        private IfsType InferFunctionDefnType(ITree node, TypesScope scope)
        {
            ITree args = GetChildByType(node, fsharp_ssParser.TYPE);

            for (int i = 0; i < args.ChildCount; i++)
            {
                ITree arg = args.GetChild(i);

                ITree annotatedArgType = GetChildByType(arg, fsharp_ssParser.TYPE);               
            }
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
