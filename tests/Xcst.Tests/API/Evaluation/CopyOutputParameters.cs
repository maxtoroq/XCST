using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Xcst.Tests.API.Evaluation {

   partial class EvaluationTests {

      [TestMethod]
      [TestCategory(TestCategory)]
      public void
      Copy_OutputParameters() {

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
   }
}
