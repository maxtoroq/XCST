using System;
using System.IO;
using System.Linq;
using System.Xml;
using NUnit.Framework;
using Xcst.Compiler;

namespace Xcst.Tests.ProgramStructure.Packages.OverridingComponents.Variable {

   using ModuleResolver = VariableTests.Overridden_Non_Compiled_Resolver;

   partial class VariableTests {

      const string
      TestCategory = nameof(ProgramStructure) + "." + nameof(Packages) + "." + nameof(OverridingComponents) + "." + nameof(Variable);

      [Test]
      [Category(TestCategory)]
      public void
      Overridden_Non_Compiled() {

         var compilerA = TestsHelper.CreateCompiler();
         compilerA.TargetClass = "FooPackage";
         compilerA.TargetNamespace = typeof(VariableTests).Namespace;
         compilerA.PackageLocationResolver = name => new Uri("urn:x:" + name);
         compilerA.ModuleResolver = new ModuleResolver();

         var usingPackageUri = new Uri(@"c:\foo.xcst");

         CompileResult resultA = compilerA.Compile(
            new StringReader(ModuleResolver.GetPackageString("")),
            baseUri: usingPackageUri
         );

         var compilerB = TestsHelper.CreateCompiler();
         compilerB.PackageLocationResolver = compilerA.PackageLocationResolver;
         compilerB.ModuleResolver = compilerA.ModuleResolver;

         CompileResult resultB = compilerB.Compile(
            new StringReader(ModuleResolver.GetPackageString("localhost.PackageB")),
            baseUri: compilerB.PackageLocationResolver("localhost.PackageB")
         );

         string[] compilationUnits = resultB.CompilationUnits
            .Concat(resultA.CompilationUnits)
            .ToArray();

         TestsHelper.CompileCode(
            compilerA.TargetNamespace + "." + compilerA.TargetClass,
            usingPackageUri,
            compilationUnits,
            resultA.Language
         );

         foreach (string unit in compilationUnits) {
            Console.WriteLine(unit);
         }
      }

      internal class Overridden_Non_Compiled_Resolver : XmlResolver {

         public static string
         GetPackageString(string name) {

            switch (name) {
               case "":
                  return @"
<c:package version='1.0' language='C#' xmlns:c='http://maxtoroq.github.io/XCST'>
   <c:use-package name='localhost.PackageB'>
      <c:override>
         <c:variable name='foo' value='""bar""' as='string'/>
      </c:override>
   </c:use-package>
</c:package>
";
               case "localhost.PackageB":
                  return @"
<c:package name='localhost.PackageB' version='1.0' language='C#' xmlns:c='http://maxtoroq.github.io/XCST'>
   <c:import-namespace ns='System'/>
   <c:variable name='foo' value='""foo""' as='String' visibility='public'/>
</c:package>
";
               default:
                  throw new ArgumentException("Invalid name.", nameof(name));
            }
         }

         public override object
         GetEntity(Uri absoluteUri, string role, System.Type ofObjectToReturn) =>
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
