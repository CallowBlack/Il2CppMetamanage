using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Il2CppMetamanage.Library.Data.Model
{
    public class SQLCppGlobalField : SQLLinkedElement
    {
        public int Address { get; }

        public bool IsTypePtr { get; }

        public bool IsMetaInfo { get => !IsDefault && !IsTypePtr && Address > 0; }

        public SQLCppGlobalField(int id, string name, bool isDefault, int elementId, int address, bool isTypePtr) 
            : base(id, name, isDefault, elementId)
        {
            Address = address;
            IsTypePtr = isTypePtr;

            TypeKind = SQLCppTypeKind.Field;
        }

        public override string ToString()
        {
            var namedElement = Element.Entry as SQLNamedEntry;
            if (IsTypePtr)
                return $"DO_TYPEDEF(0x{Address:08X}, {namedElement.Name});";
            else if (IsMetaInfo)
                return $"DO_APP_FUNC_METHODINFO(0x{Address:08X}, {Name});";
            else
                return $"{Element} {Name};";
        }
    }
}
