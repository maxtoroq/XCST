using System;
using TestAssert = NUnit.Framework.Assert;

namespace Xcst.Tests {

   public abstract class TestBase {

      protected Type
      CompileType<T>(T _) => typeof(T);

      public static class Assert {

         public static void
         IsTrue(bool condition) => TestAssert.IsTrue(condition);

         public static void
         IsFalse(bool condition) => TestAssert.IsFalse(condition);

         public static void
         AreEqual<T>(T expected, T actual) =>
            TestAssert.AreEqual(expected, actual);

         public static void
         IsNull(object? value) => TestAssert.IsNull(value);

         public static void
         IsNotNull(object? value) => TestAssert.IsNotNull(value);

         public static void
         Fail() => TestAssert.Fail();
      }
   }
}
