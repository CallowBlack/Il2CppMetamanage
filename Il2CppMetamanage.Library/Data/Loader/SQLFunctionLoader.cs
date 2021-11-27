using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Il2CppMetamanage.Library.Data.Model;

namespace Il2CppMetamanage.Library.Data.Loader
{
    public class SQLFunctionLoader : SQLEntryLoader<SQLCppFunction>
    {
        public SQLFunctionLoader() : base(SQLLoadFunctions) { }

        private static void SQLLoadFunctions(Dictionary<int, SQLEntryPromise> promises)
        {
            var command = SQLDataManager.Connection.CreateCommand();
            command.CommandText = @$"
                SELECT func.id, func.name, func.isDefault, func.address, func.functionTypeId, funcType.returnId 
                FROM CppFunctions AS func 
                LEFT JOIN CppFunctionTypes AS funcType ON func.functionTypeId = funcType.id 
                WHERE func.id IN ({string.Join(',', promises.Keys)});
            ";

            using var reader = command.ExecuteReader();

            while (reader.Read())
            {
                var id = reader.GetInt32(0);
                var name = reader.GetString(1);
                var isDefault = reader.GetInt32(2) > 0;
                var address = reader.GetInt32(3);
                var functionTypeId = reader.GetInt32(4);
                var returnId = reader.GetInt32(5);

                var functionType = new SQLCppFunctionType(functionTypeId, returnId);
                var promise = promises[id];
                promise.Value = new SQLCppFunction(id, name, isDefault, address, functionType);
            }
        }
    }
}
