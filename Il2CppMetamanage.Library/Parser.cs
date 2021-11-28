using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Il2CppMetamanage.Library
{
    public class Parser
    {
        const string defaultTypedefs =
            "typedef unsigned __int8 uint8_t;\n" +
            "typedef unsigned __int16 uint16_t;\n" +
            "typedef unsigned __int32 uint32_t;\n" +
            "typedef unsigned __int64 uint64_t;\n" +
            "typedef __int8 int8_t;\n" +
            "typedef __int16 int16_t;\n" +
            "typedef __int32 int32_t;\n" +
            "typedef __int64 int64_t;\n" +
            "typedef size_t intptr_t;\n" +
            "typedef size_t uintptr_t;\n" +
            "\n" +
            "#define DO_APP_FUNC(a, r, n, p) r n p\n" + 
            "#define DO_APP_FUNC_METHODINFO(a, n) extern struct MethodInfo ** n\n" +
            "\n";


        private readonly Dictionary<string, CppAst.CppAttribute> classAligns = new();
        private readonly Dictionary<string, CppAst.CppAttribute> functionsAddresses = new();
        private readonly Dictionary<string, CppAst.CppAttribute> fieldsAdresses = new();

        private static readonly Regex alignRegex = new(@"(?:__attribute__\(\(aligned\((?<Align>\d+)\)\)\)|__declspec\(align\((?<Align>\d+)\)\)) (?<Name>[\w\d_]+) {");
        private static readonly Regex funcAdrRegex = new(@"DO_APP_FUNC\(0x(?<Address>[\dA-F]+), [^,]+, (?<Name>[\w\d_]+),");
        private static readonly Regex fieldsAdrRegex = new(@"DO_APP_FUNC_METHODINFO\(0x(?<Address>[\dA-F]+), (?<Name>[\w\d_]+)");

        private static readonly string[] forbiddenWords = new[] {
            "__attribute__((aligned(8)))",
            "__declspec(align(8))",
            "#pragma pack(push, p1,4)",
            "#pragma pack(pop, p1)",
            "#include \"il2cpp-types.h\"",
            "using namespace app;"
        };

        private void AddAligns(string line)
        {   
            Match match = alignRegex.Match(line);
            if (!match.Success)
                return;

            var attribute = new CppAst.CppAttribute("Align");
            attribute.Arguments = match.Groups["Align"].Value;
                
            classAligns.Add(match.Groups["Name"].Value, attribute);
        }

        private void AddFunctionAddress(string line)
        {

            Match match = funcAdrRegex.Match(line);
            if (!match.Success)
                return;

            var addressText = match.Groups["Address"].Value;
            var name = match.Groups["Name"].Value;
            var address = int.Parse(addressText, System.Globalization.NumberStyles.HexNumber);

            var attribute = new CppAst.CppAttribute("address");
            attribute.Arguments = address.ToString();

            functionsAddresses.Add(name, attribute);

        }

        private void AddFieldAddress(string line)
        {
            Match match = fieldsAdrRegex.Match(line);
            if (!match.Success)
                return;

            var addressText = match.Groups["Address"].Value;
            var name = match.Groups["Name"].Value;
            var address = int.Parse(addressText, System.Globalization.NumberStyles.HexNumber);

            var attribute = new CppAst.CppAttribute("address");
            attribute.Arguments = address.ToString();

            fieldsAdresses.Add(name, attribute);
        }

        public CppAst.CppCompilation ParseHeaderFile(string typeFilepath, string functionsFilepath)
        {
            string content;
            {
                StringBuilder contentBuilder = new();
                contentBuilder.Append(defaultTypedefs);
                using (var reader = new StreamReader(typeFilepath))
                {
                    while (!reader.EndOfStream)
                    {
                        string line = reader.ReadLine();
                        AddAligns(line);
                        contentBuilder.AppendLine(line);
                    }
                }

                contentBuilder.AppendLine("namespace app {");
                using (var reader = new StreamReader(functionsFilepath))
                {
                    while (!reader.EndOfStream)
                    {
                        string line = reader.ReadLine();
                        AddFieldAddress(line);
                        AddFunctionAddress(line);
                        contentBuilder.AppendLine(line);
                    }
                }
                contentBuilder.AppendLine("}");

                content = contentBuilder.ToString();
            }

            foreach (var word in forbiddenWords)
            {
                content = content.Replace(word, "");
            }

            var compilation = CppAst.CppParser.Parse(content);
            var namespaceStack = new Stack<CppAst.ICppGlobalDeclarationContainer>();
            namespaceStack.Push(compilation);
            while (namespaceStack.Count > 0)
            {
                var currentNamespace = namespaceStack.Pop();
                foreach (var cppClass in currentNamespace.Classes)
                {
                    if (classAligns.ContainsKey(cppClass.Name))
                    {
                        cppClass.Attributes.Add(classAligns[cppClass.Name]);
                        classAligns.Remove(cppClass.Name);
                    }
                }

                foreach (var func in currentNamespace.Functions)
                {
                    if (functionsAddresses.ContainsKey(func.Name))
                    {
                        func.Attributes.Add(functionsAddresses[func.Name]);
                        functionsAddresses.Remove(func.Name);
                    }
                }

                foreach (var field in currentNamespace.Fields)
                {
                    if (fieldsAdresses.ContainsKey(field.Name))
                    {
                        if (field.Attributes is null)
                            field.Attributes = new List<CppAst.CppAttribute>();
                        field.Attributes.Add(fieldsAdresses[field.Name]);
                        fieldsAdresses.Remove(field.Name);
                    }
                }

                foreach (var childNamespace in currentNamespace.Namespaces)
                {
                    namespaceStack.Push(childNamespace);
                }
            }
            return compilation;
        }

    }
}
