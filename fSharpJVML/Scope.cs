using System.Collections.Generic;

namespace fSharpJVML
{
    class TypesScope
    {
        private Dictionary<string, IfsType> varsTypes;
        private Dictionary<string, IfsType> functionsTypes;
        private TypesScope parent;

        public TypesScope(TypesScope parent)
        {
            this.parent = parent;
        }

        public IfsType GetVarType(string varName)
        {
            if (varsTypes == null)
                return null;

            if (varsTypes.ContainsKey(varName))
                return varsTypes[varName];

            return parent.GetVarType(varName);
        }

        public IfsType GetFunctionType(string functionName)
        {
            if (functionsTypes == null)
                return null;

            if (functionsTypes.ContainsKey(functionName))
                return functionsTypes[functionName];

            return parent.GetFunctionType(functionName);
        }
    }
}