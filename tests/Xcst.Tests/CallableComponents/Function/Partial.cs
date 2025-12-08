using System;
using System.IO;
using System.Linq;
using NUnit.Framework;

namespace Xcst.Tests.CallableComponents.Function;

partial class FunctionTests {

   [Test]
   [Category(TestCategory)]
   public void
   Partial() {

      var compiler = TestsHelper.CreateCompiler();
      compiler.TargetClass = "FooPackage";
      compiler.TargetNamespace = typeof(FunctionTests).Namespace;

      var usingPackageUri = new Uri(@"c:\foo.xcst");

      var result = compiler.Compile(
         new StringReader("""
            <c:package version='1.0' language='C#' xmlns:c='http://maxtoroq.github.io/XCST'>
               <c:function name='MyFunc' as='string' visibility='public' partial='yes'/>
            </c:package>
            """),
         usingPackageUri);

      var compilationUnits = result.CompilationUnits
         .Append($$"""
            namespace {{compiler.TargetNamespace}} {
               partial class {{compiler.TargetClass}} {
                  public virtual partial string MyFunc() => "Hello Partial";
               }
            }
            """)
         .ToArray();

      var pkgType = TestsHelper.CompileCode(
         compiler.TargetNamespace + "." + compiler.TargetClass,
         usingPackageUri,
         compilationUnits,
         result.Language,
         languageVersion: 9);

      var pkg = Activator.CreateInstance(pkgType)!;
      var method = pkgType.GetMethod("MyFunc");

      Assert.IsNotNull(method);

      Assert.AreEqual("Hello Partial", method!.Invoke(pkg, null));
   }
}
