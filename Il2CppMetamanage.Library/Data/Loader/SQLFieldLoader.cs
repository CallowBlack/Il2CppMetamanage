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
        public SQLFieldLoader() : base(SQLLoadFiels) { }

        private static void SQLLoadFiels(Dictionary<int, SQLEntryPromise> promises)
        {
            using var reader = SQLDataManager.GetDataByIds(promises.Keys, "CppFields", 
                new string[] { "id", "name", "isDefault", "elementId", "isTypePtr", "address" });

            while (reader.Read())
            {
                var element = ReadElement(reader);
                var promise = promises[element.Id];
                promise.Value = element;
            }
        }

        private static SQLCppGlobalField ReadElement(SqliteDataReader reader)
        {
            var id = reader.GetInt32(0);
            var name = reader.GetString(1);
            var isDefault = reader.GetInt32(2) > 0;
            var elementId = reader.GetInt32(3);
            var isTypePtr = reader.GetInt32(4) > 0;
            var address = reader.IsDBNull(5) ? 0 : reader.GetInt32(5);

            return new SQLCppGlobalField(id, name, isDefault, elementId, address, isTypePtr);
        }

        protected override int GetCount()
        {
            return SQLDataManager.GetCountTableElements("CppFields");
        }

        public override List<SQLCppGlobalField> GetNextElements(int id, int count)
        {
            var command = SQLDataManager.Connection.CreateCommand();
            command.CommandText = $"SELECT id, name, isDefault, elementId, isTypePtr, address FROM CppFields WHERE id > {id} LIMIT {count};";
            using var reader = command.ExecuteReader();

            var elements = new List<SQLCppGlobalField>();
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
