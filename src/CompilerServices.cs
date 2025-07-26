
namespace System.Runtime.CompilerServices;

#if !NET6_0_OR_GREATER
[AttributeUsage(AttributeTargets.Parameter, Inherited = false)]
sealed class CallerArgumentExpressionAttribute : Attribute {

   public string
   ParameterName { get; }

   public
   CallerArgumentExpressionAttribute(string parameterName) {
      this.ParameterName = parameterName;
   }
}
#endif

#if !NET9_0_OR_GREATER
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Constructor | AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
sealed class OverloadResolutionPriorityAttribute : Attribute {

   public int
   Priority { get; }

   public
   OverloadResolutionPriorityAttribute(int priority) {
      this.Priority = priority;
   }
}
#endif
