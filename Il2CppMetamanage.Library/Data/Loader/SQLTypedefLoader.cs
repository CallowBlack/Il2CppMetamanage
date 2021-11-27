using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Il2CppMetamanage.Library.Data.Model;

namespace Il2CppMetamanage.Library.Data.Loader
{
    public class SQLTypedefLoader : SQLEntryLoader<SQLCppTypedef>
    {
        public SQLTypedefLoader() : base(SQLLoadTypedefs) { }

        private static void SQLLoadTypedefs(Dictionary<int, SQLEntryPromise> promises)
        {
            using var reader = SQLDataManager.GetDataByIds(promises.Keys, "CppTypedefs", new string[] { "id", "name", "isDefault", "elementId" });

            while (reader.Read())
            {
                var id = reader.GetInt32(0);
                var name = reader.GetString(1);
                var isDefault = reader.GetInt32(2) > 0;
                var elementId = reader.GetInt32(3);

                var promise = promises[id];
                promise.Value = new SQLCppTypedef(id, name, isDefault, elementId);
            }
        }
    }
}
