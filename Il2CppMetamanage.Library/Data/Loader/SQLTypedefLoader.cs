using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Il2CppMetamanage.Library.Data.Model;
using Microsoft.Data.Sqlite;

namespace Il2CppMetamanage.Library.Data.Loader
{
    public class SQLTypedefLoader : SQLEntryLoader<SQLCppTypedef>
    {
        public SQLTypedefLoader() : base("CppTypedefs") { }

        public override SQLCppTypedef ReadElement(SqliteDataReader reader)
        {
            var id = reader.GetInt32(0);
            var name = reader.GetString(1);
            var isDefault = reader.GetInt32(2) > 0;
            var elementId = reader.GetInt32(3);

            return new SQLCppTypedef(id, name, isDefault, elementId);
        }
    }
}
