using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Xcst.Tests.API.Evaluation {

   partial class EvaluationTests {

      [TestMethod, TestCategory(TestCategory)]
      public void
      Call_Final_Template() {

         XcstEvaluator.Using(new ExposePublicTemplatesOnly())
            .CallTemplate("final")
            .OutputTo(TextWriter.Null)
            .Run();
      }
   }
}
