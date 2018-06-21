using System;
using System.IO;
using System.Linq;
using System.Xml;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Xcst.Compiler;

namespace Xcst.Tests.ProgramStructure.Packages.AcceptingComponents {

   [TestClass]
   public partial class AcceptingComponentsTests {

      const string TestCategory = nameof(ProgramStructure) + "." + nameof(Packages) + "." + nameof(AcceptingComponents);

      [TestMethod, TestCategory(TestCategory)]
      public void Component_Using_Accepted_Type() {

         var compilerA = TestsHelper.CreateCompiler();
         compilerA.TargetClass = "FooPackage";
         compilerA.TargetNamespace = typeof(AcceptingComponentsTests).Namespace;
         compilerA.PackageLocationResolver = name => new Uri("urn:x:" + name);
         compilerA.ModuleResolver = new StringModuleResolver1();

         var usingPackageUri = new Uri(@"c:\foo.xcst");

         CompileResult resultA = compilerA.Compile(
            new StringReader(StringModuleResolver1.GetPackageString("")),
            baseUri: usingPackageUri
         );

         var compilerB = TestsHelper.CreateCompiler();
         compilerB.PackageLocationResolver = compilerA.PackageLocationResolver;
         compilerB.ModuleResolver = compilerA.ModuleResolver;

         CompileResult resultB = compilerB.Compile(
            new StringReader(StringModuleResolver1.GetPackageString("localhost.PackageB")),
            baseUri: compilerB.PackageLocationResolver("localhost.PackageB")
         );

         var compilerC = TestsHelper.CreateCompiler();
         compilerC.PackageLocationResolver = compilerA.PackageLocationResolver;
         compilerC.ModuleResolver = compilerA.ModuleResolver;

         CompileResult resultC = compilerC.Compile(
            new StringReader(StringModuleResolver1.GetPackageString("localhost.PackageC")),
            baseUri: compilerC.PackageLocationResolver("localhost.PackageC")
         );

         string[] compilationUnits = resultC.CompilationUnits
            .Concat(resultB.CompilationUnits)
            .Concat(resultA.CompilationUnits)
            .ToArray();

         TestsHelper.CompileCode(
            compilationUnits,
            compilerA.TargetNamespace + "." + compilerA.TargetClass,
            usingPackageUri
         );

         foreach (string unit in compilationUnits) {
            Console.WriteLine(unit);
         }
      }

      class StringModuleResolver1 : XmlResolver {

         public static string GetPackageString(string name) {

            switch (name) {
               case "":
                  return @"
<c:package version='1.0' language='C#' xmlns:c='http://maxtoroq.github.io/XCST'>
   <c:use-package name='localhost.PackageB'/>
</c:package>
";
               case "localhost.PackageB":
                  return @"
<c:package name='localhost.PackageB' version='1.0' language='C#' xmlns:c='http://maxtoroq.github.io/XCST'>
   <c:use-package name='localhost.PackageC'/>
   <c:template name='bar' as='Foo' visibility='public'>
      <c:object value='default(Foo)'/>
   </c:template>
</c:package>
";
               case "localhost.PackageC":
                  return @"
<c:package name='localhost.PackageC' version='1.0' language='C#' xmlns:c='http://maxtoroq.github.io/XCST'>
   <c:type name='Foo' visibility='public'/>
</c:package>
";

               default:
                  return null;
            }
         }

         public override object GetEntity(Uri absoluteUri, string role, System.Type ofObjectToReturn) {
            return CreateStreamForString(GetPackageString(absoluteUri.AbsoluteUri.Substring("urn:x:".Length)));
         }

         static Stream CreateStreamForString(string s) {

            var stream = new MemoryStream();
            var writer = new StreamWriter(stream);

            writer.Write(s);
            writer.Flush();

            stream.Position = 0;

            return stream;
         }
      }
   }
}
