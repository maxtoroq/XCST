using System.IO;
using NUnit.Framework;

namespace Xcst.Tests.API.Evaluation {

   using TestPackage = ExplicitPkgInitDefaultMode;

   partial class EvaluationTests {

      [Test]
      [Category(TestCategory)]
      public void
      Explicit_Package_Initial_Default_Named_Mode() {

         var output = new StringWriter();

         XcstEvaluator.Using(new TestPackage())
            .ApplyTemplates(new object())
            .OutputTo(output)
            .Run();

         Assert.AreEqual("foo", output.ToString());
      }
   }
}
