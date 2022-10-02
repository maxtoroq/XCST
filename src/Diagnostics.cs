
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

#if NETFRAMEWORK || NETCOREAPP2_0 || NETSTANDARD2_0
namespace System.Diagnostics.CodeAnalysis {

   [AttributeUsage(AttributeTargets.Method, Inherited = false)]
   sealed class DoesNotReturnAttribute : Attribute { }

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
