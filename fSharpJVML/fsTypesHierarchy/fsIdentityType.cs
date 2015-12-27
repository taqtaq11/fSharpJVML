using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace fSharpJVML
{
    class fsIdentityType : fsType
    {
        protected fsScope scope;
        protected List<string> argsNames;

        public fsIdentityType(fsScope scope, List<string> argsNames)
            : base("identity", null)
        {
            this.scope = scope;
            this.argsNames = argsNames;
        }

        private List<IfsType> GetArgsTypes(List<string> argsNames)
        {
            return argsNames.Select<string, IfsType>((argName) =>
                    {
                        return scope.GetVarType(argName);
                    })      
                            .ToList();
        }

        public override List<IfsType> Types
        {
            get
            {
                return GetArgsTypes(argsNames);
            }
        }
    }
}
