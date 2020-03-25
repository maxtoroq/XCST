using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Xcst.Compiler;

namespace Xcst.Tests.API.Compilation {

   [TestFixture]
   public class CompilationTests {

      const string
      TestCategory = nameof(API) + "." + nameof(Compilation);

      [Test]
      [Category(TestCategory)]
      public void
      CompileResult_Lists_Public_Templates_Only() {

         var compiler = TestsHelper.CreateCompiler();
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

         Assert.IsFalse(result.Templates.Contains("private"));
         Assert.IsTrue(result.Templates.Contains("public"));
         Assert.IsTrue(result.Templates.Contains("final"));
         Assert.IsTrue(result.Templates.Contains("abstract"));
      }
   }
}
