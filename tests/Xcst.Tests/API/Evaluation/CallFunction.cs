using System.IO;
using NUnit.Framework;

namespace Xcst.Tests.API.Evaluation;

partial class EvaluationTests {

   [Test]
   [Category(TestCategory)]
   public void
   Call_Function() {

      var result = XcstEvaluator.Using(new CallFunction())
         .WithParam("i", 2)
         .CallFunction(p => p.Foo())
         .Evaluate();

      Assert.AreEqual("foo2", result);
   }
}
