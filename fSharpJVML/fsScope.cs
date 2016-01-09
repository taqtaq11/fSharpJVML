using System.Collections.Generic;
using System;

namespace fSharpJVML
{
    delegate void ScopeVarOrFuncTypeChangedDelegate(string oldTypeName, IfsType newType);

    class fsScope
    {
        private Dictionary<string, IfsType> varsTypes;
        private Dictionary<string, IfsType> functionsTypes;
        private Dictionary<string, fsVariableInfo> varsInfo;
        private fsScope parent;
        private int argNum = 0;
        private int localNum = 0;
        static private int scopeNestingIndexer = 0;

        static public event ScopeVarOrFuncTypeChangedDelegate ScopeVarOrFuncTypeChanged;

        public fsScope(fsScope parent)
        {
            this.parent = parent;
            varsTypes = new Dictionary<string, IfsType>();
            functionsTypes = new Dictionary<string, IfsType>();
            varsInfo = new Dictionary<string, fsVariableInfo>();
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
            if (varType.Name == "function")
            {
                if (functionsTypes.ContainsKey(varName))
                {
                    throw new Exception($"Function {varName} already exists in current scope");
                }

                functionsTypes.Add(varName, varType);
            }

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

        public void ChangeVarOrFuncType(string varName, string oldTypeName, IfsType newType)
        {
            ScopeVarOrFuncTypeChanged(oldTypeName, newType);
        }

        public void SetVarInfo(string varName, ScopePositionType spt)
        {
            varsInfo.Add(varName, new fsVariableInfo(spt, spt == ScopePositionType.functionArg ? argNum++ : localNum++));
        }

        public fsVariableInfo GetVarInfo(string varName, bool isFromInnerScope)
        {
            if (isFromInnerScope)
                scopeNestingIndexer++;

            if (varsInfo.ContainsKey(varName))
            {
                return varsInfo[varName];
            }

            if (parent != null)
            {
                if (isFromInnerScope)
                {
                    return parent.GetVarInfo(varName, true);
                }
                else
                {
                    fsVariableInfo vi = parent.GetVarInfo(varName, true);
                    int nestingIndBuf = scopeNestingIndexer;
                    scopeNestingIndexer = 0;
                    return new fsVariableInfo(ScopePositionType.outer, vi.NumberInScopePositionTypes,
                        vi.PositionInScopeType, nestingIndBuf);
                }               
            }

            throw new Exception($"Info for variable {varName} not found!");
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