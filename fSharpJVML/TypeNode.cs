using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Antlr.Runtime.Tree;

namespace fSharpJVML
{
    class TypeNode : ITree
    {
        private List<ITree> children;
        private ITree parent;
        private string text;


        public TypeNode(string text)
        {
            this.text = text;
        }

        public TypeNode(IfsType type)
        {
            this.text = type.Name;

            fsType t;
            if ((t = type as fsType)?.Types != null)
            {
                for (int i = 0; i < t.Types.Count; i++)
                {
                    AddChild(new TypeNode(t.Types[i]));
                }
            }
        }
        
        public int CharPositionInLine
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public int ChildCount
        {
            get
            {
                return children.Count;
            }
        }

        public int ChildIndex
        {
            get
            {
                throw new NotImplementedException();
            }

            set
            {
                throw new NotImplementedException();
            }
        }

        public bool IsNil
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public int Line
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public ITree Parent
        {
            get
            {
                return parent;
            }

            set
            {
                parent = value;
            }
        }

        public string Text
        {
            get
            {
                return text;
            }

            set
            {
                text = value;
            }
        }

        public int TokenStartIndex
        {
            get
            {
                throw new NotImplementedException();
            }

            set
            {
                throw new NotImplementedException();
            }
        }

        public int TokenStopIndex
        {
            get
            {
                throw new NotImplementedException();
            }

            set
            {
                throw new NotImplementedException();
            }
        }

        public int Type
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public void AddChild(ITree t)
        {
            children = children ?? new List<ITree>();
            children.Add(t);
        }

        public object DeleteChild(int i)
        {
            children.RemoveAt(i);
            return children;
        }

        public ITree DupNode()
        {
            return (ITree)this.MemberwiseClone();
        }

        public void FreshenParentAndChildIndexes()
        {
            throw new NotImplementedException();
        }

        public ITree GetAncestor(int ttype)
        {
            throw new NotImplementedException();
        }

        public IList<ITree> GetAncestors()
        {
            throw new NotImplementedException();
        }

        public ITree GetChild(int i)
        {
            return children[i];
        }

        public bool HasAncestor(int ttype)
        {
            return parent != null;
        }

        public void ReplaceChildren(int startChildIndex, int stopChildIndex, object t)
        {
            throw new NotImplementedException();
        }

        public void SetChild(int i, ITree t)
        {
            children[i] = t;
        }

        public string ToStringTree()
        {
            return text;
        }

        public override string ToString()
        {
            return text;
        }
    }
}
