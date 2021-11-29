using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Il2CppMetamanage.Library.Data.Model
{
    public class SQLCppFunctionType : SQLEntry
    {
        public List<SQLDataManager.NamedType> Parameters
        {
            get => _parameters.Item;
            set => _parameters.Item = value;
        }

        public SQLCppTypeInfo ReturnType
        {
            get => _returnType.Item;
            set => _returnType.Item = value;
        }

        private readonly LoadableObject<List<SQLDataManager.NamedType>> _parameters;
        private readonly LoadableObject<SQLCppTypeInfo> _returnType;
        private readonly int _returnTypeId;

        public SQLCppFunctionType(int id) : base(id)
        {
            using var reader = SQLDataManager.GetDataByIds(new int[] { id }, "CppFunctionTypes");
            reader.Read();
            _returnTypeId = reader.GetInt32(1);
            _parameters = new(SQLLoadParameters);
            _returnType = new(SQLLoadReturnType);
            TypeKind = SQLCppTypeKind.FunctionType;
        }

        public SQLCppFunctionType(int id, int returnTypeId) : base(id)
        {
            _returnTypeId = returnTypeId;
            _parameters = new(SQLLoadParameters);
            _returnType = new(SQLLoadReturnType);
            TypeKind = SQLCppTypeKind.FunctionType;
        }

        private List<SQLDataManager.NamedType> SQLLoadParameters()
        {
            var command = SQLDataManager.Connection.CreateCommand();
            command.CommandText = @"SELECT [ownerId] as id, [name], [elementId] FROM [CppFunctionParameters] WHERE [ownerId] = $ownerId";

            var parameter = SQLDataManager.CreateParameter(command, "ownerId");
            parameter.Value = Id;

            return SQLDataManager.GetLinkedTypes(command);
        }

        private SQLCppTypeInfo SQLLoadReturnType()
        {
            var command = SQLDataManager.Connection.CreateCommand();
            command.CommandText = @"SELECT 1 as id, '' as name, $returnId as elementId";

            var parameter = SQLDataManager.CreateParameter(command, "returnId");
            parameter.Value = _returnTypeId;

            return SQLDataManager.GetLinkedTypes(command)[0].typeInfo;
        }

        public override string ToString()
        {
            var parametersSignatures = new List<string>();
            foreach (var parameter in this.Parameters)
                parametersSignatures.Add($"{parameter}");

            return $"{ReturnType}()({string.Join(',', parametersSignatures)})";
        }
    }
}
