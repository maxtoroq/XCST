using System;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Xcst.Compiler;

namespace Xcst.Tests.API.Compilation {

   [TestClass]
   public class CompilationTests {

      const string TestCategory = nameof(API) + "." + nameof(Compilation);

      [TestMethod, TestCategory(TestCategory)]
      public void CompileResult_Lists_Public_Templates_Only() {

         var compilerFactory = new XcstCompilerFactory();
         var compiler = compilerFactory.CreateCompiler();
         compiler.TargetClass = "FooPackage";
         compiler.TargetNamespace = typeof(CompilationTests).Namespace;

         var module = new StringReader(@"
<c:package version='1.0' language='C#' xmlns:c='http://maxtoroq.github.io/XCST'>
   <c:template name='private' visibility='private'/>
   <c:template name='public' visibility='public'/>
   <c:template name='final' visibility='final'/>
   <c:template name='abstract' visibility='abstract'/>
</c:package>
");

         CompileResult result = compiler.Compile(module, baseUri: new Uri("http://localhost"));

         Assert.IsFalse(result.Templates.Contains(new QualifiedName("private")));
         Assert.IsTrue(result.Templates.Contains(new QualifiedName("public")));
         Assert.IsTrue(result.Templates.Contains(new QualifiedName("final")));
         Assert.IsTrue(result.Templates.Contains(new QualifiedName("abstract")));
      }
   }
}
