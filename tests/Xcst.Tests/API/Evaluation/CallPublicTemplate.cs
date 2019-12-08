using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Xcst.Tests.API.Evaluation {

   partial class EvaluationTests {

      [TestMethod]
      [TestCategory(TestCategory)]
      public void
      Call_Public_Template() {

         XcstEvaluator.Using(new ExposePublicTemplatesOnly())
            .CallTemplate("public")
            .OutputTo(TextWriter.Null)
            .Run();
      }
   }
}
