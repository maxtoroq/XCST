// Copyright 2017 Max Toro Q.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;

namespace Xcst.Runtime;

public static class EvaluateDelegate {

   public static void
   Invoke<TDerived, TBase>(XcstDelegate<TDerived> del, TemplateContext context, ISequenceWriter<TBase> output)
         where TDerived : TBase {

      Argument.NotNull(del);
      Argument.NotNull(context);
      Argument.NotNull(output);

      var derivedWriter = output as ISequenceWriter<TDerived>
         ?? new DerivedSequenceWriter<TDerived, TBase>(output);

      del.Invoke(context, derivedWriter);
   }

   [Obsolete("The provided delegate is not compatible with the current sequence constructor.", error: true)]
   public static void
   Invoke<TItem>(Delegate del, TemplateContext context, ISequenceWriter<TItem> output) {

      Argument.NotNull(del);
      Argument.NotNull(context);
      Argument.NotNull(output);

      var delType = del.GetType();
      var xcstType = delType.GetGenericTypeDefinition();

      if (xcstType != typeof(XcstDelegate<>)) {
         throw new RuntimeException("Invalid delegate.");
      }

      var xcstTypeParams = delType.GetGenericArguments();
      var derivedType = xcstTypeParams[0];
      var baseType = typeof(TItem);

      object derivedWriter = output;

      var compatibleOutput = typeof(ISequenceWriter<>)
         .MakeGenericType(derivedType)
         .IsAssignableFrom(output.GetType());

      if (!compatibleOutput) {

         if (baseType.IsAssignableFrom(derivedType)) {

            derivedWriter = Activator.CreateInstance(typeof(DerivedSequenceWriter<,>)
               .MakeGenericType(derivedType, baseType), output)!;

         } else {

            throw new RuntimeException($"{derivedType.FullName} is not compatible with {baseType.FullName}.");
         }
      }

      del.DynamicInvoke(context, derivedWriter);
   }
}
