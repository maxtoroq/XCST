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
using TestAssert = Microsoft.VisualStudio.TestTools.UnitTesting.Assert;

namespace Xcst.Compiler.Tests.Language {

   static class LanguageTestsHelper {

      static readonly XcstCompilerFactory CompilerFactory = new XcstCompilerFactory {
         EnableExtensions = true,
         PackageTypeResolver = (typeName) => Assembly.GetExecutingAssembly()
            .GetType(typeName)
      };

      static readonly QualifiedName InitialName = new QualifiedName("initial-template", "http://maxtoroq.github.io/XCST");
      static readonly QualifiedName ExpectedName = new QualifiedName("expected");

      public static void RunXcstTest(string packageFile, bool correct, bool fail) {

         string usePackageBase = new StackFrame(1, true).GetMethod().DeclaringType.Namespace;

         CompileResult xcstResult;
         string packageName;

         try {
            var codegenResult = GenerateCode(packageFile, usePackageBase);
            xcstResult = codegenResult.Item1;
            packageName = codegenResult.Item2;

         } catch (CompileException ex) {

            Console.WriteLine($"// {ex.Message}");
            Console.WriteLine($"// Module URI: {ex.ModuleUri}");
            Console.WriteLine($"// Line number: {ex.LineNumber}");

            throw;
         }

         try {

            Type packageType = CompileCode(xcstResult, packageName);

            if (!correct) {
               return;
            }

            try {

               if (fail) {

                  if (!xcstResult.Templates.Contains(InitialName)) {
                     TestAssert.Fail("A failing package should define an initial template.");
                  } else if (xcstResult.Templates.Contains(ExpectedName)) {
                     TestAssert.Fail("A failing package should not define an 'expected' template.");
                  }

                  SimplyRun(packageType);

               } else {

                  string packageDir = Path.GetDirectoryName(packageFile);
                  string packageFileWithoutExt = Path.GetFileNameWithoutExtension(Path.GetFileNameWithoutExtension(packageFile));
                  string outputFileXml = Path.Combine(packageDir, packageFileWithoutExt + ".xml");
                  string outputFileTxt = Path.Combine(packageDir, packageFileWithoutExt + ".txt");

                  char outputOpt = File.Exists(outputFileXml) ? 'x'
                     : File.Exists(outputFileTxt) ? 't'
                     : ' ';

                  if (outputOpt != ' ') {

                     if (!xcstResult.Templates.Contains(InitialName)) {
                        TestAssert.Fail("When an output document exists the package should define an initial template.");
                     } else if (xcstResult.Templates.Contains(ExpectedName)) {
                        TestAssert.Fail("When an output document exists the package should not define an 'expected' template.");
                     }

                     switch (outputOpt) {
                        case 'x':
                           TestAssert.IsTrue(OutputEqualsToDoc(packageType, outputFileXml));
                           break;
                        case 't':
                           TestAssert.IsTrue(OutputEqualsToText(packageType, outputFileTxt));
                           break;
                     }

                  } else {

                     if (xcstResult.Templates.Contains(InitialName)) {

                        if (xcstResult.Templates.Contains(ExpectedName)) {
                           TestAssert.IsTrue(OutputEqualsToExpected(packageType));
                        } else {
                           SimplyRun(packageType);
                        }

                     } else if (xcstResult.Templates.Contains(ExpectedName)) {
                        TestAssert.Fail("A package that defines an 'expected' template without an initial template makes no sense.");
                     }
                  }
               }

            } catch (RuntimeException ex) {

               Console.WriteLine($"// {ex.Message}");
               throw;
            }

         } finally {

            foreach (string unit in xcstResult.CompilationUnits) {
               Console.WriteLine(unit);
            }
         }
      }

      static Tuple<CompileResult, string> GenerateCode(string packageFile, string usePackageBase) {

         XcstCompiler compiler = CompilerFactory.CreateCompiler();
         compiler.TargetNamespace = typeof(LanguageTestsHelper).Namespace + ".Runtime";
         compiler.TargetClass = "TestModule";
         compiler.UseLineDirective = true;
         compiler.UsePackageBase = usePackageBase;
         compiler.SetTargetBaseTypes(typeof(TestBase));

         CompileResult result = compiler.Compile(new Uri(packageFile, UriKind.Absolute));

         return Tuple.Create(result, compiler.TargetNamespace + "." + compiler.TargetClass);
      }

      static Type CompileCode(CompileResult result, string packageName) {

         var parseOptions = new CSharpParseOptions(preprocessorSymbols: new[] { "DEBUG", "TRACE" });

         SyntaxTree[] syntaxTrees = result.CompilationUnits
            .Select(c => CSharpSyntaxTree.ParseText(c, parseOptions))
            .ToArray();

         // TODO: Should compiler give list of assembly references?

         MetadataReference[] references = {
            MetadataReference.CreateFromFile(typeof(System.Object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(System.Uri).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(System.Linq.Enumerable).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(System.Xml.XmlWriter).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(System.ComponentModel.DataAnnotations.ValidationAttribute).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Newtonsoft.Json.JsonWriter).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Xcst.PackageModel.IXcstPackage).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Microsoft.CSharp.RuntimeBinder.RuntimeBinderException).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Microsoft.VisualStudio.TestTools.UnitTesting.Assert).Assembly.Location),
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

               if (error != null) {
                  Console.WriteLine($"// {error.Id}: {error.GetMessage()}");
                  Console.WriteLine($"// Line number: {error.Location.GetLineSpan().StartLinePosition.Line}");
               }

               throw new CompileException("C# compilation failed.");
            }

            assemblyStream.Position = 0;

            Assembly assembly = Assembly.Load(assemblyStream.ToArray());
            Type type = assembly.GetType(packageName);

            return type;
         }
      }

      static bool OutputEqualsToDoc(Type packageType, string fileName) {

         var expectedDoc = XDocument.Load(fileName, LoadOptions.PreserveWhitespace);
         var actualDoc = new XDocument();

         using (XmlWriter output = actualDoc.CreateWriter()) {

            XcstEvaluator.Using(Activator.CreateInstance(packageType))
               .CallInitialTemplate()
               .OutputTo(output)
               .Run();
         }

         return XDocumentNormalizer.DeepEqualsWithNormalization(expectedDoc, actualDoc);
      }

      static bool OutputEqualsToText(Type packageType, string fileName) {

         string result;

         using (var output = new StringWriter()) {

            XcstEvaluator.Using(Activator.CreateInstance(packageType))
               .CallInitialTemplate()
               .OutputTo(output)
               .Run();

            result = output.ToString();
         }

         return String.Equals(result, File.ReadAllText(fileName));
      }

      static bool OutputEqualsToExpected(Type packageType) {

         var expectedDoc = new XDocument();
         var actualDoc = new XDocument();

         XcstEvaluator evaluator = XcstEvaluator.Using(Activator.CreateInstance(packageType));

         using (XmlWriter actualWriter = actualDoc.CreateWriter()) {

            evaluator.CallInitialTemplate()
               .OutputTo(actualWriter)
               .Run();
         }

         using (XmlWriter expectedWriter = expectedDoc.CreateWriter()) {

            evaluator.CallTemplate("expected")
               .OutputTo(expectedWriter)
               .Run();
         }

         return XDocumentNormalizer.DeepEqualsWithNormalization(expectedDoc, actualDoc);
      }

      static void SimplyRun(Type packageType) {

         XcstEvaluator.Using(Activator.CreateInstance(packageType))
            .CallInitialTemplate()
            .OutputTo(TextWriter.Null)
            .Run();
      }
   }

   public abstract class TestBase {

      protected Type CompileType<T>(T obj) {
         return typeof(T);
      }

      public static class Assert {

         public static void IsTrue(bool condition) {
            TestAssert.IsTrue(condition);
         }

         public static void IsFalse(bool condition) {
            TestAssert.IsFalse(condition);
         }

         public static void AreEqual<T>(T expected, T actual) {
            TestAssert.AreEqual(expected, actual);
         }

         public static void IsNull(object value) {
            TestAssert.IsNull(value);
         }
      }
   }
}
