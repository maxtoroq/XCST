using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.VisualBasic;
using Xcst.Compiler;
using CSharpVersion = Microsoft.CodeAnalysis.CSharp.LanguageVersion;
using VBVersion = Microsoft.CodeAnalysis.VisualBasic.LanguageVersion;
using TestAssert = NUnit.Framework.Assert;

namespace Xcst.Tests {

   static class TestsHelper {

      static readonly XcstCompilerFactory
      CompilerFactory = new XcstCompilerFactory {
         EnableExtensions = true
      };

      static readonly string
      InitialName = "Q{http://maxtoroq.github.io/XCST}initial-template";

      static readonly string
      ExpectedName = "expected";

      public static void
      RunXcstTest(string packageFile, string testName, string testNamespace, bool correct, bool fail) {

         var packageUri = new Uri(packageFile, UriKind.Absolute);

         CompileResult xcstResult;
         string packageName;

         try {
            var codegenResult = GenerateCode(packageUri, testName, testNamespace);
            xcstResult = codegenResult.Item1;
            packageName = codegenResult.Item2;

         } catch (CompileException ex) {

            if (correct) {
               Console.WriteLine($"// {ex.Message}");
               Console.WriteLine($"// Module URI: {ex.ModuleUri}");
               Console.WriteLine($"// Line number: {ex.LineNumber}");
            }

            throw;
         }

         bool printCode = false;

         try {

            Type packageType = CompileCode(packageName, packageUri, xcstResult.CompilationUnits, xcstResult.Language, correct);

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

                  SimplyRun(packageType, packageUri);

                  // did not fail, print code
                  printCode = true;

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
                           TestAssert.IsTrue(OutputEqualsToDoc(packageType, packageUri, outputFileXml));
                           break;
                        case 't':
                           TestAssert.IsTrue(OutputEqualsToText(packageType, packageUri, outputFileTxt));
                           break;
                     }

                  } else {

                     if (xcstResult.Templates.Contains(InitialName)) {

                        if (xcstResult.Templates.Contains(ExpectedName)) {

                           bool equals = OutputEqualsToExpected(packageType, packageUri);
                           printCode = !equals;

                           TestAssert.IsTrue(equals);

                        } else {
                           SimplyRun(packageType, packageUri);
                        }

                     } else if (xcstResult.Templates.Contains(ExpectedName)) {
                        TestAssert.Fail("A package that defines an 'expected' template without an initial template makes no sense.");
                     }
                  }
               }

            } catch (RuntimeException ex) {

               if (!fail) {
                  Console.WriteLine($"// {ex.Message}");
                  printCode = true;
               }

               throw;
            }

         } finally {

            if (printCode) {
               foreach (string unit in xcstResult.CompilationUnits) {
                  Console.WriteLine(unit);
               }
            }
         }
      }

      public static XcstCompiler
      CreateCompiler() {

         XcstCompiler compiler = CompilerFactory.CreateCompiler();
         compiler.UseLineDirective = true;
         compiler.PackageTypeResolver = n => Assembly.GetExecutingAssembly().GetType(n);

         return compiler;
      }

      static Tuple<CompileResult, string>
      GenerateCode(Uri packageUri, string testName, string testNamespace) {

         XcstCompiler compiler = CreateCompiler();
         compiler.TargetNamespace = testNamespace;
         compiler.TargetClass = testName;
         compiler.UsePackageBase = testNamespace;
         compiler.SetTargetBaseTypes(typeof(TestBase));

         CompileResult result = compiler.Compile(packageUri);

         return Tuple.Create(result, compiler.TargetNamespace + "." + compiler.TargetClass);
      }

      public static Type
      CompileCode(string packageName, Uri packageUri, IEnumerable<string> compilationUnits, string language, bool correct) {

         bool isCSharp = language.Equals("C#", StringComparison.OrdinalIgnoreCase);

         var csOptions = new CSharpParseOptions(CSharpVersion.CSharp6, preprocessorSymbols: new[] { "DEBUG", "TRACE" });
         var vbOptions = new VisualBasicParseOptions(VBVersion.VisualBasic14, preprocessorSymbols: new[] { "DEBUG", "TRACE" }.ToDictionary(s => s, s => (object)String.Empty));

         SyntaxTree[] syntaxTrees = compilationUnits
            .Select(c => (isCSharp) ?
               CSharpSyntaxTree.ParseText(c, csOptions, path: packageUri.LocalPath, encoding: Encoding.UTF8)
               : VisualBasicSyntaxTree.ParseText(c, vbOptions, path: packageUri.LocalPath, encoding: Encoding.UTF8))
            .ToArray();

         MetadataReference[] references = {
            // XCST dependencies
            MetadataReference.CreateFromFile(typeof(System.Object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(System.Uri).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(System.Linq.Enumerable).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(System.Xml.XmlWriter).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(System.Xml.Linq.XDocument).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(System.ComponentModel.DataAnnotations.ValidationAttribute).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Newtonsoft.Json.JsonWriter).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Xcst.PackageModel.IXcstPackage).Assembly.Location),
            // Tests dependencies
            MetadataReference.CreateFromFile(typeof(Microsoft.CSharp.RuntimeBinder.RuntimeBinderException).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Microsoft.VisualBasic.Constants).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(TestAssert).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(System.Data.DataTable).Assembly.Location),
            MetadataReference.CreateFromFile(Assembly.GetExecutingAssembly().Location)
         };

         Compilation compilation = (isCSharp) ?
            (Compilation)CSharpCompilation.Create(
               Path.GetRandomFileName(),
               syntaxTrees: syntaxTrees,
               references: references,
               options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
            : VisualBasicCompilation.Create(
               Path.GetRandomFileName(),
               syntaxTrees: syntaxTrees,
               references: references,
               options: new VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

         using (var assemblyStream = new MemoryStream()) {
            using (var pdbStream = new MemoryStream()) {

               EmitResult codeResult = compilation.Emit(assemblyStream, pdbStream);

               if (!codeResult.Success) {

                  Diagnostic error = codeResult.Diagnostics
                     .Where(d => d.IsWarningAsError || d.Severity == DiagnosticSeverity.Error)
                     .FirstOrDefault();

                  if (error != null
                     && correct) {

                     Console.WriteLine($"// {error.Id}: {error.GetMessage()}");
                     Console.WriteLine($"// Line number: {error.Location.GetLineSpan().StartLinePosition.Line}");
                  }

                  throw new CompileException($"{language} compilation failed.");
               }

               assemblyStream.Position = 0;
               pdbStream.Position = 0;

               Assembly assembly = Assembly.Load(assemblyStream.ToArray(), pdbStream.ToArray());
               Type type = assembly.GetType(packageName);

               return type;
            }
         }
      }

      static bool
      OutputEqualsToDoc(Type packageType, Uri packageUri, string fileName) {

         var expectedDoc = XDocument.Load(fileName, LoadOptions.PreserveWhitespace);
         var actualDoc = new XDocument();

         using (XmlWriter output = actualDoc.CreateWriter()) {

            XcstEvaluator.Using(Activator.CreateInstance(packageType))
               .CallInitialTemplate()
               .OutputTo(output)
               .WithBaseUri(packageUri)
               .WithBaseOutputUri(packageUri)
               .Run();
         }

         return XDocumentNormalizer.DeepEqualsWithNormalization(expectedDoc, actualDoc);
      }

      static bool
      OutputEqualsToText(Type packageType, Uri packageUri, string fileName) {

         string result;

         using (var output = new StringWriter()) {

            XcstEvaluator.Using(Activator.CreateInstance(packageType))
               .CallInitialTemplate()
               .OutputTo(output)
               .WithBaseUri(packageUri)
               .WithBaseOutputUri(packageUri)
               .Run();

            result = output.ToString();
         }

         return String.Equals(result, File.ReadAllText(fileName));
      }

      static bool
      OutputEqualsToExpected(Type packageType, Uri packageUri) {

         var expectedDoc = new XDocument();
         var actualDoc = new XDocument();

         XcstEvaluator evaluator = XcstEvaluator.Using(Activator.CreateInstance(packageType));

         using (XmlWriter actualWriter = actualDoc.CreateWriter()) {

            evaluator.CallInitialTemplate()
               .OutputTo(actualWriter)
               .WithBaseUri(packageUri)
               .WithBaseOutputUri(packageUri)
               .Run();
         }

         using (XmlWriter expectedWriter = expectedDoc.CreateWriter()) {

            evaluator.CallTemplate(ExpectedName)
               .OutputTo(expectedWriter)
               .Run();
         }

         XDocument normalizedExpected = XDocumentNormalizer.Normalize(expectedDoc);
         XDocument normalizedActual = XDocumentNormalizer.Normalize(actualDoc);
         bool equals = XNode.DeepEquals(normalizedExpected, normalizedActual);

         if (!equals) {
            Console.WriteLine("<!-- expected -->");
            Console.WriteLine(normalizedExpected.ToString());
            Console.WriteLine();
            Console.WriteLine("<!-- actual -->");
            Console.WriteLine(normalizedActual.ToString());
         }

         return equals;
      }

      static void
      SimplyRun(Type packageType, Uri packageUri) {

         XcstEvaluator.Using(Activator.CreateInstance(packageType))
            .CallInitialTemplate()
            .OutputTo(TextWriter.Null)
            .WithBaseUri(packageUri)
            .WithBaseOutputUri(packageUri)
            .Run();
      }
   }

   public abstract class TestBase {

      protected Type
      CompileType<T>(T obj) => typeof(T);

      public static class Assert {

         public static void
         IsTrue(bool condition) => TestAssert.IsTrue(condition);

         public static void
         IsFalse(bool condition) => TestAssert.IsFalse(condition);

         public static void
         AreEqual<T>(T expected, T actual) =>
            TestAssert.AreEqual(expected, actual);

         public static void
         IsNull(object value) => TestAssert.IsNull(value);
      }
   }
}
