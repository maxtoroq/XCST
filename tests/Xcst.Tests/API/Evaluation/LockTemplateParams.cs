using System.IO;
using NUnit.Framework;

namespace Xcst.Tests.API.Evaluation {

   partial class EvaluationTests {

      [Test]
      [Category(TestCategory)]
      public void
      Lock_Template_Params() {

         var template = XcstEvaluator.Using(new LockTemplateParams())
            .CallInitialTemplate();

         var output1 = new StringWriter();

         var outputter1 = template.WithParam("foo", "foo")
            .OutputTo(output1);

         outputter1.Run();

         Assert.AreEqual("foo", output1.ToString());

         // outputter keeps parameters after first run
         // second run should give same result

         output1.GetStringBuilder().Clear();
         outputter1.Run();

         Assert.AreEqual("foo", output1.ToString());

         // new outputter should also give same result

         var output2 = new StringWriter();

         template.OutputTo(output2)
            .Run();

         Assert.AreEqual("foo", output2.ToString());

         // adding another parameter

         var output3 = new StringWriter();

         template.WithParam("bar", "bar")
            .OutputTo(output3)
            .Run();

         Assert.AreEqual("bar", output3.ToString());

         // new parameter should not affect previously created outputters

         output1.GetStringBuilder().Clear();
         outputter1.Run();

         Assert.AreEqual("foo", output1.ToString());
      }
   }
}
