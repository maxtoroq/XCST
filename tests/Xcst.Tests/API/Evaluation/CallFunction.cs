using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Xcst.Tests.API.Evaluation {

   partial class EvaluationTests {

      [TestMethod, TestCategory(TestCategory)]
      public void
      Call_Function() {

         var result = XcstEvaluator.Using(new CallFunction())
            .WithParam("i", 2)
            .CallFunction(p => p.Foo())
            .Evaluate();

         Assert.AreEqual("foo2", result);
      }
   }
}
