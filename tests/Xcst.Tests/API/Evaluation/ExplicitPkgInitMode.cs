using System.IO;
using NUnit.Framework;

namespace Xcst.Tests.API.Evaluation;
using TestPackage = ExplicitPkgInitMode;

partial class EvaluationTests {

   [Test]
   [Category(TestCategory)]
   public void
   Explicit_Package_Initial_Default_Unnamed_Mode() {

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
   Explicit_Package_Initial_Named_Mode() {

      var outputter = XcstEvaluator.Using(new TestPackage())
         .ApplyTemplates(new object(), "foo")
         .OutputTo(TextWriter.Null);

      Assert.Throws<RuntimeException>(() => outputter.Run());
   }
}
