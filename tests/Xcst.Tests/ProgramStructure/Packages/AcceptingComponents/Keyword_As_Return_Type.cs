using System;
using System.IO;
using System.Linq;
using System.Xml;
using NUnit.Framework;

namespace Xcst.Tests.ProgramStructure.Packages.AcceptingComponents {

   using ModuleResolver = AcceptingComponentsTests.Keyword_As_Return_Type_Resolver;

   public partial class AcceptingComponentsTests {

      [Test]
      [Category(TestCategory)]
      public void
      Keyword_As_Return_Type() {

         var compilerA = TestsHelper.CreateCompiler();
         compilerA.TargetClass = "FooPackage";
         compilerA.TargetNamespace = typeof(AcceptingComponentsTests).Namespace;
         compilerA.PackageLocationResolver = name => new Uri("urn:x:" + name);
         compilerA.ModuleResolver = new ModuleResolver();

         var usingPackageUri = new Uri(@"c:\foo.xcst");

         var resultA = compilerA.Compile(
            new StringReader(ModuleResolver.GetPackageString("")),
            baseUri: usingPackageUri
         );

         var compilerB = TestsHelper.CreateCompiler();
         compilerB.PackageLocationResolver = compilerA.PackageLocationResolver;
         compilerB.ModuleResolver = compilerA.ModuleResolver;

         var resultB = compilerB.Compile(
            new StringReader(ModuleResolver.GetPackageString("localhost.PackageB")),
            baseUri: compilerB.PackageLocationResolver("localhost.PackageB")
         );

         var compilationUnits = resultB.CompilationUnits
            .Concat(resultA.CompilationUnits)
            .ToArray();

         TestsHelper.CompileCode(
            compilerA.TargetNamespace + "." + compilerA.TargetClass,
            usingPackageUri,
            compilationUnits,
            resultA.Language
         );
      }

      internal class Keyword_As_Return_Type_Resolver : XmlResolver {

         public static string
         GetPackageString(string name) {

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
   
   <c:param name='a' as='string'/>
   <c:variable name='b' as='string' visibility='public'/>

   <c:function name='c' as='string' visibility='public'>
      <c:return value='default(string)'/>
   </c:function>

   <c:template name='d' as='string' visibility='public'>d</c:template>

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
}
