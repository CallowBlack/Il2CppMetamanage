using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Il2CppMetamanage.Library.Data.Model
{
    public class SQLCppFunction : SQLNamedEntry
    {
        public List<SQLDataManager.NamedType> Parameters {
            get => _functionType.Parameters;
            set => _functionType.Parameters = value;
        }
        
        public SQLCppTypeInfo ReturnType {
            get => _functionType.ReturnType;
            set => _functionType.ReturnType = value; 
        }
        
        public int Address { get; }

        private readonly SQLCppFunctionType _functionType;

        public SQLCppFunction(int id, string name, bool isDefault, int address, SQLCppFunctionType functionType) : base(id, name, isDefault)
        {
            Address = address;
            _functionType = functionType;
            TypeKind = SQLCppTypeKind.Function;
        }

        public override string ToString()
        {
            return $"DO_APP_FUNC(0x{Address:08X}, {ReturnType}, {Name}, ({string.Join(',', Parameters)}));\r\n";
        }
    }
}
