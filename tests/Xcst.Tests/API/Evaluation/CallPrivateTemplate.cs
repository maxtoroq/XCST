using System.IO;
using NUnit.Framework;

namespace Xcst.Tests.API.Evaluation {

   partial class EvaluationTests {

      [Test]
      [Category(TestCategory)]
      public void
      Call_Private_Template() {

         var outputter = XcstEvaluator.Using(new ExposePublicTemplatesOnly())
            .CallTemplate("private")
            .OutputTo(TextWriter.Null);

         Assert.Throws<RuntimeException>(() => outputter.Run());
      }
   }
}
