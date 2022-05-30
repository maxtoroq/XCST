using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.VisualBasic;
using Xcst.Compiler;
using CSharpVersion = Microsoft.CodeAnalysis.CSharp.LanguageVersion;
using VBVersion = Microsoft.CodeAnalysis.VisualBasic.LanguageVersion;
using TestAssert = NUnit.Framework.Assert;
using TestAssertException = NUnit.Framework.AssertionException;
using Xcst.PackageModel;

namespace Xcst.Tests;

static class TestsHelper {

   const bool
   _printCode = false;

   static readonly string
   _initialName = "Q{" + XmlNamespaces.Xcst + "}initial-template";

   static readonly string
   _expectedName = "expected";

   public static void
   RunXcstTest(
         string packageFile, string testName, string testNamespace, bool correct, bool error, bool fail,
         decimal languageVersion = -1m, string? disableWarning = null, string? warningAsError = null,
         Type? extension = default) {

      var printCode = _printCode;
      var packageUri = new Uri(packageFile, UriKind.Absolute);

      CompileResult xcstResult;
      string packageName;

      try {
         var codegenResult = GenerateCode(packageUri, testName, testNamespace, extension);

         if (!correct) {
            // did not fail, caller Assert.Throws will
            return;
         }

         xcstResult = codegenResult.result;
         packageName = codegenResult.packageName;

      } catch (RuntimeException ex) {

         dynamic? errorData = ex.ErrorData;

         Console.WriteLine($"// {ex.Message}");
         Console.WriteLine($"// Module URI: {errorData?.ModuleUri}");
         Console.WriteLine($"// Line number: {errorData?.LineNumber}");

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

         var packageDir = Path.GetDirectoryName(packageFile)!;
         var packageFileWithoutExt = Path.GetFileNameWithoutExtension(Path.GetFileNameWithoutExtension(packageFile));

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
               warningAsError,
               printCode
            );

            if (!correct) {
               // did not fail, caller Assert.Throws will
               return;
            }

         } catch (ApplicationException) when (correct) {

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

            Console.WriteLine($"// {ex.Message}");

            if (!fail) {
               printCode = true;
            }

            throw;

         } catch (TestAssertException) {

            printCode = true;
            throw;
         }

      } finally {

         if (printCode) {
            foreach (var unit in xcstResult.CompilationUnits) {
               Console.WriteLine(unit);
            }
         }
      }
   }

   public static XcstCompiler
   CreateCompiler(Type? extension = default) {

      var compiler = new XcstCompiler();
      compiler.TargetRuntime = 2m;

      if (extension != null) {
         compiler.RegisterExtension((IXcstPackage)Activator.CreateInstance(extension)!);
      }

      compiler.UseLineDirective = true;
      //compiler.PackageTypeResolver = n => Assembly.GetExecutingAssembly().GetType(n);
      compiler.AddPackageLibrary(Assembly.GetExecutingAssembly().Location);

      return compiler;
   }

   static (CompileResult result, string packageName)
   GenerateCode(Uri packageUri, string testName, string testNamespace, Type? extension) {

      var compiler = CreateCompiler(extension);
      compiler.TargetNamespace = testNamespace;
      compiler.TargetClass = testName;
      compiler.UsePackageBase = testNamespace;
      compiler.SetTargetBaseTypes(typeof(TestBase));

      compiler.NullableAnnotate = true;
      compiler.NullableContext = "enable";

      var result = compiler.Compile(packageUri);

      return (result, compiler.TargetNamespace + "." + compiler.TargetClass);
   }

   public static Type
   CompileCode(
         string packageName, Uri packageUri, IEnumerable<string> compilationUnits, string language,
         bool error = false, decimal languageVersion = -1m, string? disableWarning = null, string? warningAsError = null, bool printCode = false) {

      var isCSharp = language.Equals("C#", StringComparison.OrdinalIgnoreCase);

      var csOptions = new CSharpParseOptions(
         CSharpVersionEnum((isCSharp) ? languageVersion : -1m),
         preprocessorSymbols: new[] { "DEBUG", "TRACE" });

      var vbOptions = new VisualBasicParseOptions(
         VBVersionEnum((!isCSharp) ? languageVersion : -1m),
         preprocessorSymbols: new[] { "DEBUG", "TRACE" }.ToDictionary(s => s, s => (object)String.Empty));

      var syntaxTrees = compilationUnits
         .Select(c => (isCSharp) ?
            CSharpSyntaxTree.ParseText(c, csOptions, path: packageUri.LocalPath, encoding: Encoding.UTF8)
            : VisualBasicSyntaxTree.ParseText(c, vbOptions, path: packageUri.LocalPath, encoding: Encoding.UTF8))
         .ToArray();

      // See <https://stackoverflow.com/a/47196516/39923>
      // The location of the .NET assemblies
      var assemblyPath = Path.GetDirectoryName(typeof(object).Assembly.Location)!;

      MetadataReference[] references = {
         // XCST dependencies
         MetadataReference.CreateFromFile(Path.Combine(assemblyPath, "mscorlib.dll")),
         MetadataReference.CreateFromFile(Path.Combine(assemblyPath, "System.dll")),
         MetadataReference.CreateFromFile(Path.Combine(assemblyPath, "System.Collections.dll")),
         MetadataReference.CreateFromFile(Path.Combine(assemblyPath, "System.Core.dll")),
         MetadataReference.CreateFromFile(Path.Combine(assemblyPath, "System.Runtime.dll")),
         MetadataReference.CreateFromFile(Path.Combine(assemblyPath, "System.Runtime.Extensions.dll")),
         MetadataReference.CreateFromFile(Path.Combine(assemblyPath, "System.Xml.ReaderWriter.dll")),
         MetadataReference.CreateFromFile(Path.Combine(assemblyPath, "System.Xml.XDocument.dll")),
         MetadataReference.CreateFromFile(Path.Combine(assemblyPath, "netstandard.dll")),
         MetadataReference.CreateFromFile(typeof(System.Object).Assembly.Location),
         MetadataReference.CreateFromFile(typeof(System.Uri).Assembly.Location),
         MetadataReference.CreateFromFile(typeof(System.Collections.Generic.List<>).Assembly.Location),
         MetadataReference.CreateFromFile(typeof(System.Linq.Enumerable).Assembly.Location),
         MetadataReference.CreateFromFile(typeof(System.Xml.XmlWriter).Assembly.Location),
         MetadataReference.CreateFromFile(typeof(System.Xml.Linq.XDocument).Assembly.Location),
         MetadataReference.CreateFromFile(typeof(System.Diagnostics.Trace).Assembly.Location),
         MetadataReference.CreateFromFile(typeof(System.IServiceProvider).Assembly.Location),
         MetadataReference.CreateFromFile(typeof(System.ComponentModel.DescriptionAttribute).Assembly.Location),
         MetadataReference.CreateFromFile(typeof(System.ComponentModel.DataAnnotations.ValidationAttribute).Assembly.Location),
         MetadataReference.CreateFromFile(typeof(Newtonsoft.Json.JsonWriter).Assembly.Location),
         MetadataReference.CreateFromFile(typeof(Xcst.PackageModel.IXcstPackage).Assembly.Location),
         // Tests dependencies
         MetadataReference.CreateFromFile(Path.Combine(assemblyPath, "System.Dynamic.Runtime.dll")),
         MetadataReference.CreateFromFile(Path.Combine(assemblyPath, "System.Linq.Expressions.dll")),
         MetadataReference.CreateFromFile(Path.Combine(assemblyPath, "System.ObjectModel.dll")),
         MetadataReference.CreateFromFile(typeof(Microsoft.CSharp.RuntimeBinder.RuntimeBinderException).Assembly.Location),
         MetadataReference.CreateFromFile(typeof(Microsoft.VisualBasic.Constants).Assembly.Location),
         MetadataReference.CreateFromFile(typeof(TestAssert).Assembly.Location),
         MetadataReference.CreateFromFile(typeof(System.IO.File).Assembly.Location),
         MetadataReference.CreateFromFile(typeof(System.Data.DataTable).Assembly.Location),
         MetadataReference.CreateFromFile(Assembly.GetExecutingAssembly().Location)
      };

      var specificDiagnosticOptions = ((disableWarning != null) ?
         disableWarning.Split(' ').Select(p => new KeyValuePair<string, ReportDiagnostic>(p, ReportDiagnostic.Suppress)).ToArray()
         : Array.Empty<KeyValuePair<string, ReportDiagnostic>>())
         .Concat(((warningAsError != null) ?
            warningAsError.Split(' ').Select(p => new KeyValuePair<string, ReportDiagnostic>(p, ReportDiagnostic.Error)).ToArray()
            : Array.Empty<KeyValuePair<string, ReportDiagnostic>>()));

      var compilation = (isCSharp) ?
         (Compilation)CSharpCompilation.Create(
            Path.GetRandomFileName(),
            syntaxTrees: syntaxTrees,
            references: references,
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary,
               specificDiagnosticOptions: specificDiagnosticOptions
                  .Append(new KeyValuePair<string, ReportDiagnostic>("CS1701", ReportDiagnostic.Suppress))))
         : VisualBasicCompilation.Create(
            Path.GetRandomFileName(),
            syntaxTrees: syntaxTrees,
            references: references,
            options: new VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary,
               specificDiagnosticOptions: specificDiagnosticOptions));

      using var assemblyStream = new MemoryStream();
      using var pdbStream = new MemoryStream();

      var codeResult = compilation.Emit(assemblyStream, pdbStream);

      var failed = codeResult.Diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error
         || (d.Severity == DiagnosticSeverity.Warning && d.WarningLevel > 1));

      if (printCode || failed) {

         foreach (var item in codeResult.Diagnostics.Where(d => d.Severity != DiagnosticSeverity.Hidden)) {
            var lineSpan = item.Location.GetLineSpan();
            Console.WriteLine($"// ({lineSpan.StartLinePosition.Line},{lineSpan.StartLinePosition.Character}) {item.Severity} {item.Id}: {item.GetMessage()}");
         }
      }

      if (failed) {

         if (error) {
            TestAssert.Pass($"{language} compilation failed.");
         }

         throw new ApplicationException($"{language} compilation failed.");
      }

      assemblyStream.Position = 0;
      pdbStream.Position = 0;

      var assembly = Assembly.Load(assemblyStream.ToArray(), pdbStream.ToArray());
      var type = assembly.GetType(packageName)!;

      return type;
   }

   static CSharpVersion
   CSharpVersionEnum(decimal languageVersion) =>
      languageVersion switch {
         -1m => CSharpVersion.CSharp8,
         7m => CSharpVersion.CSharp8,
         7.1m => CSharpVersion.CSharp8,
         7.2m => CSharpVersion.CSharp8,
         7.3m => CSharpVersion.CSharp8,
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

      using (var output = actualDoc.CreateWriter()) {

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

      var evaluator = XcstEvaluator.Using(Activator.CreateInstance(packageType)!);

      using (var actualWriter = actualDoc.CreateWriter()) {

         evaluator.CallInitialTemplate()
            .OutputTo(actualWriter)
            .WithBaseUri(packageUri)
            .WithBaseOutputUri(packageUri)
            .Run();
      }

      using (var expectedWriter = expectedDoc.CreateWriter()) {

         evaluator.CallTemplate(_expectedName)
            .OutputTo(expectedWriter)
            .Run();
      }

      var normalizedExpected = XDocumentNormalizer.Normalize(expectedDoc);
      var normalizedActual = XDocumentNormalizer.Normalize(actualDoc);
      var equals = XNode.DeepEquals(normalizedExpected, normalizedActual);

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
