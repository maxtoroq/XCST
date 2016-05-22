using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using Xcst.Compiler;

namespace Xcst.Web.Mvc.Tests.Extension {

   static class ExtensionTestsHelper {

      static readonly XcstCompilerFactory CompilerFactory = new XcstCompilerFactory {
         EnableExtensions = true,
         PackageTypeResolver = (typeName) => Assembly.GetExecutingAssembly()
            .GetType(typeName)
      };

      static ExtensionTestsHelper() {
         CompilerFactory.RegisterWebExtension();
         CompilerFactory.RegisterApplicationExtension();
      }

      public static Type CompileFromFile(string fileName, bool correct) {

         using (var fileStream = File.OpenRead(fileName)) {

            XcstCompiler compiler = CompilerFactory.CreateCompiler();
            compiler.TargetNamespace = typeof(ExtensionTestsHelper).Namespace + ".Runtime";
            compiler.TargetClass = "TestModule";
            compiler.UseLineDirective = true;
            compiler.UsePackageBase = new StackFrame(1, true).GetMethod().DeclaringType.Namespace;

            compiler.SetParameter(
               new QualifiedName("application-uri", XmlNamespaces.XcstWeb),
               new Uri(Directory.GetCurrentDirectory())
            );

            CompileResult xcstResult;

            try {
               xcstResult = compiler.Compile(fileStream, baseUri: new Uri(fileName, UriKind.Absolute));

            } catch (CompileException ex) {

               if (!correct) {
                  Console.WriteLine(ex.Message);
                  Console.WriteLine($"Module URI: {ex.ModuleUri}");
                  Console.WriteLine($"Line number: {ex.LineNumber}");
               }

               throw;
            }

            foreach (string unit in xcstResult.CompilationUnits) {
               Console.WriteLine(unit);
            }

            if (!correct) {
               return null;
            }

            SyntaxTree[] syntaxTrees = xcstResult.CompilationUnits
               .Select(c => CSharpSyntaxTree.ParseText(c))
               .ToArray();

            // TODO: Should compiler give list of assembly references?

            MetadataReference[] references = {
               MetadataReference.CreateFromFile(typeof(System.Object).Assembly.Location),
               MetadataReference.CreateFromFile(typeof(System.Uri).Assembly.Location),
               MetadataReference.CreateFromFile(typeof(System.Linq.Enumerable).Assembly.Location),
               MetadataReference.CreateFromFile(typeof(System.Xml.XmlWriter).Assembly.Location),
               MetadataReference.CreateFromFile(typeof(Xcst.IXcstPackage).Assembly.Location),
               MetadataReference.CreateFromFile(Assembly.GetExecutingAssembly().Location)
            };

            CSharpCompilation compilation = CSharpCompilation.Create(
               Path.GetRandomFileName(),
               syntaxTrees: syntaxTrees,
               references: references,
               options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            using (var assemblyStream = new MemoryStream()) {

               EmitResult csharpResult = compilation.Emit(assemblyStream);

               if (!csharpResult.Success) {

                  Diagnostic error = csharpResult.Diagnostics
                     .Where(d => d.IsWarningAsError || d.Severity == DiagnosticSeverity.Error)
                     .FirstOrDefault();

                  throw new ArgumentException($"{error?.Id}: {error?.GetMessage() ?? "C# compilation failed."}", nameof(fileName));

               } else {

                  assemblyStream.Position = 0;

                  Assembly assembly = Assembly.Load(assemblyStream.ToArray());
                  Type type = assembly.GetType(compiler.TargetNamespace + "." + compiler.TargetClass);

                  return type;
               }
            }
         }
      }

      public static bool OutputEqualsToDoc(Type module, string fileName) {

         // TODO: XNode.DeepEquals has quirks <http://blogs.msdn.com/b/ericwhite/archive/2009/01/28/equality-semantics-of-linq-to-xml-trees.aspx>
         // use fn:deep-equals instead ?

         XDocument comparingDoc = XDocument.Load(fileName, LoadOptions.PreserveWhitespace);
         XDocument outputDoc = new XDocument();

         using (XmlWriter outputWriter = outputDoc.CreateWriter()) {

            XcstEvaluator.Using(module)
               .CallInitialTemplate()
               .OutputTo(outputWriter)
               .Run();
         }

         return XNode.DeepEquals(comparingDoc, outputDoc);
      }
   }
}
