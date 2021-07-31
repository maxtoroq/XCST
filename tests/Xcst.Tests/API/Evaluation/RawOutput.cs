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

      [Test]
      [Category(TestCategory)]
      public void
      Raw_Output_Derived() {

         var output = new List<object>();

         XcstEvaluator.Using(new RawOutput())
            .CallInitialTemplate()
            .OutputTo(output)
            .Run();

         Assert.AreEqual(2, output.Count);
         Assert.AreEqual(1, output[0]);
         Assert.AreEqual(2, output[1]);
      }

      [Test]
      [Category(TestCategory)]
      public void
      Raw_Output_Cast() {

         var output = new List<int>();

         XcstEvaluator.Using(new RawOutput())
            .CallTemplate("objects")
            .OutputTo(output)
            .Run();

         Assert.AreEqual(2, output.Count);
         Assert.AreEqual(1, output[0]);
         Assert.AreEqual(2, output[1]);
      }

      [Test]
      [Category(TestCategory)]
      public void
      Raw_Output_Incompatible() {

         var output = new List<int>();

         var outputter = XcstEvaluator.Using(new RawOutput())
            .CallTemplate("strings")
            .OutputTo(output);

         Assert.Throws<RuntimeException>(() => outputter.Run());
      }
   }
}
