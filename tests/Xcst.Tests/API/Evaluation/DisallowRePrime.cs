using System;
using System.IO;
using NUnit.Framework;

namespace Xcst.Tests.API.Evaluation;

partial class EvaluationTests {

   [Test]
   [Category(TestCategory)]
   public void
   Disallow_Re_Prime() {

      var evaluator = XcstEvaluator.Using(new DisallowRePrime())
         .WithParam("foo", "foo");

      evaluator.CallInitialTemplate()
         .OutputTo(new StringWriter())
         .Run();

      Assert.Throws<InvalidOperationException>(() => evaluator.WithParam("bar", "bar"));
   }
}
