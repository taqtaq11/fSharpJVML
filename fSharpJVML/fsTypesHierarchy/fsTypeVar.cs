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

        public IfsType Instance
        {
            get;
            set;
        }

        static private string GenerateNewName()
        {
            string name = nameChar.ToString() + '\'';
            nameChar++;
            return name;
        }
    }
}
