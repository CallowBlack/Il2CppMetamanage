using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Il2CppMetamanage.Library.Data.Model
{
    public class SQLCppEnum : SQLNamedEntry
    {
        public Dictionary<string, long> Values { get => _values.Item; set => _values.Item = value; }

        private readonly SQLObject<Dictionary<string, long>> _values;

        //public SQLCppEnum(SQLDataManager manager, int id) : base(manager, id)
        //{
        //    var reader = SQLLoadDataById("CppEnums", new string[] { "name", "isDefault" });

        //    Name = reader.GetString(0);
        //    IsDefault = reader.GetBoolean(1);

        //    TypeKind = SQLCppTypeKind.Enum;
        //    _values = new (SQLLoadValues);
        //}

        public SQLCppEnum(int id, string name, bool isDefault) : base(id)
        {
            Name = name;
            IsDefault = isDefault;

            TypeKind = SQLCppTypeKind.Enum;
            _values = new(SQLLoadValues);
        }

        private Dictionary<string, long> SQLLoadValues()
        {
            var connection = SQLDataManager.Connection;
            var command = connection.CreateCommand();
            command.CommandText = @"SELECT name, value FROM CppEnumValues WHERE ownerId = $ownerId";

            var ownerIdParam = SQLDataManager.CreateParameter(command, "ownerId");
            ownerIdParam.Value = Id;

            var values = new Dictionary<string, long>();
            using (var reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    var name = reader.GetString(0);
                    var value = reader.GetInt64(1);

                    values.Add(name, value);
                }
            }
            return values;
        }

        public override string ToString()
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendLine($"enum class {Name} : int32_t {{");
            foreach (var value in Values)
            {
                builder.AppendLine($"\t{value.Key} = {value.Value},");
            }
            builder.AppendLine("};");
            return builder.ToString();
        }
    }
}
