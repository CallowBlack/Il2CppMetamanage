using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Il2CppMetamanage.Library.Data.Model
{
    public class SQLEntry
    {
        public int Id { get; }

        public SQLCppTypeKind TypeKind { get; protected set; }

        protected SQLEntry(int id)
        {
            Id = id;
        }
    }
}
