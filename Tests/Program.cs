using System;
using System.Collections.Generic;
using System.IO;
using System.Timers;
using CppAst;
using Il2CppMetamanage.Library;
using Il2CppMetamanage.Library.Data;
using Il2CppMetamanage.Library.Data.Model;

namespace Tests
{
    internal class Program
    {
        static void Main(string[] args)
        {
#if false
            var appdataDirectoryPath = @"C:\Users\strog\Desktop\Temps\GenshinImpact\Il2CppManagerTestData";
#else
            var appdataDirectoryPath = @"C:\Users\strog\Desktop\Temps\CrabGame\Il2CppManagerData";
#endif
            var typesFilePath = appdataDirectoryPath + @"\il2cpp-types.h";
            var functionsFilePath = appdataDirectoryPath + @"\il2cpp-functions.h";
            var typesPtrFilePath = appdataDirectoryPath + @"\il2cpp-types-ptr.h";

            var databasePath = appdataDirectoryPath + @"\database.db";
            var isEmpty = false;
            if (!File.Exists(databasePath))
                isEmpty = true;
            //    File.Delete(databasePath);

            SQLDataManager.OpenDatabase(databasePath);
            
            if (isEmpty)
            {
                Console.WriteLine("Parsing .h files...");
                var parser = new Parser();
                CppCompilation compilation = parser.ParseHeaderFile(typesFilePath, functionsFilePath, typesPtrFilePath);

                Console.WriteLine("Writing data to database...");
                SQLDataManager.WriteCompilation(compilation);
            }

            while (true)
            {
                TestDependencies();
                Console.WriteLine("Any key to continue...");
                Console.ReadKey();
                Console.Clear();
            }
        }

        static void TestLoaderFunctionality()
        {
            Console.WriteLine("[1] Class count: " + SQLDataManager.ClassLoader.Count);
            Console.WriteLine("[2] Enum count: " + SQLDataManager.EnumLoader.Count);
            Console.WriteLine("[3] Field count: " + SQLDataManager.FieldLoader.Count);
            Console.WriteLine("[4] Function count: " + SQLDataManager.FunctionLoader.Count);
            Console.WriteLine("[5] Typedef count: " + SQLDataManager.TypedefLoader.Count);

            Console.Write("Select type [1-5]: ");
            var elementType = int.Parse(Console.ReadLine());
            if (elementType > 5 || elementType < 1)
                return;

            Console.Write("Input start id: ");
            var startId = int.Parse(Console.ReadLine());

            Console.Write("Input count: ");
            var count = int.Parse(Console.ReadLine());

            var elements = new List<SQLEntry>();
            switch (elementType)
            {
                case 1:
                    elements.AddRange(SQLDataManager.ClassLoader.GetNextElements(startId, count));
                    break;
                case 2:
                    elements.AddRange(SQLDataManager.EnumLoader.GetNextElements(startId, count));
                    break;
                case 3:
                    elements.AddRange(SQLDataManager.FieldLoader.GetNextElements(startId, count));
                    break;
                case 4:
                    elements.AddRange(SQLDataManager.FunctionLoader.GetNextElements(startId, count));
                    break;
                case 5:
                    elements.AddRange(SQLDataManager.TypedefLoader.GetNextElements(startId, count));
                    break;
            }
            elements.ForEach((element) => Console.WriteLine(element));
        }

        static void TestFields()
        {
            Console.Write("Field id: ");
            var fieldId = int.Parse(Console.ReadLine());

            Console.WriteLine("Finding field...");
            var promise = SQLDataManager.FieldLoader.GetPromise(fieldId);
            SQLDataManager.FieldLoader.LoadPromised();

            var field = promise.Value as SQLCppGlobalField;
            Console.WriteLine(field);
        }

        static void TestFindByName()
        {
            Console.WriteLine("[1] Class count: " + SQLDataManager.ClassLoader.Count);
            Console.WriteLine("[2] Enum count: " + SQLDataManager.EnumLoader.Count);
            Console.WriteLine("[3] Field count: " + SQLDataManager.FieldLoader.Count);
            Console.WriteLine("[4] Function count: " + SQLDataManager.FunctionLoader.Count);
            Console.WriteLine("[5] Typedef count: " + SQLDataManager.TypedefLoader.Count);

            Console.Write("Select type [1-5]: ");
            var elementType = int.Parse(Console.ReadLine());
            if (elementType > 5 || elementType < 1)
                return;

            Console.Write("Input part of name: ");
            var name = Console.ReadLine();

            Console.WriteLine("Finding by name...");
            var watch = System.Diagnostics.Stopwatch.StartNew();
            List<SQLNamedEntry> element = new();
            switch (elementType)
            {
                case 1:
                    element.AddRange(SQLDataManager.ClassLoader.FindElementsByName(name));
                    break;
                case 2:
                    element.AddRange(SQLDataManager.EnumLoader.FindElementsByName(name));
                    break;
                case 3:
                    element.AddRange(SQLDataManager.FieldLoader.FindElementsByName(name));
                    break;
                case 4:
                    element.AddRange(SQLDataManager.FunctionLoader.FindElementsByName(name));
                    break;
                case 5:
                    element.AddRange(SQLDataManager.TypedefLoader.FindElementsByName(name));
                    break;
                default:
                    throw new Exception();
            }
            watch.Stop();
            Console.WriteLine($"Find takes {watch.Elapsed.TotalMilliseconds} ms.");
            Console.WriteLine($"Found {element.Count} elements with this name.");
        }

        static void TestDependencies()
        {
            Console.WriteLine("[1] Class count: " + SQLDataManager.ClassLoader.Count);
            Console.WriteLine("[2] Enum count: " + SQLDataManager.EnumLoader.Count);
            Console.WriteLine("[3] Field count: " + SQLDataManager.FieldLoader.Count);
            Console.WriteLine("[4] Function count: " + SQLDataManager.FunctionLoader.Count);
            Console.WriteLine("[5] Typedef count: " + SQLDataManager.TypedefLoader.Count);

            Console.Write("Select type [1-5]: ");
            var elementType = int.Parse(Console.ReadLine());
            if (elementType > 5 || elementType < 1)
                return;

            Console.Write("Input start id: ");
            var startId = int.Parse(Console.ReadLine());

            Console.WriteLine("Finding dependencies...");
            SQLNamedEntry element;
            switch (elementType)
            {
                case 1:
                    element = SQLDataManager.ClassLoader[startId];
                    break;
                case 2:
                    element = SQLDataManager.EnumLoader[startId];
                    break;
                case 3:
                    element = SQLDataManager.FieldLoader[startId];
                    break;
                case 4:
                    element = SQLDataManager.FunctionLoader[startId];
                    break;
                case 5:
                    element = SQLDataManager.TypedefLoader[startId];
                    break;
                default:
                    throw new Exception();
            }
            var watch = System.Diagnostics.Stopwatch.StartNew();
            var dependencies = SQLDataManager.GetDependencies(new [] { element });
            watch.Stop();
            Console.WriteLine($"Find takes {watch.Elapsed.TotalMilliseconds} ms.");
            
            var counts = new Dictionary<string, int>();
            counts["Classes"] = 0;
            counts["Typedefs"] = 0;
            counts["Enums"] = 0;
            counts["Fields"] = 0;
            counts["Functions"] = 0;
            Console.WriteLine($"Found {dependencies.Count} dependencies:");
            foreach (var dependency in dependencies)
            {
                switch (dependency.TypeKind)
                {
                    case SQLCppTypeKind.Class:
                        counts["Classes"]++;
                        break;
                    case SQLCppTypeKind.Enum:
                        counts["Enums"]++;
                        break;
                    case SQLCppTypeKind.Typedef:
                        counts["Typedefs"]++;
                        break;
                    case SQLCppTypeKind.Field:
                        Console.WriteLine(dependency);
                        counts["Fields"]++;
                        break;
                    case SQLCppTypeKind.FunctionType:
                    case SQLCppTypeKind.Function:
                        counts["Functions"]++;
                        break;
                }
            }
            foreach (var entry in counts)
                Console.WriteLine($"\t{entry.Key} -> {entry.Value}");
        }
    }
}
