using System;
using System.IO;
using System.Linq;
using NUnit.Framework;

namespace Xcst.Tests.Types;

partial class TypesTests {

   [Test]
   [Category(TestCategory)]
   public void
   Partial() {

      var compiler = TestsHelper.CreateCompiler();
      compiler.TargetClass = "FooPackage";
      compiler.TargetNamespace = typeof(TypesTests).Namespace;

      var usingPackageUri = new Uri(@"c:\foo.xcst");

      var result = compiler.Compile(
         new StringReader("""
            <c:package version='1.0' language='C#' xmlns:c='http://maxtoroq.github.io/XCST'>
               <c:type name='Foo' visibility='public'/>
            </c:package>
            """),
         usingPackageUri
      );

      var compilationUnits = result.CompilationUnits
         .Append($$"""
            namespace {{compiler.TargetNamespace}} {
               partial class {{compiler.TargetClass}} {
                  
                  partial class Foo {
                     public override string ToString() =>
                        "Hello " + nameof(Foo);
                  }
               }
            }
            """)
         .ToArray();

      var pkgType = TestsHelper.CompileCode(
         compiler.TargetNamespace + "." + compiler.TargetClass,
         usingPackageUri,
         compilationUnits,
         result.Language
      );

      var fooType = pkgType.GetNestedType("Foo");

      Assert.IsNotNull(fooType);

      var foo = Activator.CreateInstance(fooType!)!;

      Assert.AreEqual("Hello Foo", foo.ToString());
   }
}
