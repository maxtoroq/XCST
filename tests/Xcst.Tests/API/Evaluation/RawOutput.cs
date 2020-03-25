using System.Collections.Generic;
using NUnit.Framework;

namespace Xcst.Tests.API.Evaluation {

   partial class EvaluationTests {

      [Test]
      [Category(TestCategory)]
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
