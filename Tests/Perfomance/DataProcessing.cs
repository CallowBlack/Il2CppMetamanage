using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tests.Perfomance
{
    internal static class DataProcessing
    {

        
        static public void DumpCompilation(string directory, CppAst.CppCompilation compilation)
        {
            // Ouput some info
            Console.WriteLine("Header file information:");
            Console.WriteLine("Message count: {0}", compilation.Diagnostics.Messages.Count);
            Console.WriteLine("Enum count: {0}", compilation.Enums.Count);
            Console.WriteLine("Function count: {0}", compilation.Functions.Count);
            Console.WriteLine("Class count: {0}", compilation.Classes.Count);
            Console.WriteLine("Typedef count: {0}", compilation.Typedefs.Count);
            Console.WriteLine("Namespaces count: {0}", compilation.Namespaces.Count);

            // Dumping files
            var errorFilePath = directory + @"\error-messages.txt";

            using (StreamWriter writer = new StreamWriter(errorFilePath))
            {
                foreach (var message in compilation.Diagnostics.Messages)
                {
                    writer.WriteLine(message);
                }
            }

            var contentFilePath = directory + @"\header-structure.txt";

            using (StreamWriter writer = new StreamWriter(contentFilePath))
            {
                for (int i = -1; i < compilation.Namespaces.Count; i++)
                {
                    string namespaceName = "Global";
                    CppAst.ICppGlobalDeclarationContainer currentNamespace = compilation;

                    if (i >= 0)
                    {
                        currentNamespace = compilation.Namespaces[i];
                        namespaceName = compilation.Namespaces[i].Name;
                    }

                    writer.WriteLine("Namespace {0}", namespaceName);
                    writer.WriteLine("\tEnum count: {0}", currentNamespace.Enums.Count);
                    foreach (var data in currentNamespace.Enums)
                    {
                        writer.WriteLine("\t\t" + data);
                    }

                    writer.WriteLine("\tFunction count: {0}", currentNamespace.Functions.Count);
                    foreach (var data in currentNamespace.Functions)
                    {
                        writer.WriteLine("\t\t" + data);
                    }

                    writer.WriteLine("\tClass count: {0}", currentNamespace.Classes.Count);
                    foreach (var data in currentNamespace.Classes)
                    {
                        writer.WriteLine("\t\t" + data + data.Name);
                    }

                    writer.WriteLine("\tTypedef count: {0}", currentNamespace.Typedefs.Count);
                    foreach (var data in currentNamespace.Typedefs)
                    {
                        writer.WriteLine("\t\t" + data);
                    }

                    writer.WriteLine("\tNamespaces count: {0}", currentNamespace.Namespaces.Count);
                    foreach (var data in currentNamespace.Namespaces)
                    {
                        writer.WriteLine("\t\t" + data);
                    }
                }
            }
        }


    }
}
