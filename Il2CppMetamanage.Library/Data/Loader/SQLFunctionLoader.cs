using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Il2CppMetamanage.Library.Data.Model;
using Microsoft.Data.Sqlite;

namespace Il2CppMetamanage.Library.Data.Loader
{
    public class SQLFunctionLoader : SQLEntryLoader<SQLCppFunction>
    {
        public SQLFunctionLoader() : base("CppFunctions") 
        {
            _selectSQL = @"
                SELECT * FROM (SELECT func.id as [id], func.name as [name], func.isDefault, func.address, func.functionTypeId, funcType.returnId 
                FROM CppFunctions AS func 
                LEFT JOIN CppFunctionTypes AS funcType ON func.functionTypeId = funcType.id)";
        }

        public override SQLCppFunction ReadElement(SqliteDataReader reader)
        {
            var id = reader.GetInt32(0);
            var name = reader.GetString(1);
            var isDefault = reader.GetInt32(2) > 0;
            var address = reader.IsDBNull(3) ? 0 : reader.GetInt32(3);
            var functionTypeId = reader.GetInt32(4);
            var returnId = reader.GetInt32(5);

            var functionType = new SQLCppFunctionType(functionTypeId, returnId);

            return new SQLCppFunction(id, name, isDefault, address, functionType);
        }
    }
}
