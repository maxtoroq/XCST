
namespace System.Runtime.CompilerServices;

// removing IsExternalInit is a breaking change resulting in MissingMethodException
// when caller is compiled against older version
static class IsExternalInit { }

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
