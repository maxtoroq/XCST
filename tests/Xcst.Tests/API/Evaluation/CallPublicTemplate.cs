using System.IO;
using NUnit.Framework;

namespace Xcst.Tests.API.Evaluation {

   partial class EvaluationTests {

      [Test]
      [Category(TestCategory)]
      public void
      Call_Public_Template() {

         XcstEvaluator.Using(new ExposePublicTemplatesOnly())
            .CallTemplate("public")
            .OutputTo(TextWriter.Null)
            .Run();
      }
   }
}
