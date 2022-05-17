using System.IO;
using NUnit.Framework;

namespace Xcst.Tests.API.Evaluation;
using TestPackage = ImplicitPkgInitTemplate;

partial class EvaluationTests {

   [Test]
   [Category(TestCategory)]
   public void
   Call_Private_Template() {

      var outputter = XcstEvaluator.Using(new TestPackage())
         .CallTemplate("private")
         .OutputTo(TextWriter.Null);

      Assert.Throws<RuntimeException>(() => outputter.Run());
   }

   [Test]
   [Category(TestCategory)]
   public void
   Call_Public_Template() {

      var output = new StringWriter();

      XcstEvaluator.Using(new TestPackage())
         .CallTemplate("public")
         .OutputTo(output)
         .Run();

      Assert.AreEqual("public", output.ToString());
   }

   [Test]
   [Category(TestCategory)]
   public void
   Call_Final_Template() {

      var output = new StringWriter();

      XcstEvaluator.Using(new TestPackage())
         .CallTemplate("final")
         .OutputTo(output)
         .Run();

      Assert.AreEqual("final", output.ToString());
   }

   [Test]
   [Category(TestCategory)]
   public void
   Call_No_Visibility_Template() {

      var output = new StringWriter();

      XcstEvaluator.Using(new TestPackage())
         .CallTemplate("no-visibility")
         .OutputTo(output)
         .Run();

      Assert.AreEqual("no-visibility", output.ToString());
   }
}
