
namespace System.Diagnostics {

   using CodeAnalysis;

   static class Assert {

      [Conditional("DEBUG")]
      public static void
      That([DoesNotReturnIf(false)] bool condition) =>
         Debug.Assert(condition);

      [Conditional("DEBUG")]
      public static void
      That([DoesNotReturnIf(false)] bool condition, string message) =>
         Debug.Assert(condition, message);
   }
}

#if NETFRAMEWORK || NETSTANDARD2_0
namespace System.Diagnostics.CodeAnalysis {

   [AttributeUsage(AttributeTargets.Parameter, Inherited = false)]
   sealed class DoesNotReturnIfAttribute : Attribute {

      public bool
      ParameterValue { get; }

      public
      DoesNotReturnIfAttribute(bool parameterValue) {
         this.ParameterValue = parameterValue;
      }
   }
}
#endif
