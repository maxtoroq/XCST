using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Xcst;

static class Argument {

   public static void
   NotNull(object? argument, [CallerArgumentExpression("argument")] string? paramName = null) {

      if (argument is null) {
         Throw(paramName);
      }
   }

   [DoesNotReturn]
   static void
   Throw(string? paramName) => throw new ArgumentNullException(paramName);

   public static ArgumentNullException
   Null(object? argument, [CallerArgumentExpression("argument")] string? paramName = null) =>
      new ArgumentNullException(paramName);
}
