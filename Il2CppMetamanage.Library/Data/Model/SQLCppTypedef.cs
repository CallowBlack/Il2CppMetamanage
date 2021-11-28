using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Il2CppMetamanage.Library.Data.Model
{
    public class SQLCppTypedef : SQLLinkedElement
    {
        public SQLCppTypedef(int id, string name, bool isDefault, int elementId)
            : base(id, name, isDefault, elementId) {
            TypeKind = SQLCppTypeKind.Typedef;
        }

        public override string ToString()
        {
            if (Element.Entry.TypeKind == SQLCppTypeKind.FunctionType)
            {
                var funcType = Element.Entry as SQLCppFunctionType;
                return $"typedef {funcType.ReturnType} (*{Name})({string.Join(',', funcType.Parameters)});\r\n";
            }
            else if (Element.Entry.TypeKind == SQLCppTypeKind.Typedef)
            {
                return $"typedef {Element} {Name};\r\n";
            }
            else
            {
                var elementText = Element.Entry.ToString();
                var endText = elementText.LastIndexOf(';');
                if (endText != -1)
                    elementText = elementText[..endText];
                return $"typedef {elementText} {Name};\r\n";
            }
        }
    }
}
