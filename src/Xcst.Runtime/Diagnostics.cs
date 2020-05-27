
namespace System.Diagnostics {

   static class Assert {

      [Conditional("DEBUG")]
      public static void IsNotNull(object? value) =>
         Debug.Assert(value != null);
   }
}
