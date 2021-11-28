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
        public SQLEnumLoader() : base(SQLLoadEnums) { }

        private static void SQLLoadEnums(Dictionary<int, SQLEntryPromise> promises)
        {
            using var reader = SQLDataManager.GetDataByIds(promises.Keys, "CppEnums", new string[] {"id", "name", "isDefault" });

            while (reader.Read())
            {
                var element = ReadElement(reader);
                var promise = promises[element.Id];
                promise.Value = element;
            }
        }

        private static SQLCppEnum ReadElement(SqliteDataReader reader)
        {
            var id = reader.GetInt32(0);
            var name = reader.GetString(1);
            var isDefault = reader.GetInt32(2) > 0;

            return new SQLCppEnum(id, name, isDefault);
        }

        protected override int GetCount()
        {
            return SQLDataManager.GetCountTableElements("CppEnums");
        }

        public override List<SQLCppEnum> GetNextElements(int id, int count)
        {
            var command = SQLDataManager.Connection.CreateCommand();
            command.CommandText = $"SELECT id, name, isDefault FROM CppEnums WHERE id > {id} LIMIT {count};";
            using var reader = command.ExecuteReader();

            var elements = new List<SQLCppEnum>();
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
