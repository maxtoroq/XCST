
namespace System.Runtime.CompilerServices;

#if NETFRAMEWORK || NETCOREAPP2_0 || NETSTANDARD
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

#if !NET7_0_OR_GREATER
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false, Inherited = false)]
sealed class RequiredMemberAttribute : Attribute { }

[AttributeUsage(AttributeTargets.All, AllowMultiple = true, Inherited = false)]
sealed class CompilerFeatureRequiredAttribute : Attribute {

   public string
   FeatureName { get; }

   public bool
   IsOptional { get; init; }

   public
   CompilerFeatureRequiredAttribute(string featureName) {
      this.FeatureName = featureName;
   }
}
#endif
