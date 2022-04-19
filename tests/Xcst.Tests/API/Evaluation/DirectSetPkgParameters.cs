using NUnit.Framework;

namespace Xcst.Tests.API.Evaluation {

   partial class EvaluationTests {

      [Test]
      [Category(TestCategory)]
      public void
      Direct_Set_Package_Parameter_No_Default() {

         const int value = 5;

         var pkg = new DirectSetPkgParamNoDefault {
            a = value
         };

         var result = XcstEvaluator.Using(pkg)
            .WithParam(nameof(pkg.a), 7)
            .CallFunction(p => p.foo())
            .Evaluate();

         Assert.AreEqual(value, result);
      }

      [Test]
      [Category(TestCategory)]
      public void
      Direct_Set_Package_Parameter_With_Default() {

         const int value = 5;

         var pkg = new DirectSetPkgParamWithDefault {
            a = value
         };

         var result = XcstEvaluator.Using(pkg)
            .CallFunction(p => p.foo())
            .Evaluate();

         Assert.AreEqual(value, result);

         var result2 = XcstEvaluator.Using(pkg)
            .WithParam(nameof(pkg.a), 7)
            .CallFunction(p => p.foo())
            .Evaluate();

         Assert.AreEqual(value, result2);
      }

      [Test]
      [Category(TestCategory)]
      public void
      Direct_Set_Package_Parameter_Required() {

         const int value = 5;
         const int withParam = 7;

         var pkg = new DirectSetPkgParamRequired {
            a = value
         };

         var result = XcstEvaluator.Using(pkg)
            .WithParam(nameof(pkg.a), withParam)
            .CallFunction(p => p.foo())
            .Evaluate();

         Assert.AreEqual(withParam, result);
      }
   }
}
