
namespace System.Runtime.CompilerServices;

#if NETFRAMEWORK || NETCOREAPP2_0
static class IsExternalInit { }
#endif

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
