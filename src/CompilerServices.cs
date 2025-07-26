
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
