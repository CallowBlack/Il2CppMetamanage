using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.IO;
using Il2CppMetamanage.Library.Data.Model;
using Il2CppMetamanage.Library.Data.Loader;

namespace Il2CppMetamanage.Library.Data
{
    public static class SQLDataManager 
    {
        public const string ClassesTableName = "CppClasses";
        public const string TypedefsTableName = "CppTypedefs";
        public const string EnumsTableName = "CppEnums";
        public const string FunctionsTableName = "CppFunctions";

        public static SqliteConnection Connection { get; private set; }

        public static SQLClassLoader ClassLoader { get; private set; }
        public static SQLEnumLoader EnumLoader { get; private set; }
        public static SQLFunctionLoader FunctionLoader { get; private set; }
        public static SQLTypedefLoader TypedefLoader { get; private set; }
        public static SQLFieldLoader FieldLoader { get; private set; }

        public struct NamedType
        {
            public string name;
            public SQLCppTypeInfo typeInfo;

            public NamedType(string name, SQLCppTypeInfo typeInfo)
            {
                this.name = name;
                this.typeInfo = typeInfo;
            }

            public override string ToString()
            {
                return $"{typeInfo} {name}";
            }
        }

        private struct FunctionTypeWrapper
        {
            public CppAst.CppFunction function;
            public CppAst.CppFunctionType functionType;
            public bool isFunction;

            public FunctionTypeWrapper(CppAst.CppFunction function)
            {
                this.function = function;
                this.functionType = null;
                this.isFunction = true;
            }

            public FunctionTypeWrapper(CppAst.CppFunctionType functionType)
            {
                this.function = null;
                this.functionType = functionType;
                this.isFunction = false;
            }

            public CppAst.CppContainerList<CppAst.CppParameter> Parameters { get => isFunction ? function.Parameters : functionType.Parameters; }
            public CppAst.CppType ReturnType { get => isFunction ? function.ReturnType : functionType.ReturnType; }
            public CppAst.CppCallingConvention CallingConvention { get => isFunction ? function.CallingConvention : functionType.CallingConvention; }
        }

        public static SqliteParameter CreateParameter(SqliteCommand command, string name)
        {
            var param = command.CreateParameter();
            param.ParameterName = "$" + name;
            command.Parameters.Add(param);
            return param;
        }
        
        public static List<SQLEntry> GetDependencies(IEnumerable<SQLNamedEntry> targets)
        {
            HashSet<SQLEntry> typeInfoSet = new();
            List<SQLEntry> dependencies = new();
            List<SQLEntry> items = new();
            items.AddRange(targets);
            foreach (var target in targets)
                typeInfoSet.Add(target);

            while (items.Count > 0)
            {
                List<SQLEntry> childs = new();
                foreach (var item in items) {
                    switch (item.TypeKind)
                    {
                        case SQLCppTypeKind.Primitive:
                        case SQLCppTypeKind.Enum:
                            continue;
                        case SQLCppTypeKind.Class:
                            {
                                var cls = item as SQLCppClass;
                                foreach (var member in cls.Fields)
                                {
                                    if (member.typeInfo.Entry.TypeKind == SQLCppTypeKind.Class)
                                    {
                                        var childCls = member.typeInfo.Entry as SQLCppClass;
                                        if (childCls.IsInner)
                                            continue;
                                    }
                                    childs.Add(member.typeInfo.Entry);
                                }

                                var command = Connection.CreateCommand();
                                command.CommandText = @$"SELECT field.* FROM CppFields as field 
                                                    LEFT JOIN CppElements AS elem ON field.elementId = elem.id WHERE elem.classId = {cls.Id}";
                                var reader = command.ExecuteReader();
                                if (reader.Read())
                                {
                                    var field = FieldLoader.ReadElement(reader);
                                    childs.Add(field);
                                }
                            }
                            break;
                        case SQLCppTypeKind.Typedef:
                            {
                                var typedef = item as SQLCppTypedef;
                                childs.Add(typedef.Element.Entry);
                            }
                            break;
                        case SQLCppTypeKind.Function:
                            {
                                var func = item as SQLCppFunction;
                                childs.AddRange(func.Parameters.ConvertAll((it) => it.typeInfo.Entry));

                                var metaInfoField = FieldLoader[$"{func.Name}__MethodInfo"];
                                if (metaInfoField is not null)
                                    childs.Add(metaInfoField);
                            }
                            break;
                        case SQLCppTypeKind.FunctionType:
                            {
                                var funcType = item as SQLCppFunctionType;
                                childs.AddRange(funcType.Parameters.ConvertAll((it) => it.typeInfo.Entry));
                            }
                            break;
                        case SQLCppTypeKind.Field:
                            {
                                var field = item as SQLCppGlobalField;
                                childs.Add(field.Element.Entry);
                            }
                            break;
                        default:
                            throw new NotImplementedException();
                    }
                }
                items.Clear();

                foreach (var entry in childs)
                {
                    SQLNamedEntry namedEntry = entry as SQLNamedEntry;
                    if (namedEntry.IsDefault)
                        continue;

                    if (typeInfoSet.Contains(entry))
                        continue;

                    items.Add(entry);
                    typeInfoSet.Add(entry);
                    dependencies.Add(entry);
                }
            }

            return dependencies;
        }

        public static List<NamedType> GetLinkedTypes(SqliteCommand command)
        {
            command.CommandText =
                @$"
                    SELECT
                        member.name,
                        element.id,
                        element.kind,
                        element.pointerLevel,
                        element.arraySize,
                        CASE element.kind
                            WHEN 0 THEN element.primitiveId
                            WHEN 7 THEN element.classId
                            WHEN 6 THEN element.typedefId
                            WHEN 5 THEN element.functionId
                            WHEN 8 THEN element.enumId
                        END
                    FROM ({command.CommandText}) as member 
                    LEFT JOIN CppElements AS element 
                        ON member.elementId = element.id;
                ";

            var types = new List<Tuple<string, SQLCppTypeInfo, SQLEntryPromise>>();
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                var memberName = reader.GetString(0);

                var elementId = reader.GetInt32(1);
                var elementKind = (CppAst.CppTypeKind) reader.GetInt32(2);
                var pointerLevel = reader.GetInt32(3);
                var arraySize = reader.GetInt32(4);
                var typeId = reader.GetInt32(5);

                var typeInfo = new SQLCppTypeInfo(elementId, pointerLevel, arraySize);
                SQLEntryPromise promise = null;
                switch (elementKind)
                {
                    case CppAst.CppTypeKind.Primitive:
                        promise = new();
                        promise.Value = SQLCppPrimitive.GetPrimitive(typeId);
                        break;
                    case CppAst.CppTypeKind.StructOrClass:
                        promise = ClassLoader.GetPromise(typeId);
                        break;
                    case CppAst.CppTypeKind.Enum:
                        promise = EnumLoader.GetPromise(typeId);
                        break;
                    case CppAst.CppTypeKind.Function:
                        promise = new();
                        promise.Value = new SQLCppFunctionType(typeId);
                        break;
                    case CppAst.CppTypeKind.Typedef:
                        promise = TypedefLoader.GetPromise(typeId);
                        break;
                    default:
                        throw new NotImplementedException();
                }
                types.Add(new (memberName, typeInfo, promise));
            }

            ClassLoader.LoadPromised();
            TypedefLoader.LoadPromised();
            EnumLoader.LoadPromised();

            var result = new List<NamedType>();
            foreach (var entry in types)
            {
                var key = entry.Item1;
                var typeInfo = entry.Item2;
                var promise = entry.Item3;
                typeInfo.Entry = promise.Value;
                result.Add(new (key, typeInfo));
            }

            return result;
        }

        public static SqliteDataReader GetDataByIds(IEnumerable<int> ids, string tableName)
        {
            var command = Connection.CreateCommand();
            command.CommandText += $"SELECT * FROM [{tableName}] WHERE [id] IN ({string.Join(',', ids)})";
            
            var reader = command.ExecuteReader();
            if (!reader.HasRows)
                throw new KeyNotFoundException($"Not found any ids in table {tableName}.");
            return reader;
        }

        public static void OpenDatabase(string databasePath)
        {
            if (Connection != null && Connection.State != System.Data.ConnectionState.Closed)
                throw new Exception("Connection already opened. Close connection before open new.");

            bool emptyDatabase = false;
            if (!File.Exists(databasePath))
            {
                emptyDatabase = true;
            }

            TypedefLoader = new();
            ClassLoader = new();
            EnumLoader = new();
            FunctionLoader = new();
            FieldLoader = new();

			Connection = new SqliteConnection($"Data Source={@databasePath}");
            Connection.Open();
            
			if (emptyDatabase) CreateDatabaseTables();
        }

        public static int GetCountTableElements(string tableName)
        {
            var command = Connection.CreateCommand();
            command.CommandText = $"SELECT COUNT(*) FROM [{tableName}]";

            using var reader = command.ExecuteReader();
            reader.Read();
            return reader.GetInt32(0);
        }

        public static void Close()
        {
            if (Connection != null && Connection.State != System.Data.ConnectionState.Closed)
                Connection.Close();
        }

        private static void CreateDatabaseTables()
        {
            var command = Connection.CreateCommand();
            command.CommandText =
                @"
                CREATE TABLE [CppClasses] (
                    [id]    INTEGER,
                    [name]  TEXT NOT NULL UNIQUE,
                    [isDefault] INTEGER NOT NULL DEFAULT 0,
                    [align] INTEGER,
                    [kind] INTEGER DEFAULT 0,
	                PRIMARY KEY([id] AUTOINCREMENT)
                );

                CREATE TABLE [CppEnums](
                    [id]    INTEGER,
                    [name]  INTEGER NOT NULL UNIQUE,
                    [isDefault] INTEGER NOT NULL DEFAULT 0,
                    PRIMARY KEY([id] AUTOINCREMENT)
                );

                CREATE TABLE [CppTypedefs](
                    [id]    INTEGER,
                    [name]  TEXT NOT NULL UNIQUE,
                    [isDefault] INTEGER NOT NULL DEFAULT 0,
                    [elementId] INTEGER,
                    PRIMARY KEY([id] AUTOINCREMENT)
                );

                CREATE TABLE [CppFunctionTypes](
                    [id]    INTEGER,
                    [returnId] INTEGER,
                    PRIMARY KEY([id] AUTOINCREMENT)
                );

                CREATE TABLE [CppFunctions](
                    [id]    INTEGER,
                    [name]  TEXT NOT NULL UNIQUE,
                    [isDefault] INTEGER NOT NULL DEFAULT 0,
                    [address]   INTEGER,
                    [functionTypeId] INTEGER NOT NULL,
                    FOREIGN KEY([functionTypeId]) REFERENCES [CppFunctionTypes]([id]),
                    PRIMARY KEY([id] AUTOINCREMENT)
                );
                
                CREATE TABLE [CppPrimitives](
                    [id]    INTEGER,
                    [name]  TEXT NOT NULL UNIQUE,
                    PRIMARY KEY([id] AUTOINCREMENT)
                );

                CREATE TABLE [CppElements](
                    [id]    INTEGER,
                    [kind]  INTEGER,
                    [classId]   INTEGER,
                    [enumId]    INTEGER,
                    [typedefId] INTEGER,
                    [functionId] INTEGER,
                    [primitiveId] INTEGER,
                    [pointerLevel] INTEGER DEFAULT 0,
                    [arraySize] INTEGER DEFAULT 1,
                    FOREIGN KEY([functionId]) REFERENCES [CppFunctionTypes]([id]),
                    FOREIGN KEY([typedefId]) REFERENCES [CppTypedefs]([id]),
                    FOREIGN KEY([enumId]) REFERENCES [CppEnums]([id]),
                    FOREIGN KEY([classId]) REFERENCES [CppClasses]([id]),
                    PRIMARY KEY([id] AUTOINCREMENT)
                );

                CREATE TABLE [CppClassMembers](
                    [id]    INTEGER NOT NULL,
                    [name]  TEXT,
                    [ownerId]   INTEGER NOT NULL,
                    [elementId] INTEGER NOT NULL,
                    FOREIGN KEY([elementId]) REFERENCES [CppElements]([id]),
                    FOREIGN KEY([ownerId]) REFERENCES [CppClasses]([id]),
                    PRIMARY KEY([id] AUTOINCREMENT)
                );

                CREATE TABLE [CppEnumValues](
                    [id]    INTEGER NOT NULL,
                    [name]  TEXT NOT NULL,
                    [value] INTEGER NOT NULL,
                    [ownerId]   INTEGER NOT NULL,
                    FOREIGN KEY([ownerId]) REFERENCES [CppEnums]([id]),
                    PRIMARY KEY([id] AUTOINCREMENT)
                );

                CREATE TABLE [CppFunctionParameters](
                    [name]  TEXT,
                    [ownerId]   INTEGER NOT NULL,
                    [elementId] INTEGER NOT NULL,
                    FOREIGN KEY([ownerId]) REFERENCES [CppFunctionTypes]([id]),
                    FOREIGN KEY([elementId]) REFERENCES [CppElements]([id])
                );

                CREATE TABLE [CppFields](
                    [id]   INTEGER NOT NULL,
                    [name]  TEXT,
                    [isDefault] INTEGER,
                    [address] INTEGER,
                    [elementId] INTEGER,
                    [isTypePtr] INTEGER,
                    FOREIGN KEY([elementId]) REFERENCES [CppElements]([id]),
                    PRIMARY KEY([id] AUTOINCREMENT)
                );
                
                -- Creating indexies
                CREATE INDEX IF NOT EXISTS ownerIdMemberIndex ON CppClassMembers(ownerId);
                CREATE INDEX IF NOT EXISTS onwerIdEnumValues ON CppEnumValues(ownerId);
                CREATE INDEX IF NOT EXISTS ownerIdParameterIndex ON CppFunctionParameters(ownerId);

                CREATE INDEX IF NOT EXISTS classNameIndex ON CppClassMembers(name);
                CREATE INDEX IF NOT EXISTS enumNameIndex ON CppEnums(name);
                CREATE INDEX IF NOT EXISTS fieldsNameIndex ON CppFields(name);
                CREATE INDEX IF NOT EXISTS functionNameIndex ON CppFunctions(name);

                CREATE INDEX IF NOT EXISTS elementsClassIdIndex ON CppElements(classId);
            ";
			command.ExecuteNonQuery();
        }

        public static void WriteCompilation(CppAst.CppCompilation compilation)
        {

            int GetPointerLevel(CppAst.CppType cppType)
            {
                var level = 0;
                while (cppType.TypeKind == CppAst.CppTypeKind.Pointer)
                {
                    level++;
                    cppType = ((CppAst.CppPointerType)cppType).ElementType;
                }
                return level;
            }

            int GetArraySize(CppAst.CppType cppType) => cppType.TypeKind == CppAst.CppTypeKind.Array ? ((CppAst.CppArrayType)cppType).Size : 1;

            CppAst.CppType GetElementType(CppAst.CppType cppType)
            {
                while (cppType.TypeKind == CppAst.CppTypeKind.Pointer || cppType.TypeKind == CppAst.CppTypeKind.Array
                    || cppType.TypeKind == CppAst.CppTypeKind.Qualified)
                    cppType = ((CppAst.CppTypeWithElementType)cppType).ElementType;
                return cppType;
            }

            string GetUniqueName(CppAst.CppType cppType)
            {
                var elementType = GetElementType(cppType);
                return $"{(int)elementType.TypeKind} {elementType.GetHashCode()} {GetPointerLevel(cppType)} {GetArraySize(cppType)}";
            }

            string GetUniqueNameFromFunctionType(FunctionTypeWrapper funcType)
            {
                return funcType.isFunction ? $"{funcType.function.GetHashCode()}" : GetUniqueName(funcType.functionType);
            }

            #region FillDataLists
            var namespaces = new List<CppAst.ICppGlobalDeclarationContainer>();
            {
                var nspcStack = new Stack<CppAst.ICppGlobalDeclarationContainer>();
                nspcStack.Push(compilation);
                while (nspcStack.Count > 0)
                {
                    var currNspc = nspcStack.Pop();
                    namespaces.Add(currNspc);

                    foreach (var childNspc in currNspc.Namespaces)
                    {
                        nspcStack.Push(childNspc);
                    }
                }
            }

            var classes = new List<Tuple<CppAst.CppClass, bool>>();
            {
                var classesStack = new Stack<CppAst.CppClass>();
                var isDefault = true;
                foreach (var ns in namespaces)
                {
                    foreach (var cls in ns.Classes)
                    {
                        classesStack.Push(cls);
                        while (classesStack.Count > 0)
                        {
                            var currentCls = classesStack.Pop();
                            classes.Add(new(currentCls, isDefault));
                            foreach (var childClass in currentCls.Classes)
                            {
                                childClass.Name = $"{currentCls.Name}:[{currentCls.Classes.IndexOf(childClass)}]{childClass.Name}";

                                bool isFound = false;
                                foreach (var field in currentCls.Fields)
                                {
                                    if (field.Type == childClass)
                                    {
                                        isFound = true;
                                        break;
                                    }
                                }

                                if (!isFound)
                                {
                                    int index = 0;
                                    foreach (var field in currentCls.Fields)
                                    {
                                        if (field.Span.Start.Line > childClass.Span.Start.Line)
                                            break;
                                        index++;
                                    }
                                    var newField = new CppAst.CppField(childClass, "");
                                    currentCls.Fields.Insert(index, newField);
                                }

                                classesStack.Push(childClass);
                            }
                        }
                    }
                    isDefault = false;
                }
            }

            var enums = new List<Tuple<CppAst.CppEnum, bool>>();
            {
                var isDefault = true;
                foreach (var ns in namespaces)
                {
                    foreach (var enm in ns.Enums)
                    {
                        enums.Add(new(enm, isDefault));
                    }
                    isDefault = false;
                }
            }

            var typedefs = new List<Tuple<CppAst.CppTypedef, bool>>();
            {
                var isDefault = true;
                foreach (var ns in namespaces)
                {
                    foreach (var typedef in ns.Typedefs)
                    {
                        typedefs.Add(new(typedef, isDefault));
                    }
                    isDefault = false;
                }
            }

            var globalFields = new List<Tuple<CppAst.CppField, bool>>();
            {
                var isDefault = true;
                foreach (var ns in namespaces)
                {
                    foreach (var field in ns.Fields)
                    {
                        globalFields.Add(new(field, isDefault));
                    }
                    isDefault = false;
                }
            }

            var functions = new List<Tuple<CppAst.CppFunction, bool>>();
            var functionTypes = new List<FunctionTypeWrapper>();
            {
                var functionStack = new Stack<FunctionTypeWrapper>();
                foreach (var cls in classes)
                {
                    foreach (var func in cls.Item1.Functions)
                    {
                        functions.Add(new(func, cls.Item2));

                        var funcType = new FunctionTypeWrapper(func);
                        functionStack.Push(funcType);
                    }

                    foreach (var field in cls.Item1.Fields)
                    {
                        var elementType = GetElementType(field.Type);
                        if (elementType.TypeKind == CppAst.CppTypeKind.Function)
                        {
                            functionStack.Push(new FunctionTypeWrapper((CppAst.CppFunctionType)elementType));
                        }
                    }
                }

                foreach (var typedef in typedefs)
                {
                    var elementType = GetElementType(typedef.Item1.ElementType);
                    if (elementType.TypeKind == CppAst.CppTypeKind.Function)
                    {
                        functionStack.Push(new FunctionTypeWrapper((CppAst.CppFunctionType)elementType));
                    }
                }

                foreach (var typedef in globalFields)
                {
                    var elementType = GetElementType(typedef.Item1.Type);
                    if (elementType.TypeKind == CppAst.CppTypeKind.Function)
                    {
                        functionStack.Push(new FunctionTypeWrapper((CppAst.CppFunctionType)elementType));
                    }
                }

                var isDefault = true;
                foreach (var ns in namespaces)
                {
                    foreach (var func in ns.Functions)
                    {
                        functions.Add(new(func, isDefault));

                        var funcType = new FunctionTypeWrapper(func);

                        functionStack.Push(funcType);
                        while (functionStack.Count > 0)
                        {
                            funcType = functionStack.Pop();
                            foreach (var param in funcType.Parameters)
                            {
                                if (param.Type.TypeKind == CppAst.CppTypeKind.Function)
                                {
                                    functionStack.Push(new FunctionTypeWrapper((CppAst.CppFunctionType)param.Type));
                                }
                            }
                            functionTypes.Add(funcType);
                        }
                    }
                    isDefault = false;
                }
            }

            #endregion

            var classIds = new Dictionary<string, int>();
            var typedefIds = new Dictionary<string, int>();
            var globalFieldIds = new Dictionary<string, int>();
            var functionsIds = new Dictionary<string, int>();
            var functionTypeIds = new Dictionary<string, int>();
            var primitiveIds = new Dictionary<string, int>();
            var enumIds = new Dictionary<string, int>();

            using (var transaction = Connection.BeginTransaction())
            {

                #region CppClasses
                {
                    var command = Connection.CreateCommand();
                    command.CommandText = "INSERT INTO [CppClasses] ([name], [align], [kind], [isDefault]) VALUES($name, $align, $kind, $isDefault)";

                    var nameParam = CreateParameter(command, "name");
                    var alignParam = CreateParameter(command, "align");
                    var kindParam = CreateParameter(command, "kind");
                    var isDefaultParam = CreateParameter(command, "isDefault");

                    var counter = 1;
                    foreach (var clsData in classes)
                    {
                        var cls = clsData.Item1;
                        var isDefault = clsData.Item2;

                        nameParam.Value = cls.Name;
                        alignParam.Value = (cls.Attributes.Count > 0 ? int.Parse(cls.Attributes[0].Arguments) : 1);
                        isDefaultParam.Value = (isDefault ? 1 : 0);
                        kindParam.Value = (int)cls.ClassKind;

                        command.ExecuteNonQuery();

                        classIds.Add(cls.Name, counter);
                        counter++;
                    }
                }
                #endregion

                #region CppEnums
                {
                    var command = Connection.CreateCommand();
                    command.CommandText = $"INSERT INTO [CppEnums] ([name], [isDefault]) VALUES($name, $isDefault)";

                    var nameParam = CreateParameter(command, "name");
                    var isDefaultParam = CreateParameter(command, "isDefault");

                    var counter = 1;
                    foreach (var enmData in enums)
                    {
                        var enm = enmData.Item1;
                        var isDefault = enmData.Item2;

                        nameParam.Value = enm.Name;
                        isDefaultParam.Value = (isDefault ? 1 : 0);

                        command.ExecuteNonQuery();

                        enumIds.Add(enm.Name, counter);
                        counter++;
                    }

                }
                #endregion

                #region CppTypedefs
                {
                    var command = Connection.CreateCommand();
                    command.CommandText = "INSERT INTO [CppTypedefs] ([name], [isDefault]) VALUES ($name, $isDefault)";

                    var nameParam = CreateParameter(command, "name");
                    var isDefaultParam = CreateParameter(command, "isDefault");

                    var counter = 1;

                    foreach (var typedefData in typedefs)
                    {
                        var typedef = typedefData.Item1;
                        var isDefault = typedefData.Item2;

                        if (typedefIds.ContainsKey(typedef.Name))
                            continue;

                        nameParam.Value = typedef.Name;
                        isDefaultParam.Value = (isDefault ? 1 : 0);

                        command.ExecuteNonQuery();

                        typedefIds.Add(typedef.Name, counter);
                        counter++;
                    }
                }
                #endregion

                #region CppFunctionTypes
                {
                    var command = Connection.CreateCommand();
                    command.CommandText = "INSERT INTO [CppFunctionTypes] DEFAULT VALUES;";

                    var counter = 1;

                    foreach (var funcType in functionTypes)
                    {
                        var uniqueName = GetUniqueNameFromFunctionType(funcType);
                        if (!functionTypeIds.ContainsKey(uniqueName))
                        {
                            command.ExecuteNonQuery();

                            functionTypeIds.Add(uniqueName, counter);
                            counter++;
                        }
                    }
                }
                #endregion

                #region CppFunctions
                {
                    var command = Connection.CreateCommand();
                    command.CommandText = "INSERT INTO [CppFunctions] ([name], [address], [isDefault], [functionTypeId]) " +
                        "VALUES($name, $address, $isDefault, $functionTypeId)";

                    var nameParam = CreateParameter(command, "name");
                    var isDefaultParam = CreateParameter(command, "isDefault");
                    var addressParam = CreateParameter(command, "address");
                    var functionTypeIdParam = CreateParameter(command, "functionTypeId");

                    var counter = 1;

                    foreach (var funcData in functions)
                    {
                        var func = funcData.Item1;
                        var isDefault = funcData.Item2;

                        nameParam.Value = func.Name;
                        isDefaultParam.Value = (isDefault ? 1 : 0);

                        var attributes = new Dictionary<string, string>();
                        foreach (var attr in func.Attributes)
                        {
                            attributes.Add(attr.Name, attr.Arguments);
                        }

                        addressParam.Value = attributes.ContainsKey("address") ? int.Parse(attributes["address"].ToString()) : DBNull.Value;

                        var tempType = new FunctionTypeWrapper(func);
                        functionTypeIdParam.Value = functionTypeIds[GetUniqueNameFromFunctionType(tempType)];

                        command.ExecuteNonQuery();

                        functionsIds.Add(func.Name, counter);
                        counter++;
                    }
                }
                #endregion

                #region CppFields
                {
                    var command = Connection.CreateCommand();
                    command.CommandText = "INSERT INTO [CppFields] ([name], [address], [isDefault], [isTypePtr]) " +
                        "VALUES($name, $address, $isDefault, $isTypePtr)";

                    var nameParam = CreateParameter(command, "name");
                    var isDefaultParam = CreateParameter(command, "isDefault");
                    var addressParam = CreateParameter(command, "address");
                    var isTypePtrParam = CreateParameter(command, "isTypePtr");

                    var counter = 1;

                    foreach (var fieldData in globalFields)
                    {
                        var field = fieldData.Item1;
                        var isDefault = fieldData.Item2;

                        nameParam.Value = field.Name;
                        isDefaultParam.Value = (isDefault ? 1 : 0);

                        var attributes = new Dictionary<string, string>();
                        if (field.Attributes is not null)
                        {
                            foreach (var attr in field.Attributes)
                            {
                                attributes.Add(attr.Name, attr.Arguments);
                            }
                        }

                        addressParam.Value = attributes.ContainsKey("address") ? int.Parse(attributes["address"].ToString()) : DBNull.Value;
                        isTypePtrParam.Value = attributes.ContainsKey("typePtr") ? 1 : 0;

                        command.ExecuteNonQuery();

                        globalFieldIds.Add(field.Name, counter);
                        counter++;
                    }
                }
                #endregion

                #region CppPrimitives
                {
                    var command = Connection.CreateCommand();
                    command.CommandText = "INSERT INTO [CppPrimitives] ([name]) VALUES ($name)";

                    var nameParam = CreateParameter(command, "name");

                    var primitivesName = new string[]
                    {
                    "void", "wchar", "char", "short", "int", "long", "long long",
                    "unsigned char", "unsigned short", "unsigned int", "unsigned long long",
                    "float", "double", "long double", "bool"
                    };

                    var counter = 1;

                    foreach (var primitiveName in primitivesName)
                    {
                        nameParam.Value = primitiveName;
                        command.ExecuteNonQuery();

                        primitiveIds.Add(primitiveName, counter);
                        counter++;
                    }
                }
                #endregion

                transaction.Commit();
            }

            var classFieldsList = new List<Tuple<CppAst.CppField, int, int>>();
            var globalFieldsList = new List<Tuple<int, int>>();
            var typedefRefList = new List<Tuple<int, int>>();
            var functionParamsList = new List<Tuple<CppAst.CppParameter, int, int>>();
            var functionReturnList = new List<Tuple<int, int>>();

            using (var transaction = Connection.BeginTransaction())
            {
                #region CppElements
                {
                    var command = Connection.CreateCommand();
                    command.CommandText = "INSERT INTO [CppElements] ([kind], [classId], [enumId], [typedefId], [functionId], [primitiveId], [pointerLevel], [arraySize]) " +
                        "VALUES($kind, $classId, $enumId, $typedefId, $functionId, $primitiveId, $pointerLevel, $arraySize)";

                    var kindParam = CreateParameter(command, "kind");

                    var classIdParam = CreateParameter(command, "classId");
                    var enumIdParam = CreateParameter(command, "enumId");
                    var typedefIdParam = CreateParameter(command, "typedefId");
                    var functionIdParam = CreateParameter(command, "functionId");
                    var primitiveIdParam = CreateParameter(command, "primitiveId");

                    var pointerLevelParam = CreateParameter(command, "pointerLevel");
                    var arraySizeParam = CreateParameter(command, "arraySize");

                    Func<CppAst.CppType, bool> SetParamsForType = (cppType) =>
                    {
                        var elementType = GetElementType(cppType);

                        classIdParam.Value = DBNull.Value;
                        enumIdParam.Value = DBNull.Value;
                        typedefIdParam.Value = DBNull.Value;
                        functionIdParam.Value = DBNull.Value;
                        primitiveIdParam.Value = DBNull.Value;

                        switch (elementType.TypeKind)
                        {
                            case CppAst.CppTypeKind.StructOrClass:
                                var name = ((CppAst.CppClass)elementType).Name;
                                classIdParam.Value = classIds[name];
                                break;
                            case CppAst.CppTypeKind.Enum:
                                enumIdParam.Value = enumIds[((CppAst.CppEnum)elementType).Name];
                                break;
                            case CppAst.CppTypeKind.Typedef:
                                typedefIdParam.Value = typedefIds[((CppAst.CppTypedef)elementType).Name];
                                break;
                            case CppAst.CppTypeKind.Primitive:
                                primitiveIdParam.Value = primitiveIds[((CppAst.CppPrimitiveType)elementType).ToString()];
                                break;
                            case CppAst.CppTypeKind.Function:
                                var funcType = new FunctionTypeWrapper((CppAst.CppFunctionType)elementType);
                                functionIdParam.Value = functionTypeIds[GetUniqueNameFromFunctionType(funcType)];
                                break;
                            default:
                                throw new NotImplementedException($"Type {elementType.TypeKind} not implemented to load into database.");
                        };

                        kindParam.Value = (int)elementType.TypeKind;
                        pointerLevelParam.Value = GetPointerLevel(cppType);
                        arraySizeParam.Value = GetArraySize(cppType);
                        return true;
                    };

                    var elementIds = new Dictionary<string, int>();

                    var counter = 1;

                    foreach (var clsData in classes)
                    {
                        var cls = clsData.Item1;
                        foreach (var field in cls.Fields)
                        {
                            var elementId = 0;
                            var uniqueName = GetUniqueName(field.Type);
                            if (!elementIds.ContainsKey(uniqueName))
                            {
                                if (!SetParamsForType(field.Type))
                                    continue;

                                command.ExecuteNonQuery();

                                elementId = counter;
                                elementIds.Add(uniqueName, counter);
                                counter++;
                            }
                            else
                                elementId = elementIds[uniqueName];

                            classFieldsList.Add(new(field, classIds[cls.Name], elementId));
                        }
                    }

                    foreach (var typedefData in typedefs)
                    {
                        var typedef = typedefData.Item1;

                        var elementId = 0;
                        var uniqueName = GetUniqueName(typedef.ElementType);
                        if (!elementIds.ContainsKey(uniqueName))
                        {
                            SetParamsForType(typedef.ElementType);
                            command.ExecuteNonQuery();

                            elementId = counter;
                            elementIds.Add(uniqueName, counter);
                            counter++;
                        }
                        else
                            elementId = elementIds[uniqueName];

                        typedefRefList.Add(new(typedefIds[typedef.Name], elementId));
                    }

                    foreach (var funcType in functionTypes)
                    {
                        var funcTypeUniqueName = GetUniqueNameFromFunctionType(funcType);
                        foreach (var param in funcType.Parameters)
                        {
                            var elementId = 0;
                            var uniqueName = GetUniqueName(param.Type);
                            if (!elementIds.ContainsKey(uniqueName))
                            {
                                SetParamsForType(param.Type);
                                command.ExecuteNonQuery();

                                elementId = counter;
                                elementIds.Add(uniqueName, counter);
                                counter++;
                            }
                            else
                                elementId = elementIds[uniqueName];

                            var functionTypeId = functionTypeIds[funcTypeUniqueName];
                            functionParamsList.Add(new(param, functionTypeId, elementId));
                        }

                        {
                            var elementId = 0;
                            var uniqueName = GetUniqueName(funcType.ReturnType);
                            if (!elementIds.ContainsKey(uniqueName))
                            {
                                SetParamsForType(funcType.ReturnType);
                                command.ExecuteNonQuery();

                                elementId = counter;
                                elementIds.Add(uniqueName, counter);
                                counter++;
                            }
                            else
                                elementId = elementIds[uniqueName];

                            var functionTypeId = functionTypeIds[funcTypeUniqueName];
                            functionReturnList.Add(new(functionTypeId, elementId));
                        }

                    }

                    foreach (var fieldData in globalFields)
                    {
                        var field = fieldData.Item1;

                        var elementId = 0;
                        var uniqueName = GetUniqueName(field.Type);
                        if (!elementIds.ContainsKey(uniqueName))
                        {
                            SetParamsForType(field.Type);
                            command.ExecuteNonQuery();

                            elementId = counter;
                            elementIds.Add(uniqueName, counter);
                            counter++;
                        }
                        else
                            elementId = elementIds[uniqueName];

                        globalFieldsList.Add(new(globalFieldIds[field.Name], elementId));
                    }
                }
                #endregion

                transaction.Commit();
            }

            using (var transaction = Connection.BeginTransaction()) {

                #region CppClassMembers
                {
                    var command = Connection.CreateCommand();
                    command.CommandText = "INSERT INTO [CppClassMembers] ([name], [ownerId], [elementId]) VALUES ($name, $ownerId, $elementId)";

                    var nameParam = CreateParameter(command, "name");
                    var ownerIdParam = CreateParameter(command, "ownerId");
                    var elementIdParam = CreateParameter(command, "elementId");

                    foreach (var data in classFieldsList)
                    {
                        nameParam.Value = data.Item1.Name;
                        ownerIdParam.Value = data.Item2;
                        elementIdParam.Value = data.Item3;

                        command.ExecuteNonQuery();
                    }
                }
                #endregion

                #region CppFunctionParameters
                {
                    var command = Connection.CreateCommand();
                    command.CommandText = "INSERT INTO [CppFunctionParameters] ([name], [ownerId], [elementId]) VALUES ($name, $ownerId, $elementId)";

                    var nameParam = CreateParameter(command, "name");
                    var ownerIdParam = CreateParameter(command, "ownerId");
                    var elementIdParam = CreateParameter(command, "elementId");

                    foreach (var data in functionParamsList)
                    {
                        nameParam.Value = data.Item1.Name;
                        ownerIdParam.Value = data.Item2;
                        elementIdParam.Value = data.Item3;

                        command.ExecuteNonQuery();
                    }
                }
                #endregion

                #region CppEnumValues
                {
                    var command = Connection.CreateCommand();
                    command.CommandText = "INSERT INTO [CppEnumValues] ([ownerId], [name], [value]) VALUES ($ownerId, $name, $value)";

                    var ownerIdParam = CreateParameter(command, "ownerId");
                    var nameParam = CreateParameter(command, "name");
                    var valueParam = CreateParameter(command, "value");

                    var nspcStack = new Stack<CppAst.ICppGlobalDeclarationContainer>();
                    nspcStack.Push(compilation);
                    while (nspcStack.Count > 0)
                    {
                        var currNspc = nspcStack.Pop();
                        foreach (var enm in currNspc.Enums)
                        {
                            var ownerId = enumIds[enm.Name];
                            foreach (var item in enm.Items)
                            {
                                nameParam.Value = item.Name;
                                valueParam.Value = item.Value;

                                ownerIdParam.Value = ownerId;
                                command.ExecuteNonQuery();
                            }
                        }

                        foreach (var childNspc in currNspc.Namespaces)
                        {
                            nspcStack.Push(childNspc);
                        }
                    }
                }
                #endregion

                #region CppTypedefs.elementId
                {
                    var command = Connection.CreateCommand();
                    command.CommandText = "UPDATE [CppTypedefs] SET elementId = $elementId WHERE id = $ownerId";

                    var ownerIdParam = CreateParameter(command, "ownerId");
                    var elementIdParam = CreateParameter(command, "elementId");

                    foreach (var data in typedefRefList)
                    {
                        ownerIdParam.Value = data.Item1;
                        elementIdParam.Value = data.Item2;

                        command.ExecuteNonQuery();
                    }
                }
                #endregion

                #region CppFields.elementId
                {
                    var command = Connection.CreateCommand();
                    command.CommandText = "UPDATE [CppFields] SET elementId = $elementId WHERE id = $ownerId";

                    var ownerIdParam = CreateParameter(command, "ownerId");
                    var elementIdParam = CreateParameter(command, "elementId");

                    foreach (var data in globalFieldsList)
                    {
                        ownerIdParam.Value = data.Item1;
                        elementIdParam.Value = data.Item2;

                        command.ExecuteNonQuery();
                    }
                }
                #endregion

                #region CppFunctionTypes.returnId
                {
                    var command = Connection.CreateCommand();
                    command.CommandText = "UPDATE [CppFunctionTypes] SET returnId = $returnId WHERE id = $ownerId";

                    var ownerIdParam = CreateParameter(command, "ownerId");
                    var returnIdParam = CreateParameter(command, "returnId");

                    foreach (var data in functionReturnList)
                    {
                        ownerIdParam.Value = data.Item1;
                        returnIdParam.Value = data.Item2;

                        command.ExecuteNonQuery();
                    }
                }
                #endregion

                transaction.Commit();
            }
        }

    }
}
