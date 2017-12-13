using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Xcst.Tests.API.Evaluation {

   [TestClass]
   public class EvaluationTests {

      const string TestCategory = nameof(API) + "." + nameof(Evaluation);

      [TestMethod, TestCategory(TestCategory)]
      [ExpectedException(typeof(InvalidOperationException))]
      public void Disallow_Re_Prime() {

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

      [TestMethod, TestCategory(TestCategory)]
      public void Lock_Template_Params() {

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

      [TestMethod, TestCategory(TestCategory)]
      public void Call_Public_Template() {

         XcstEvaluator.Using(new ExposePublicTemplatesOnly())
            .CallTemplate("public")
            .OutputTo(TextWriter.Null)
            .Run();
      }

      [TestMethod, TestCategory(TestCategory)]
      public void Call_Final_Template() {

         XcstEvaluator.Using(new ExposePublicTemplatesOnly())
            .CallTemplate("final")
            .OutputTo(TextWriter.Null)
            .Run();
      }

      [TestMethod, TestCategory(TestCategory)]
      [ExpectedException(typeof(RuntimeException))]
      public void Call_Private_Template() {

         XcstEvaluator.Using(new ExposePublicTemplatesOnly())
            .CallTemplate("private")
            .OutputTo(TextWriter.Null)
            .Run();
      }

      [TestMethod, TestCategory(TestCategory)]
      public void Copy_OutputParameters() {

         var parameters = new OutputParameters {
            Method = new QualifiedName("xml"),
            OmitXmlDeclaration = true
         };

         var output = new StringWriter();

         var outputter = XcstEvaluator.Using(new CopyOutputParameters())
            .CallInitialTemplate()
            .OutputTo(output)
            .WithParams(parameters);

         parameters.OmitXmlDeclaration = false;

         outputter.Run();

         Assert.AreEqual("<div />", output.ToString());
      }

      [TestMethod, TestCategory(TestCategory)]
      public void Raw_Output() {

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
