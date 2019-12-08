using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Xcst.Tests.API.Evaluation {

   partial class EvaluationTests {

      [TestMethod]
      [TestCategory(TestCategory)]
      [ExpectedException(typeof(RuntimeException))]
      public void
      Call_Private_Template() {

         XcstEvaluator.Using(new ExposePublicTemplatesOnly())
            .CallTemplate("private")
            .OutputTo(TextWriter.Null)
            .Run();
      }
   }
}
