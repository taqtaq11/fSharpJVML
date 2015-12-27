using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace fSharpJVML
{
    class fsTypeVar : IfsType
    {
        static private char nameChar = 'a';

        public fsTypeVar()
        {
            Name = GenerateNewName();
        }

        public string Name
        {
            get;
            set;
        }

        public string ScopeVarName
        {
            get;
            set;
        }

        public bool IsFromScope
        {
            get;
            set;
        }

        public IfsType Instance
        {
            get;
            set;
        }

        public IfsType Prune
        {
            get
            {
                IfsType pruned = this;
                while ((pruned as fsTypeVar)?.Instance != null)
                {
                    pruned = (pruned as fsTypeVar).Instance;
                }
                return pruned;
            }
        }

        static private string GenerateNewName()
        {
            string name = nameChar.ToString() + '\'';
            nameChar++;
            return name;
        }

        public override bool Equals(object obj)
        {
            fsTypeVar typeVar = obj as fsTypeVar;
            if (typeVar == null)
            {
                return false;
            }

            return this.Name == typeVar.Name;
        }

        public override string ToString()
        {
            string outputString = Name;

            IfsType prunedType = Prune;

            if (prunedType is fsType && (prunedType as fsType).Name == "composite")
            {
                outputString += " case of {";
                foreach (var type in (prunedType as fsType).Types)
                {
                    outputString += " " + type.ToString() + "; ";
                }
                outputString += "}";
            }

            return outputString;
        }
    }
}
