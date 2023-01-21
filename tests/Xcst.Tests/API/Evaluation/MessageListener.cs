using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Microsoft.VisualStudio.TestPlatform.Utilities;
using NUnit.Framework;

namespace Xcst.Tests.API.Evaluation;

partial class EvaluationTests {

   [Test]
   [Category(TestCategory)]
   public void
   Listen_Messages() {

      var sb = new StringBuilder();

      XcstEvaluator.Using(new MessageListener())
         .CallInitialTemplate()
         .OutputTo(TextWriter.Null)
         .WithMessageListener(args => sb.AppendLine(args.Message))
         .Run();

      Assert.AreEqual("foo" + Environment.NewLine + "bar" + Environment.NewLine, sb.ToString());
   }
}
