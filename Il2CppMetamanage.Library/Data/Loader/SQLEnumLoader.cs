using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Il2CppMetamanage.Library.Data.Model;
using Microsoft.Data.Sqlite;

namespace Il2CppMetamanage.Library.Data.Loader
{
    public class SQLEnumLoader : SQLEntryLoader<SQLCppEnum>
    {
        public SQLEnumLoader() : base("CppEnums") { }

        public override SQLCppEnum ReadElement(SqliteDataReader reader)
        {
            var id = reader.GetInt32(0);
            var name = reader.GetString(1);
            var isDefault = reader.GetInt32(2) > 0;

            return new SQLCppEnum(id, name, isDefault);
        }
    }
}
