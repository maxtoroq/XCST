using System.IO;
using NUnit.Framework;

namespace Xcst.Tests.API.Evaluation {

   using TestPackage = ExplicitPkgInitTemplate;

   partial class EvaluationTests {

      [Test]
      [Category(TestCategory)]
      public void
      Call_No_Visibility_Template_Explicit_Package() {

         var outputter = XcstEvaluator.Using(new TestPackage())
            .CallTemplate("no-visibility")
            .OutputTo(TextWriter.Null);

         Assert.Throws<RuntimeException>(() => outputter.Run());
      }
   }
}
