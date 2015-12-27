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
        
        public void ScopeVarOrFuncTypeChangedHandler(IfsType oldType, IfsType newType)
        {
            if (oldType.Name != this.Text)
                return;

            this.Text = newType.Name;

            if (!(newType is fsTypeVar))
            {
                for (int i = 0; i < ChildCount; i++)
                {
                    if (Children[i].Text == "GENERIC")
                    {
                        DeleteChild(i);
                        break;
                    }
                }
            }

            fsType t;
            if ((t = newType as fsType)?.Types != null)
            {
                for (int i = 0; i < t.Types.Count; i++)
                {
                    AddChild(new fsTreeNode(t.Types[i]));
                }
            }
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
            return outStr;
        }
    }
}
