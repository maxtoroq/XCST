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
using Xcst.PackageModel;

namespace Xcst.Runtime {

   public static class EvaluateDelegate {

      public static void
      Invoke<TDerived, TBase>(XcstDelegate<TDerived> del, TemplateContext context, ISequenceWriter<TBase> output)
            where TDerived : TBase {

         if (del == null) throw new ArgumentNullException(nameof(del));
         if (context == null) throw new ArgumentNullException(nameof(context));
         if (output == null) throw new ArgumentNullException(nameof(output));

         var derivedWriter = output as ISequenceWriter<TDerived>
            ?? new CastingSequenceWriter<TDerived, TBase>(output);

         del.Invoke(context, derivedWriter);
      }

      public static void
      Invoke<TItem>(Delegate del, TemplateContext context, ISequenceWriter<TItem> output) {

         if (del == null) throw new ArgumentNullException(nameof(del));
         if (context == null) throw new ArgumentNullException(nameof(context));
         if (output == null) throw new ArgumentNullException(nameof(output));

         Type delType = del.GetType();
         Type xcstType = delType.GetGenericTypeDefinition();

         if (xcstType != typeof(XcstDelegate<>)) {
            throw new RuntimeException("Invalid delegate.");
         }

         Type[] xcstTypeParams = delType.GetGenericArguments();
         Type derivedType = xcstTypeParams[0];
         Type baseType = typeof(TItem);

         object derivedWriter = output;
         bool compatibleOutput = typeof(ISequenceWriter<>).MakeGenericType(derivedType)
            .IsAssignableFrom(output.GetType());

         if (!compatibleOutput) {

            if (baseType.IsAssignableFrom(derivedType)) {

               derivedWriter = Activator.CreateInstance(typeof(CastingSequenceWriter<,>)
                  .MakeGenericType(derivedType, baseType), output);

            } else {

               throw new RuntimeException($"{derivedType.FullName} is not compatible with {baseType.FullName}.");
            }
         }

         del.DynamicInvoke(context, derivedWriter);
      }
   }
}
