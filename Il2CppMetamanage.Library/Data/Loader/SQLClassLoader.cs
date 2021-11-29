using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Il2CppMetamanage.Library.Data.Model;
using Microsoft.Data.Sqlite;

namespace Il2CppMetamanage.Library.Data.Loader
{
    public class SQLClassLoader : SQLEntryLoader<SQLCppClass>
    {
        public SQLClassLoader() : base("CppClasses") { }

        public override SQLCppClass ReadElement(SqliteDataReader reader)
        {
            var id = reader.GetInt32(0);
            var name = reader.GetString(1);
            var isDefault = reader.GetInt32(2) > 0;
            var align = reader.GetInt32(3);
            var kind = (CppAst.CppClassKind)reader.GetInt32(4);

            return new SQLCppClass(id, name, isDefault, align, kind);
        }
    }
}
