using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Il2CppMetamanage.Library.Data.Model;

namespace Il2CppMetamanage.Library.Data.Loader
{
    public class SQLClassLoader : SQLEntryLoader<SQLCppClass>
    {
        public SQLClassLoader() : base(SQLLoadClasses) { }

        private static void SQLLoadClasses(Dictionary<int, SQLEntryPromise> promises)
        {
            using var reader = SQLDataManager.GetDataByIds(promises.Keys, "CppClasses", new string[] { "id", "name", "isDefault", "align", "kind" });

            while (reader.Read())
            {
                var id = reader.GetInt32(0);
                var name = reader.GetString(1);
                var isDefault = reader.GetInt32(2) > 0;
                var align = reader.GetInt32(3);
                var kind = (CppAst.CppClassKind)reader.GetInt32(4);

                var promise = promises[id];
                promise.Value = new SQLCppClass(id, name, isDefault, align, kind);
            }
        }
    }
}
