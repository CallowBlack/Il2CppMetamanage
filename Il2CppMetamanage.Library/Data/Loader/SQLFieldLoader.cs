using Il2CppMetamanage.Library.Data.Model;
using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Il2CppMetamanage.Library.Data.Loader
{
    public class SQLFieldLoader : SQLEntryLoader<SQLCppGlobalField>
    {
        public SQLFieldLoader() : base("CppFields") { }

        public override SQLCppGlobalField ReadElement(SqliteDataReader reader)
        {
            var id = reader.GetInt32(0);
            var name = reader.GetString(1);
            var isDefault = reader.GetInt32(2) > 0;
            var address = reader.IsDBNull(3) ? 0 : reader.GetInt32(3);
            var elementId = reader.GetInt32(4);
            var isTypePtr = reader.GetInt32(5) > 0;

            return new SQLCppGlobalField(id, name, isDefault, elementId, address, isTypePtr);
        }
    }
}
