using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Il2CppMetamanage.Library.Data.Model
{
    public class SQLLinkedElement : SQLNamedEntry
    {
        public SQLCppTypeInfo Element { get => _element.Item; }

        protected int _elementId;
        protected LoadableObject<SQLCppTypeInfo> _element;

        protected SQLLinkedElement(int id) : base(id)
        {
            _element = new LoadableObject<SQLCppTypeInfo>(SQLLoadElement);
        }

        protected SQLLinkedElement(int id, string name, bool isDefault, int elementId) : base(id)
        {
            Name = name;
            IsDefault = isDefault;
            _elementId = elementId;
            _element = new LoadableObject<SQLCppTypeInfo>(SQLLoadElement);
        }

        private SQLCppTypeInfo SQLLoadElement()
        {
            var command = SQLDataManager.Connection.CreateCommand();
            command.CommandText = @"SELECT 1 as id, '' as name, $returnId as elementId";

            var parameter = SQLDataManager.CreateParameter(command, "returnId");
            parameter.Value = _elementId;

            return SQLDataManager.GetLinkedTypes(command)[0].typeInfo;
        }
    }
}
