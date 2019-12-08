using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Xcst.Tests.API.Evaluation {

   partial class EvaluationTests {

      [TestMethod]
      [TestCategory(TestCategory)]
      public void
      Raw_Output() {

         var output = new List<int>();

         XcstEvaluator.Using(new RawOutput())
            .CallInitialTemplate()
            .OutputTo(output)
            .Run();

         Assert.AreEqual(2, output.Count);
         Assert.AreEqual(1, output[0]);
         Assert.AreEqual(2, output[1]);
      }
   }
}
