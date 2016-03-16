using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Xcst.Tests.Evaluation {

   [TestClass]
   public class EvaluatorTests {

      [TestMethod, ExpectedException(typeof(InvalidOperationException))]
      public void Disallow_Re_Prime() {

         var evaluator = XcstEvaluator.Using<DisallowRePrime>()
            .WithParam("foo", "foo");

         evaluator.CallInitialTemplate()
            .OutputTo(new StringWriter())
            .Run();

         evaluator.WithParam("bar", "bar")
            .CallInitialTemplate()
            .OutputTo(new StringWriter())
            .Run();
      }

      [TestMethod]
      public void Lock_Template_Params() {

         var template = XcstEvaluator.Using<LockTemplateParams>()
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
