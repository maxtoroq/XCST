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

      // This overload is required for XcstWriter output

      public static void Invoke<TOutput>(Action<TemplateContext, TOutput> del, TemplateContext context, TOutput output) {

         if (del == null) throw new ArgumentNullException(nameof(del));
         if (context == null) throw new ArgumentNullException(nameof(context));
         if (output == null) throw new ArgumentNullException(nameof(output));

         del.Invoke(context, output);
      }

      public static void Invoke<TDerived, TBase>(Action<TemplateContext, ISequenceWriter<TDerived>> del, TemplateContext context, ISequenceWriter<TBase> output)
            where TDerived : TBase {

         if (del == null) throw new ArgumentNullException(nameof(del));
         if (context == null) throw new ArgumentNullException(nameof(context));
         if (output == null) throw new ArgumentNullException(nameof(output));

         var derivedWriter = output as ISequenceWriter<TDerived>
            ?? new CastingSequenceWriter<TDerived, TBase>(output);

         del.Invoke(context, derivedWriter);
      }

      public static void Invoke<TItem>(Delegate del, TemplateContext context, ISequenceWriter<TItem> output) {

         if (del == null) throw new ArgumentNullException(nameof(del));
         if (context == null) throw new ArgumentNullException(nameof(context));
         if (output == null) throw new ArgumentNullException(nameof(output));

         Type delType = del.GetType();
         Type actionType = delType.GetGenericTypeDefinition();
         Type[] actionTypeParams = delType.GetGenericArguments();
         Type seqWriterType = null;

         if (actionType != typeof(Action<,>)
            || actionTypeParams[0] != typeof(TemplateContext)
            || (actionTypeParams[1] != typeof(XcstWriter)
               && (seqWriterType = actionTypeParams[1].GetGenericTypeDefinition()) != typeof(ISequenceWriter<>))) {

            throw new RuntimeException("Invalid delegate.");
         }

         object derivedWriter = output;

         if (!actionTypeParams[1].IsAssignableFrom(output.GetType())) {

            Type derivedType = (seqWriterType != null) ?
               actionTypeParams[1].GetGenericArguments()[0]
               : typeof(object);

            Type baseType = typeof(TItem);

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
