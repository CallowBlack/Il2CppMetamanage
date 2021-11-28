using System;
using System.IO;
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
                TestFields();
            }
        }

        static void TestFields()
        {
            Console.Write("Field id: ");
            var fieldId = int.Parse(Console.ReadLine());

            Console.WriteLine("Finding field...");
            var promise = SQLDataManager.FieldLoader.AddToOrder(fieldId);
            SQLDataManager.FieldLoader.LoadOrdered();

            var field = promise.Value as SQLCppGlobalField;
            Console.WriteLine(field);
        }

        static void TestClassFindDependencies()
        {
            Console.Write("Class id: ");
            var classId = int.Parse(Console.ReadLine());

            Console.WriteLine("Finding dependencies...");
            var promise = SQLDataManager.ClassLoader.AddToOrder(classId);
            SQLDataManager.ClassLoader.LoadOrdered();

            var cls = promise.Value as SQLCppClass;
            var dependencies = SQLDataManager.GetDependencies(new SQLCppClass[] { cls });
            Console.WriteLine($"Found {dependencies.Count} dependencies.");
        }
    }
}
