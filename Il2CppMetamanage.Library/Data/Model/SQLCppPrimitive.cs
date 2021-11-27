using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Il2CppMetamanage.Library.Data.Model
{
    public class SQLCppPrimitive : SQLNamedEntry
    {

        public static SQLCppPrimitive GetPrimitive(int id) 
        {
            if (primitivesData.Count == 0)
                FillPrimitiveData();
            return primitivesData[id];
        }
        private static Dictionary<int, SQLCppPrimitive> primitivesData = new Dictionary<int, SQLCppPrimitive>();

        private SQLCppPrimitive(int id, string name) : base(id, name, true ) {
            TypeKind = SQLCppTypeKind.Primitive;
        }

        private static void FillPrimitiveData()
        {
            primitivesData.Add(1, new SQLCppPrimitive(1, "void"));
            primitivesData.Add(2, new SQLCppPrimitive(2, "wchar"));
            primitivesData.Add(3, new SQLCppPrimitive(3, "char"));
            primitivesData.Add(4, new SQLCppPrimitive(4, "short"));
            primitivesData.Add(5, new SQLCppPrimitive(5, "int"));
            primitivesData.Add(6, new SQLCppPrimitive(6, "long"));
            primitivesData.Add(7, new SQLCppPrimitive(7, "long long"));
            primitivesData.Add(8, new SQLCppPrimitive(8, "unsigned char"));
            primitivesData.Add(9, new SQLCppPrimitive(9, "unsigned short"));
            primitivesData.Add(10, new SQLCppPrimitive(10, "unsigned int"));
            primitivesData.Add(11, new SQLCppPrimitive(11, "unsigned long long"));
            primitivesData.Add(12, new SQLCppPrimitive(12, "float"));
            primitivesData.Add(13, new SQLCppPrimitive(13, "double"));
            primitivesData.Add(14, new SQLCppPrimitive(14, "long double"));
            primitivesData.Add(15, new SQLCppPrimitive(115, "bool"));
        }
    }
}
