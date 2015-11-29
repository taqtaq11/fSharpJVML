using System.Collections.Generic;
using System;

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
            varsTypes = new Dictionary<string, IfsType>();
            functionsTypes = new Dictionary<string, IfsType>();
        }

        public IfsType GetVarType(string varName)
        {
            if (varsTypes.ContainsKey(varName))
                return varsTypes[varName];

            if (parent == null)
            {
                return null;
            }

            return parent.GetVarType(varName);
        }

        public IfsType GetFunctionType(string functionName)
        {
            if (functionsTypes.ContainsKey(functionName))
                return functionsTypes[functionName];

            if (parent == null)
            {
                return null;
            }

            return parent.GetFunctionType(functionName);
        }

        public void AddVar(string varName, IfsType varType)
        {
            if (varsTypes.ContainsKey(varName))
            {
                throw new Exception($"Variable {varName} already exists in current scope");
            }

            varsTypes.Add(varName, varType);
        }

        public void AddFunction(string functionName, IfsType functionType)
        {
            if (functionsTypes.ContainsKey(functionName))
            {
                throw new Exception($"Function {functionName} already exists in current scope");
            }

            functionsTypes.Add(functionName, functionType);
        }

        public void ChangeVarType(string varName, IfsType varType)
        {
            if (varsTypes.ContainsKey(varName))
            {
                varsTypes[varName] = varType;
            }
            else if(parent != null)
            {
                parent.ChangeVarType(varName, varType);
            }
        }

        public int VarsTypesCount
        {
            get
            {
                return varsTypes.Count;
            }
        }

        public int FunctionsTypesCount
        {
            get
            {
                return functionsTypes.Count;
            }
        }
    }
}