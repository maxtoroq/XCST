// Copyright 2016 Max Toro Q.
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
using System.Collections.Generic;
using System.Linq;

namespace Xcst.Runtime {

   /// <exclude/>

   public interface ISequenceWriter<in TItem> {

      void WriteObject(TItem value);

      void WriteObject(IEnumerable<TItem> value);

      void WriteString(TItem text);

      void WriteRaw(TItem data);
   }

   /// <exclude/>

   public class SequenceWriter<TItem> : ISequenceWriter<TItem> {

      readonly List<TItem> buffer = new List<TItem>();

      public void WriteObject(TItem value) {
         this.buffer.Add(value);
      }

      public void WriteObject(IEnumerable<TItem> value) {

         if (value != null) {

            foreach (var item in value) {
               WriteObject(item);
            }
         }
      }

      public void WriteString(TItem text) {
         WriteObject(text);
      }

      public void WriteRaw(TItem data) {
         WriteObject(data);
      }

      public SequenceWriter<TItem> WriteSequenceConstructor(Action<ISequenceWriter<TItem>> seqCtor) {

         seqCtor(this);

         return this;
      }

      public SequenceWriter<TItem> WriteTemplate(
            Action<TemplateContext, ISequenceWriter<TItem>> template,
            TemplateContext context) {

         template(context, this);

         return this;
      }

      public SequenceWriter<TItem> WriteTemplate<TDerived>(
            Action<TemplateContext, ISequenceWriter<TDerived>> template,
            TemplateContext context,
            Func<TDerived> forTypeInference = null) where TDerived : TItem {

         ISequenceWriter<TDerived> derivedWriter = SequenceWriter.AdjustWriter<TItem, TDerived>(this);

         template(context, derivedWriter);

         return this;
      }

      public TItem[] Flush() {

         TItem[] seq = this.buffer.ToArray();
         this.buffer.Clear();

         return seq;
      }

      public TItem FlushSingle() {
         return Flush().Single();
      }
   }

   /// <exclude/>

   public static class SequenceWriter {

      public static SequenceWriter<TItem> Create<TItem>(Func<TItem> forTypeInference = null) {
         return new SequenceWriter<TItem>();
      }

      public static ISequenceWriter<TDerived> AdjustWriter<TBase, TDerived>(
            ISequenceWriter<TBase> output,
            Func<TDerived> forTypeInference = null) where TDerived : TBase {

         return output as ISequenceWriter<TDerived>
            ?? new CastingSequenceWriter<TDerived, TBase>(output);
      }

      static ISequenceWriter<TDerived> AdjustWriterDynamically<TBase, TDerived>(ISequenceWriter<TBase> output) {

         if (output is ISequenceWriter<TDerived>) {
            return (ISequenceWriter<TDerived>)output;
         }

         if (typeof(TBase).IsAssignableFrom(typeof(TDerived))) {

            return (ISequenceWriter<TDerived>)Activator.CreateInstance(typeof(CastingSequenceWriter<,>)
               .MakeGenericType(typeof(TDerived), typeof(TBase)), output);
         }

         throw new ArgumentException(nameof(output));
      }
   }

   class CastingSequenceWriter<TDerived, TBase> : ISequenceWriter<TDerived> where TDerived : TBase {

      readonly ISequenceWriter<TBase> baseWriter;

      public CastingSequenceWriter(ISequenceWriter<TBase> baseWriter) {
         this.baseWriter = baseWriter;
      }

      public void WriteObject(TDerived value) {
         this.baseWriter.WriteObject(value);
      }

      public void WriteObject(IEnumerable<TDerived> value) {

         if (value != null) {

            foreach (var item in value) {
               WriteObject(item);
            }
         }
      }

      public void WriteString(TDerived text) {
         this.baseWriter.WriteString(text);
      }

      public void WriteRaw(TDerived data) {
         this.baseWriter.WriteRaw(data);
      }
   }
}
