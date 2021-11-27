using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Il2CppMetamanage.Library.Data.Model
{
    public enum SQLCppTypeKind
    {
        None,
        Primitive,
        Class,
        Enum,
        Typedef,
        Field,
        FunctionType,
        Function
    }
}
