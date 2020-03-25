using System.IO;
using NUnit.Framework;

namespace Xcst.Tests.API.Evaluation {

   partial class EvaluationTests {

      [Test, Category(TestCategory)]
      public void
      Call_Final_Template() {

         XcstEvaluator.Using(new ExposePublicTemplatesOnly())
            .CallTemplate("final")
            .OutputTo(TextWriter.Null)
            .Run();
      }
   }
}
