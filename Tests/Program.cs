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
            var typeFileInfo = new FileInfo(@"C:\Users\strog\Desktop\Temps\GenshinImpact\il2cpp-types.h");
            var functionFileInfo = new FileInfo(@"C:\Users\strog\Desktop\Temps\GenshinImpact\il2cpp-functions.h");
            //var typeFileInfo = new FileInfo(@"C:\Users\strog\Desktop\Temps\CrabGame\appdata\il2cpp-types.h");
            //var functionFileInfo = new FileInfo(@"C:\Users\strog\Desktop\Temps\CrabGame\appdata\il2cpp-functions.h");

            var databasePath = typeFileInfo.Directory.FullName + @"\database.db";
            var isEmpty = false;
            if (!File.Exists(databasePath))
                isEmpty = true;
            //    File.Delete(databasePath);

            SQLDataManager.OpenDatabase(databasePath);
            
            if (isEmpty)
            {
                Parser parser = new Parser();
                CppCompilation compilation = parser.ParseHeaderFile(typeFileInfo.FullName, functionFileInfo.FullName);

                Console.WriteLine("Writing data to database...");
                SQLDataManager.WriteCompilation(compilation);
            }

            while (true)
            {
                Console.Write("Class id: ");
                var classId = int.Parse(Console.ReadLine());

                Console.WriteLine("Finding dependencies...");
                var promise = SQLDataManager.ClassLoader.AddToOrder(classId);
                SQLDataManager.ClassLoader.LoadOrdered();
                var cls = promise.Value as SQLCppClass;
                
                var dependencies = SQLDataManager.GetDependencies(new SQLCppClass[] { cls });
                foreach (var dependency in dependencies)
                    Console.Write(dependency.ToString());
            }

        }

    }
}
