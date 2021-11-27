using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Il2CppMetamanage.Library.Data.Model
{
    public class SQLCppClass : SQLNamedEntry
    {
        public int Align { get; }

        public bool IsInner { get; }

        public CppAst.CppClassKind Kind { get; }

        public List<SQLDataManager.NamedType> Fields { get => _fields.Item; set => _fields.Item = value; }

        private readonly SQLObject<List<SQLDataManager.NamedType>> _fields;

        public SQLCppClass(int id, string name, bool isDefault, int align, CppAst.CppClassKind kind) 
            : base(id)
        {
            IsInner = name.Contains(':');
            if (IsInner)
            {
                name = name[(name.LastIndexOf(']') + 1)..];
            }

            Name = name;
            IsDefault = isDefault;

            Align = align;
            Kind = kind;

            TypeKind = SQLCppTypeKind.Class;
            _fields = new (SQLLoadFields);
        }

        private List<SQLDataManager.NamedType> SQLLoadFields()
        {
            var command = SQLDataManager.Connection.CreateCommand();
            command.CommandText = @"SELECT [id], [name], [elementId] FROM [CppClassMembers] WHERE [ownerId] = $ownerId";

            var parameter = SQLDataManager.CreateParameter(command, "ownerId");
            parameter.Value = Id;

            return SQLDataManager.GetLinkedTypes(command);
        }

        public override string ToString()
        {
            var alignAttribute = Align > 1 ? $" __attribute__((aligned({Align})))" : "";

            StringBuilder builder = new();
            var clsName = Name != "" ? " " + Name : "";
            builder.AppendLine($"{Kind.ToString().ToLower()}{alignAttribute}{clsName} {{");
            foreach (var field in Fields)
            {
                if (field.typeInfo.Entry.TypeKind == SQLCppTypeKind.Class) {
                    var innerClass = field.typeInfo.Entry as SQLCppClass;
                    if (innerClass.IsInner)
                    {
                        var innerClassTextLines = innerClass.ToString().Split("\r\n");
                        innerClassTextLines = innerClassTextLines[..(innerClassTextLines.Length - 1)];

                        var fieldName = field.name != "" ? " " + field.name : "";
                        innerClassTextLines[innerClassTextLines.Length - 1] += $"{fieldName};";
                        foreach (var line in innerClassTextLines)
                            builder.AppendLine("\t" + line);
                        continue;
                    }
                }
                builder.AppendLine($"\t{field};");
            }
            builder.AppendLine("};");

            return builder.ToString();
        }
    }
}
