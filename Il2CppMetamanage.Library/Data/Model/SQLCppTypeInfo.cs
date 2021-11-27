using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Il2CppMetamanage.Library.Data.Model
{
    public class SQLCppTypeInfo : SQLEntry
    {
        public SQLEntry Entry { get; set; }
        
        public int ArraySize { get; }
        
        public int PointerLevel { get; }

        private readonly CppAst.CppTypeKind _typeKind;

        public SQLCppTypeInfo(int id, CppAst.CppTypeKind typeKind, int pointerLevel, int arraySize) : base(id)
        {
            _typeKind = typeKind;

            PointerLevel = pointerLevel;
            ArraySize = arraySize;
        }

        public override string ToString()
        {
            string entrySignature;
            if (Entry.TypeKind == SQLCppTypeKind.FunctionType)
                entrySignature = ((SQLCppFunctionType)Entry).ToString();
            else
                entrySignature = ((SQLNamedEntry)Entry).Name;

            var pointerSignature = PointerLevel > 0 ? new string('*', PointerLevel) : "";
            var arraySignature = ArraySize > 1 ? $"[{ArraySize}]" : "";
            return $"{entrySignature}{pointerSignature}{arraySignature}";
        }
    }
}
