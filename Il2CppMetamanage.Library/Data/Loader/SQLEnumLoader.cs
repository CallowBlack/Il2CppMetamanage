using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Il2CppMetamanage.Library.Data.Model;

namespace Il2CppMetamanage.Library.Data.Loader
{
    public class SQLEnumLoader : SQLEntryLoader<SQLCppEnum>
    {
        public SQLEnumLoader() : base(SQLLoadEnums) { }

        private static void SQLLoadEnums(Dictionary<int, SQLEntryPromise> promises)
        {
            using var reader = SQLDataManager.GetDataByIds(promises.Keys, "CppEnums", new string[] {"id", "name", "isDefault" });

            while (reader.Read())
            {
                var id = reader.GetInt32(0);
                var name = reader.GetString(1);
                var isDefault = reader.GetInt32(2) > 0;

                var promise = promises[id];
                promise.Value = new SQLCppEnum(id, name, isDefault);
            }
        }
    }
}
