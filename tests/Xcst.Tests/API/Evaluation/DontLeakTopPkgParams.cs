using System;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using NUnit.Framework;

namespace Xcst.Tests.API.Evaluation;

partial class EvaluationTests {

   [Test]
   [Category(TestCategory)]
   public void
   Dont_Leak_Top_Package_Params() {

      var compilerA = TestsHelper.CreateCompiler();
      compilerA.TargetClass = "FooPackage";
      compilerA.TargetNamespace = typeof(EvaluationTests).Namespace;
      compilerA.PackageLocationResolver = name => new Uri("urn:x:" + name);
      compilerA.ModuleResolver = new DontLeakTopPkgParamsResolver();

      var usingPackageUri = new Uri(@"c:\foo.xcst");

      var resultA = compilerA.Compile(
         new StringReader(DontLeakTopPkgParamsResolver.GetPackageString("")),
         baseUri: usingPackageUri);

      var compilerB = TestsHelper.CreateCompiler();
      compilerB.PackageLocationResolver = compilerA.PackageLocationResolver;
      compilerB.ModuleResolver = compilerA.ModuleResolver;

      var resultB = compilerB.Compile(
         new StringReader(DontLeakTopPkgParamsResolver.GetPackageString("localhost.PackageB")),
         baseUri: compilerB.PackageLocationResolver("localhost.PackageB"));

      var compilationUnits = resultB.CompilationUnits
         .Concat(resultA.CompilationUnits)
         .ToArray();

      var pkgType = TestsHelper.CompileCode(
         resultA.PackageName,
         usingPackageUri,
         compilationUnits,
         resultA.Language);

      var resultDoc = new XDocument();
      var resultWriter = resultDoc.CreateWriter();

      XcstEvaluator.Using(Activator.CreateInstance(pkgType)!)
         .WithParam("foo", "foo")
         .WithParam("bar", "bar")
         .CallInitialTemplate()
         .OutputTo(resultWriter)
         .Run();

      resultWriter.Close();

      Assert.AreEqual("foo", resultDoc.Root!.Value);
   }

   class DontLeakTopPkgParamsResolver : XmlResolver {

      public static string
      GetPackageString(string name) {

         switch (name) {
            case "":
               return @"
<c:module version='1.0' language='C#' xmlns:c='http://maxtoroq.github.io/XCST'>
   <c:use-package name='localhost.PackageB'/>
   <c:param name='foo' as='string'/>
   <c:template name='c:initial-template'>
      <output>
         <c:call-template name='my-tmpl'/>
         <c:value-of value='foo'/>
      </output>
   </c:template>
</c:module>
";
            case "localhost.PackageB":
               return @"
<c:package name='localhost.PackageB' version='1.0' language='C#' xmlns:c='http://maxtoroq.github.io/XCST'>
   <c:param name='bar' as='string'/>
   <c:template name='my-tmpl' visibility='final'>
      <c:return value='bar'/>
   </c:template>
</c:package>
";

            default:
               throw new ArgumentException("Invalid name.", nameof(name));
         }
      }

      public override object
      GetEntity(Uri absoluteUri, string? role, System.Type? ofObjectToReturn) =>
         CreateStreamForString(GetPackageString(absoluteUri.AbsoluteUri.Substring("urn:x:".Length)));

      static Stream
      CreateStreamForString(string s) {

         var stream = new MemoryStream();
         var writer = new StreamWriter(stream);

         writer.Write(s);
         writer.Flush();

         stream.Position = 0;

         return stream;
      }
   }
}
