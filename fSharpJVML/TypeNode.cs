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
    class TypeNode : CommonTree
    {
        public TypeNode(string text)
            : base(new CommonToken(fsharp_ssParser.TYPE, text))
        {
            this.Text = text;
        }

        public TypeNode(IfsType type)
            :base(new CommonToken(fsharp_ssParser.TYPE, type.Name))
        {
            this.Text = type.Name;

            fsType t;
            if ((t = type as fsType)?.Types != null)
            {
                for (int i = 0; i < t.Types.Count; i++)
                {
                    AddChild(new TypeNode(t.Types[i]));
                }
            }
        }
        
        public void ScopeVarOrFuncTypeChangedHandler(IfsType oldType, IfsType newType)
        {
            if (oldType.Name != this.Text)
                return;

            this.Text = newType.Name;

            fsType t;
            if ((t = newType as fsType)?.Types != null)
            {
                for (int i = 0; i < t.Types.Count; i++)
                {
                    AddChild(new TypeNode(t.Types[i]));
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
            return Text;
        }
    }
}
