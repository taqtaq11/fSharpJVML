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
    class fsTreeNode : CommonTree
    {
        public IfsType NodeType { get; set; }
        public fsDerFuncInfo DerFuncInfo { get; set; }
        public fsVariableInfo VarInfo { get; set; }

        public fsTreeNode(string text)
            : base(new CommonToken(fsharp_ssParser.TYPE, text))
        {
            this.Text = text;
        }

        public fsTreeNode(IfsType type)
            :base(new CommonToken(fsharp_ssParser.TYPE, type.Name))
        {
            this.Text = "TYPE";
            this.NodeType = type;
        }

        public fsTreeNode(fsVariableInfo vi)
            :base(new CommonToken(65, vi.PositionInScopeType.ToString()))
        {
            this.Text = "SCOPE_POSITION";
            VarInfo = vi;
        }
        
        public fsTreeNode(fsDerFuncInfo dfi)
            : base(new CommonToken(66, ""))
        {
            this.Text = "DER_FUNC_INFO";
            DerFuncInfo = dfi;
        }

        public void ScopeVarOrFuncTypeChangedHandler(string oldTypeName, IfsType newType)
        {
            if (this.Text != "TYPE" || oldTypeName != NodeType.Name)
            {
                if (NodeType.Name == "function")
                {
                    fsType nodeTyped = NodeType as fsType;
                    for (int i = 0; i < nodeTyped.Types.Count; i++)
                    {
                        if (nodeTyped.Types[i].Name == oldTypeName)
                        {
                            nodeTyped.Types[i] = newType;
                        }
                    }
                }
                return;
            }

            NodeType = newType;
        }

        public override string Text
        {
            get;
            set;
        }

        public override string ToString()
        {
            string outStr = string.Empty;

            if (NodeType != null)
            {
                outStr += "Type:: " + NodeType.ToString() + " ";
            }

            if (VarInfo != null)
            {
                outStr += "VarInfo:: " + VarInfo.ToString();
            }

            if (DerFuncInfo != null)
            {
                outStr += "DerFuncInfo:: funcName: " + DerFuncInfo.Name + " argsNames: ";
                for (int i = 0; i < DerFuncInfo.ArgsNames.Count; i++)
                {
                    outStr += DerFuncInfo.ArgsNames[i] + " ";
                }
                outStr += " before:" + DerFuncInfo.BeforePassedArgsNum;
                outStr += " after:" + DerFuncInfo.AfterPassedArgsNum;
            }

            return outStr;
        }
    }
}
