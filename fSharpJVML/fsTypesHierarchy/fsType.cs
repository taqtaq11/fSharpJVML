﻿using System;
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

        public List<IfsType> Types
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
            if (obj == null)
            {
                return false;
            }

            if (this.Name == type.Name)
            {
                return true;
            }

            return false;
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
    }
}