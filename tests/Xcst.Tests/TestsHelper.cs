﻿using System;
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
using TestAssertException = NUnit.Framework.AssertionException;

namespace Xcst.Tests {

   static class TestsHelper {

      const bool
      _printCode = false;

      static readonly XcstCompilerFactory
      _compilerFactory = new XcstCompilerFactory {
         EnableExtensions = true
      };

      static readonly string
      _initialName = "Q{http://maxtoroq.github.io/XCST}initial-template";

      static readonly string
      _expectedName = "expected";

      public static void
      RunXcstTest(
            string packageFile, string testName, string testNamespace, bool correct, bool error, bool fail,
            decimal languageVersion = -1m, string? disableWarning = null) {

         bool printCode = _printCode;
         var packageUri = new Uri(packageFile, UriKind.Absolute);

         CompileResult xcstResult;
         string packageName;

         try {
            var codegenResult = GenerateCode(packageUri, testName, testNamespace);

            if (!correct) {
               // did not fail, caller Assert.Throws will
               return;
            }

            xcstResult = codegenResult.result;
            packageName = codegenResult.packageName;

         } catch (CompileException ex) when (printCode) {

            Console.WriteLine($"// {ex.Message}");
            Console.WriteLine($"// Module URI: {ex.ModuleUri}");
            Console.WriteLine($"// Line number: {ex.LineNumber}");

            throw;
         }

         string? outputFileXml, outputFileTxt;
         char outputOpt;

         if (fail) {

            if (!xcstResult.Templates.Contains(_initialName)) {
               TestAssert.Fail("A failing package should define an initial template.");
            } else if (xcstResult.Templates.Contains(_expectedName)) {
               TestAssert.Fail("A failing package should not define an 'expected' template.");
            }

            outputFileXml = null;
            outputFileTxt = null;
            outputOpt = ' ';

         } else {

            string packageDir = Path.GetDirectoryName(packageFile)!;
            string packageFileWithoutExt = Path.GetFileNameWithoutExtension(Path.GetFileNameWithoutExtension(packageFile));

            outputFileXml = Path.Combine(packageDir, packageFileWithoutExt + ".xml");
            outputFileTxt = Path.Combine(packageDir, packageFileWithoutExt + ".txt");

            outputOpt = File.Exists(outputFileXml) ? 'x'
               : File.Exists(outputFileTxt) ? 't'
               : ' ';

            if (outputOpt != ' ') {

               if (!xcstResult.Templates.Contains(_initialName)) {
                  TestAssert.Fail("When an output document exists the package should define an initial template.");
               } else if (xcstResult.Templates.Contains(_expectedName)) {
                  TestAssert.Fail("When an output document exists the package should not define an 'expected' template.");
               }

            } else if (xcstResult.Templates.Contains(_expectedName)
               && !xcstResult.Templates.Contains(_initialName)) {

               TestAssert.Fail("A package that defines an 'expected' template without an initial template makes no sense.");
            }
         }

         try {

            Type packageType;

            try {

               packageType = CompileCode(
                  packageName,
                  packageUri,
                  xcstResult.CompilationUnits,
                  xcstResult.Language,
                  error,
                  languageVersion,
                  disableWarning,
                  printCode
               );

               if (!correct) {
                  // did not fail, caller Assert.Throws will
                  return;
               }

            } catch (CompileException) when (correct) {

               printCode = true;
               throw;
            }

            try {

               if (fail) {

                  SimplyRun(packageType, packageUri);

                  // did not fail, print code
                  printCode = true;

               } else if (outputOpt != ' ') {

                  switch (outputOpt) {
                     case 'x':
                        TestAssert.IsTrue(OutputEqualsToDoc(packageType, packageUri, outputFileXml!));
                        break;
                     case 't':
                        TestAssert.IsTrue(OutputEqualsToText(packageType, packageUri, outputFileTxt!));
                        break;
                  }

               } else if (xcstResult.Templates.Contains(_initialName)) {

                  if (xcstResult.Templates.Contains(_expectedName)) {
                     TestAssert.IsTrue(OutputEqualsToExpected(packageType, packageUri, printCode));
                  } else {
                     SimplyRun(packageType, packageUri);
                  }
               }

            } catch (RuntimeException ex) {

               if (printCode) {
                  Console.WriteLine($"// {ex.Message}");
               } else if (!fail) {
                  printCode = true;
               }

               throw;

            } catch (TestAssertException) {

               printCode = true;
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

         XcstCompiler compiler = _compilerFactory.CreateCompiler();
         compiler.UseLineDirective = true;
         compiler.PackageTypeResolver = n => Assembly.GetExecutingAssembly().GetType(n);
         //compiler.PackageTypeResolver = n => null;
         //compiler.AddPackageLibrary(Assembly.GetExecutingAssembly().Location);

         return compiler;
      }

      static (CompileResult result, string packageName)
      GenerateCode(Uri packageUri, string testName, string testNamespace) {

         XcstCompiler compiler = CreateCompiler();
         compiler.TargetNamespace = testNamespace;
         compiler.TargetClass = testName;
         compiler.UsePackageBase = testNamespace;
         compiler.SetTargetBaseTypes(typeof(TestBase));

         CompileResult result = compiler.Compile(packageUri);

         return (result, compiler.TargetNamespace + "." + compiler.TargetClass);
      }

      public static Type
      CompileCode(
            string packageName, Uri packageUri, IEnumerable<string> compilationUnits, string language,
            bool error = false, decimal languageVersion = -1m, string? disableWarning = null, bool printCode = false) {

         bool isCSharp = language.Equals("C#", StringComparison.OrdinalIgnoreCase);

         var csOptions = new CSharpParseOptions(
            CSharpVersionEnum((isCSharp) ? languageVersion : -1m),
            preprocessorSymbols: new[] { "DEBUG", "TRACE" });

         var vbOptions = new VisualBasicParseOptions(
            VBVersionEnum((!isCSharp) ? languageVersion : -1m),
            preprocessorSymbols: new[] { "DEBUG", "TRACE" }.ToDictionary(s => s, s => (object)String.Empty));

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

         var specificDiagnosticOptions = (disableWarning != null) ?
            disableWarning.Split(' ').Select(p => new KeyValuePair<string, ReportDiagnostic>(p, ReportDiagnostic.Suppress)).ToArray()
            : Array.Empty<KeyValuePair<string, ReportDiagnostic>>();

         Compilation compilation = (isCSharp) ?
            (Compilation)CSharpCompilation.Create(
               Path.GetRandomFileName(),
               syntaxTrees: syntaxTrees,
               references: references,
               options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary,
                  specificDiagnosticOptions: specificDiagnosticOptions))
            : VisualBasicCompilation.Create(
               Path.GetRandomFileName(),
               syntaxTrees: syntaxTrees,
               references: references,
               options: new VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary,
                  specificDiagnosticOptions: specificDiagnosticOptions));

         using (var assemblyStream = new MemoryStream()) {
            using (var pdbStream = new MemoryStream()) {

               EmitResult codeResult = compilation.Emit(assemblyStream, pdbStream);

               bool failed = codeResult.Diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error
                  || (d.Severity == DiagnosticSeverity.Warning && d.WarningLevel > 1));

               if (printCode
                  || (failed && !error)) {

                  foreach (Diagnostic item in codeResult.Diagnostics.Where(d => d.Severity != DiagnosticSeverity.Hidden)) {
                     var lineSpan = item.Location.GetLineSpan();
                     Console.WriteLine($"// ({lineSpan.StartLinePosition.Line},{lineSpan.StartLinePosition.Character}) {item.Severity} {item.Id}: {item.GetMessage()}");
                  }
               }

               if (failed) {

                  if (error) {
                     TestAssert.Pass($"{language} compilation failed.");
                  }

                  throw new ArgumentException($"{language} compilation failed.");
               }

               assemblyStream.Position = 0;
               pdbStream.Position = 0;

               Assembly assembly = Assembly.Load(assemblyStream.ToArray(), pdbStream.ToArray());
               Type type = assembly.GetType(packageName)!;

               return type;
            }
         }
      }

      static CSharpVersion
      CSharpVersionEnum(decimal languageVersion) =>
         languageVersion switch {
            -1m => CSharpVersion.CSharp6,
            7m => CSharpVersion.CSharp7,
            7.1m => CSharpVersion.CSharp7_1,
            7.2m => CSharpVersion.CSharp7_2,
            7.3m => CSharpVersion.CSharp7_3,
            8m => CSharpVersion.CSharp8,
            9m => CSharpVersion.CSharp9,
            _ => throw new ArgumentOutOfRangeException(nameof(languageVersion))
         };

      static VBVersion
      VBVersionEnum(decimal languageVersion) =>
         languageVersion switch {
            -1m => VBVersion.VisualBasic14,
            15m => VBVersion.VisualBasic15,
            15.3m => VBVersion.VisualBasic15_3,
            15.5m => VBVersion.VisualBasic15_5,
            16m => VBVersion.VisualBasic16,
            _ => throw new ArgumentOutOfRangeException(nameof(languageVersion))
         };

      static bool
      OutputEqualsToDoc(Type packageType, Uri packageUri, string fileName) {

         var expectedDoc = XDocument.Load(fileName, LoadOptions.PreserveWhitespace);
         var actualDoc = new XDocument();

         using (XmlWriter output = actualDoc.CreateWriter()) {

            XcstEvaluator.Using(Activator.CreateInstance(packageType)!)
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

            XcstEvaluator.Using(Activator.CreateInstance(packageType)!)
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
      OutputEqualsToExpected(Type packageType, Uri packageUri, bool printCode) {

         var expectedDoc = new XDocument();
         var actualDoc = new XDocument();

         XcstEvaluator evaluator = XcstEvaluator.Using(Activator.CreateInstance(packageType)!);

         using (XmlWriter actualWriter = actualDoc.CreateWriter()) {

            evaluator.CallInitialTemplate()
               .OutputTo(actualWriter)
               .WithBaseUri(packageUri)
               .WithBaseOutputUri(packageUri)
               .Run();
         }

         using (XmlWriter expectedWriter = expectedDoc.CreateWriter()) {

            evaluator.CallTemplate(_expectedName)
               .OutputTo(expectedWriter)
               .Run();
         }

         XDocument normalizedExpected = XDocumentNormalizer.Normalize(expectedDoc);
         XDocument normalizedActual = XDocumentNormalizer.Normalize(actualDoc);
         bool equals = XNode.DeepEquals(normalizedExpected, normalizedActual);

         if (printCode || !equals) {
            Console.WriteLine("/*");
            Console.WriteLine("<!-- expected -->");
            Console.WriteLine(normalizedExpected.ToString());
            Console.WriteLine();
            Console.WriteLine("<!-- actual -->");
            Console.WriteLine(normalizedActual.ToString());
            Console.WriteLine("*/");
         }

         return equals;
      }

      static void
      SimplyRun(Type packageType, Uri packageUri) {

         XcstEvaluator.Using(Activator.CreateInstance(packageType)!)
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

         public static void
         IsNotNull(object value) => TestAssert.IsNotNull(value);

         public static void
         Fail() => TestAssert.Fail();
      }
   }
}
