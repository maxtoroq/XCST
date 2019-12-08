using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Xcst.Tests.API.Evaluation {

   partial class EvaluationTests {

      [TestMethod]
      [TestCategory(TestCategory)]
      [ExpectedException(typeof(InvalidOperationException))]
      public void
      Disallow_Re_Prime() {

         var evaluator = XcstEvaluator.Using(new DisallowRePrime())
            .WithParam("foo", "foo");

         evaluator.CallInitialTemplate()
            .OutputTo(new StringWriter())
            .Run();

         evaluator.WithParam("bar", "bar")
            .CallInitialTemplate()
            .OutputTo(new StringWriter())
            .Run();
      }
   }
}
