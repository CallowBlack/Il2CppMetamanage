using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Il2CppMetamanage.Library.Data.Model
{
    public class SQLNamedEntry : SQLEntry
    {
        public string Name { get; protected set; }
        
        public bool IsDefault { get; protected set; }

        public SQLNamedEntry(int id) : base(id) { }

        public SQLNamedEntry(int id, string name, bool isDefault) : base(id) {
            Name = name;
            IsDefault = isDefault;
        }
    }
}
