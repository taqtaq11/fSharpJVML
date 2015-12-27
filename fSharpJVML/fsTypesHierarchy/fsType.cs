using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace fSharpJVML
{
    class fsType : IfsType
    {
        public fsType(string name, List<IfsType> types)
        {
            Types = types ?? new List<IfsType>();
            Name = name;
        }

        public virtual List<IfsType> Types
        {
            get;
            private set;
        }

        public string Name
        {
            get;
            set;
        }

        public override bool Equals(object obj)
        {
            fsType type = obj as fsType;
            if (type == null)
            {
                return false;
            }

            return this.Name == type.Name;
        }

        static public fsType GetStringType()
        {
            return new fsType("string", null);
        }

        static public fsType GetCharType()
        {
            return new fsType("char", null);
        }

        static public fsType GetIntType()
        {
            return new fsType("int", null);
        }

        static public fsType GetDoubleType()
        {
            return new fsType("double", null);
        }

        static public fsType GetBoolType()
        {
            return new fsType("bool", null);
        }

        static public fsType GetFunctionType(List<IfsType> types)
        {
            return new fsType("function", types);
        }

        static public fsType GetCompositeType(List<IfsType> types)
        {
            return new fsType("composite", types);
        }

        static public fsType GetProgramType()
        {
            return new fsType("program", null);
        }

        static public fsIdentityType GetIdentityType(fsScope scope, List<string> argsNames)
        {
            return new fsIdentityType(scope, argsNames);
        }

        public override string ToString()
        {
            string outputString = Name;

            if (Types != null && Types.Count > 0)
            {
                outputString += ": ";
                for (int i = 0; i < Types.Count; i++)
                {
                    if (i > 0)
                    {
                        outputString += "->";
                    }

                    outputString += Types[i].ToString();
                }
            }

            return outputString;
        }
    }
}
