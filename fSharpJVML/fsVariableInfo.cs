using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace fSharpJVML
{
    public enum ScopePositionType
    {
        nil,
        functionArg,
        local,
        outer
    }

    public class fsVariableInfo
    {
        public ScopePositionType PositionInScopeType { get; set; }
        public int NumberInScopePositionTypes { get; set; }

        public ScopePositionType PositionInParentScopeType { get; set; } = ScopePositionType.nil;
        public int ScopeNestingDepth { get; set; }

        public fsVariableInfo(ScopePositionType posType, int numberInScopePosTypes)
        {
            PositionInScopeType = posType;
            NumberInScopePositionTypes = numberInScopePosTypes;
        }

        public fsVariableInfo(ScopePositionType posType, int numberInScopePosTypes, 
            ScopePositionType positionInParentScopeType, int scopeNestingDepth)
        {
            PositionInScopeType = posType;
            NumberInScopePositionTypes = numberInScopePosTypes;
            PositionInParentScopeType = positionInParentScopeType;
            ScopeNestingDepth = scopeNestingDepth;
        }

        public override string ToString()
        {
            string outerStr = "INSCOPE TYPE: " + PositionInScopeType.ToString("F") +
                    "; POS: " + NumberInScopePositionTypes.ToString();

            if (PositionInParentScopeType != ScopePositionType.nil)
            {
                outerStr += "; TYPE IN PARENT SCOPE: " + PositionInParentScopeType.ToString("F") +
                    "; NESTING DEPTH: " + ScopeNestingDepth;
            }

            return outerStr;
        }
    }
}
