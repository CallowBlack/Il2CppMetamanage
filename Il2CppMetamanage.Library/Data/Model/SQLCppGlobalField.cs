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

        public SQLCppGlobalField(int id, string name, bool isDefault, int elementId, int address, bool isTypePtr) 
            : base(id, name, isDefault, elementId)
        {
            Address = address;
            IsTypePtr = isTypePtr;

            TypeKind = SQLCppTypeKind.Field;
        }
    }
}
