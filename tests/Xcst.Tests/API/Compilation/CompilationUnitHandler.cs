using System;
using System.IO;
using NUnit.Framework;
using Xcst.Compiler;

namespace Xcst.Tests.API.Compilation {

   partial class CompilationTests {

      [Test]
      [Category(TestCategory)]
      public void
      Compilation_Unit_Handler() {

         var compiler = TestsHelper.CreateCompiler();
         compiler.TargetClass = "FooPackage";
         compiler.TargetNamespace = typeof(CompilationTests).Namespace;

         var writer = new StringWriter();

         compiler.CompilationUnitHandler = href => {

            Assert.AreEqual("xcst.generated.cs", href);

            return writer;
         };

         var module = new StringReader(@"
<c:package version='1.0' language='C#' xmlns:c='http://maxtoroq.github.io/XCST'>
</c:package>
");

         CompileResult result = compiler.Compile(module);

         Assert.IsFalse(String.IsNullOrEmpty(writer.ToString()));
         Assert.IsEmpty(result.CompilationUnits);

         // writer should not be closed
         writer.WriteLine();
      }

      [Test]
      [Category(TestCategory)]
      public void
      Compilation_Unit_Handler_VB() {

         var compiler = TestsHelper.CreateCompiler();
         compiler.TargetClass = "FooPackage";
         compiler.TargetNamespace = typeof(CompilationTests).Namespace;

         var writer = new StringWriter();

         compiler.CompilationUnitHandler = href => {

            Assert.AreEqual("xcst.1.generated.vb", href);

            return writer;
         };

         var module = new StringReader(@"
<c:package version='1.0' language='VisualBasic' xmlns:c='http://maxtoroq.github.io/XCST'>
</c:package>
");

         CompileResult result = compiler.Compile(module);

         Assert.IsFalse(String.IsNullOrEmpty(writer.ToString()));
         Assert.IsEmpty(result.CompilationUnits);

         // writer should not be closed
         writer.WriteLine();
      }
   }
}
