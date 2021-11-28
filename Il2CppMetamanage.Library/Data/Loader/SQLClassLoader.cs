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
        public SQLClassLoader() : base(SQLLoadClasses) { }

        private static void SQLLoadClasses(Dictionary<int, SQLEntryPromise> promises)
        {
            using var reader = SQLDataManager.GetDataByIds(promises.Keys, "CppClasses", new string[] { "id", "name", "isDefault", "align", "kind" });

            while (reader.Read())
            {
                var element = ReadElement(reader);
                var promise = promises[element.Id];
                promise.Value = element;
            }
        }

        private static SQLCppClass ReadElement(SqliteDataReader reader)
        {
            var id = reader.GetInt32(0);
            var name = reader.GetString(1);
            var isDefault = reader.GetInt32(2) > 0;
            var align = reader.GetInt32(3);
            var kind = (CppAst.CppClassKind)reader.GetInt32(4);

            return new SQLCppClass(id, name, isDefault, align, kind);
        }

        protected override int GetCount()
        {
            return SQLDataManager.GetCountTableElements("CppClasses");
        }

        public override List<SQLCppClass> GetNextElements(int id, int count)
        {
            var command = SQLDataManager.Connection.CreateCommand();
            command.CommandText = $"SELECT id, name, isDefault, align, kind FROM CppClasses WHERE id > {id} LIMIT {count};";
            using var reader = command.ExecuteReader();

            var elements = new List<SQLCppClass>();
            while (reader.Read())
            {
                var element = ReadElement(reader);
                elements.Add(element);
                Add(element);
            }
            return elements;
        }
    }
}
