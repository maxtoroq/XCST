using System.IO;
using NUnit.Framework;

namespace Xcst.Tests.API.Evaluation;

partial class EvaluationTests {

   [Test]
   [Category(TestCategory)]
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
