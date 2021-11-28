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
        public SQLTypedefLoader() : base(SQLLoadTypedefs) { }

        private static void SQLLoadTypedefs(Dictionary<int, SQLEntryPromise> promises)
        {
            using var reader = SQLDataManager.GetDataByIds(promises.Keys, "CppTypedefs", new string[] { "id", "name", "isDefault", "elementId" });

            while (reader.Read())
            {
                var element = ReadElement(reader);
                var promise = promises[element.Id];
                promise.Value = element;
            }
        }

        private static SQLCppTypedef ReadElement(SqliteDataReader reader)
        {
            var id = reader.GetInt32(0);
            var name = reader.GetString(1);
            var isDefault = reader.GetInt32(2) > 0;
            var elementId = reader.GetInt32(3);

            return new SQLCppTypedef(id, name, isDefault, elementId);
        }

        protected override int GetCount()
        {
            return SQLDataManager.GetCountTableElements("CppTypedefs");
        }

        public override List<SQLCppTypedef> GetNextElements(int id, int count)
        {
            var command = SQLDataManager.Connection.CreateCommand();
            command.CommandText = $"SELECT id, name, isDefault, elementId FROM CppTypedefs WHERE id > {id} LIMIT {count};";
            using var reader = command.ExecuteReader();

            var elements = new List<SQLCppTypedef>();
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
