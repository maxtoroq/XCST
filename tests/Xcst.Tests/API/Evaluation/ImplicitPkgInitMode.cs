using System.IO;
using NUnit.Framework;

namespace Xcst.Tests.API.Evaluation {

   using TestPackage = ImplicitPkgInitMode;

   partial class EvaluationTests {

      [Test]
      [Category(TestCategory)]
      public void
      Implicit_Package_Initial_Default_Unnamed_Mode() {

         var output = new StringWriter();

         XcstEvaluator.Using(new TestPackage())
            .ApplyTemplates(new object())
            .OutputTo(output)
            .Run();

         Assert.AreEqual("#unnamed", output.ToString());
      }

      [Test]
      [Category(TestCategory)]
      public void
      Implicit_Package_Initial_Named_Mode() {

         var output = new StringWriter();

         XcstEvaluator.Using(new TestPackage())
            .ApplyTemplates(new object(), new QualifiedName("foo"))
            .OutputTo(output)
            .Run();

         Assert.AreEqual("foo", output.ToString());
      }
   }
}
