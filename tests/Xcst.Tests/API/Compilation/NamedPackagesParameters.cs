using System;
using System.IO;
using NUnit.Framework;

namespace Xcst.Tests.API.Compilation {

   partial class CompilationTests {

      [Test]
      [Category(TestCategory)]
      public void
      Ignore_Target_Namespace_On_Named_Package_With_Namespace() {

         var compiler = TestsHelper.CreateCompiler();
         compiler.CompilationUnitHandler = n => TextWriter.Null;

         compiler.TargetNamespace = "localhost";

         var module = new StringReader(@"
<c:package name='localhost.FooPackage' version='1.0' language='C#' xmlns:c='http://maxtoroq.github.io/XCST'>
</c:package>
");

         compiler.Compile(module, baseUri: new Uri("http://localhost"));
      }

      [Test]
      [Category(TestCategory)]
      public void
      Ignore_Target_Class_On_Named_Package_With_Namespace() {

         var compiler = TestsHelper.CreateCompiler();
         compiler.CompilationUnitHandler = n => TextWriter.Null;

         compiler.TargetClass = "FooPackage";

         var module = new StringReader(@"
<c:package name='localhost.FooPackage' version='1.0' language='C#' xmlns:c='http://maxtoroq.github.io/XCST'>
</c:package>
");

         compiler.Compile(module, baseUri: new Uri("http://localhost"));
      }

      [Test]
      [Category(TestCategory)]
      public void
      Ignore_Target_Class_On_Named_Package_Without_Namespace() {

         var compiler = TestsHelper.CreateCompiler();
         compiler.CompilationUnitHandler = n => TextWriter.Null;

         compiler.TargetClass = "FooPackage";
         compiler.TargetNamespace = typeof(CompilationTests).Namespace;

         var module = new StringReader(@"
<c:package name='FooPackage' version='1.0' language='C#' xmlns:c='http://maxtoroq.github.io/XCST'>
</c:package>
");

         compiler.Compile(module, baseUri: new Uri("http://localhost"));
      }
   }
}
