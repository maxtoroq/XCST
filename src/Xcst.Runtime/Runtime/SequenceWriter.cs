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
using Xcst.PackageModel;

namespace Xcst.Runtime {

   /// <exclude/>
   public class SequenceWriter<TItem> : ISequenceWriter<TItem> {

      readonly ICollection<TItem>
      buffer;

      public
      SequenceWriter()
         : this(new List<TItem>()) { }

      public
      SequenceWriter(ICollection<TItem> buffer) {

         if (buffer == null) throw new ArgumentNullException(nameof(buffer));

         this.buffer = buffer;
      }

      public void
      WriteObject(TItem value) => this.buffer.Add(value);

      public void
      WriteObject(IEnumerable<TItem> value) {

         if (value != null) {

            foreach (var item in value) {
               WriteObject(item);
            }
         }
      }

      public void
      WriteObject<TDerived>(IEnumerable<TDerived> value) where TDerived : TItem =>
         WriteObject(value as IEnumerable<TItem> ?? value?.Cast<TItem>());

      public void
      WriteString(TItem text) => WriteObject(text);

      public void
      WriteRaw(TItem data) => WriteObject(data);

      public void
      WriteComment(string text) { }

      public void
      CopyOf(TItem value) {
         throw new NotImplementedException();
      }

      public void
      CopyOf(IEnumerable<TItem> value) {

         if (value != null) {

            foreach (var item in value) {
               CopyOf(item);
            }
         }
      }

      public void
      CopyOf<TDerived>(IEnumerable<TDerived> value) where TDerived : TItem =>
         CopyOf(value as IEnumerable<TItem> ?? value?.Cast<TItem>());

      public XcstWriter
      TryCastToDocumentWriter() => null;

      public MapWriter
      TryCastToMapWriter() => null;

      public SequenceWriter<TItem>
      WriteSequenceConstructor(Action<ISequenceWriter<TItem>> seqCtor) {

         seqCtor(this);

         return this;
      }

      public SequenceWriter<TItem>
      WriteTemplate(
            Action<TemplateContext, ISequenceWriter<TItem>> template,
            TemplateContext context) {

         template(context, this);

         return this;
      }

      public SequenceWriter<TItem>
      WriteTemplate<TDerived>(
            Action<TemplateContext, ISequenceWriter<TDerived>> template,
            TemplateContext context,
            Func<TDerived> forTypeInference = null) where TDerived : TItem {

         ISequenceWriter<TDerived> derivedWriter = SequenceWriter.AdjustWriter<TItem, TDerived>(this);

         template(context, derivedWriter);

         return this;
      }

      public TItem[]
      Flush() {

         TItem[] seq = (this.buffer as List<TItem>)?.ToArray()
            ?? this.buffer.ToArray();

         this.buffer.Clear();

         return seq;
      }

      public TItem
      FlushSingle() => Flush().Single();
   }

   /// <exclude/>
   public static class SequenceWriter {

      public static SequenceWriter<TItem>
      Create<TItem>(Func<TItem> forTypeInference = null) =>
         new SequenceWriter<TItem>();

      public static ISequenceWriter<TDerived>
      AdjustWriter<TBase, TDerived>(
            ISequenceWriter<TBase> output,
            Func<TDerived> forTypeInference = null) where TDerived : TBase {

         if (output == null) throw new ArgumentNullException(nameof(output));

         return output as ISequenceWriter<TDerived>
            ?? new CastingSequenceWriter<TDerived, TBase>(output);
      }

      public static ISequenceWriter<TDerived>
      AdjustWriterDynamically<TBase, TDerived>(
            ISequenceWriter<TBase> output,
            Func<TDerived> forTypeInference = null) {

         if (output == null) throw new ArgumentNullException(nameof(output));

         var derivedWriter = output as ISequenceWriter<TDerived>;

         if (derivedWriter != null) {
            return derivedWriter;
         }

         if (typeof(TBase).IsAssignableFrom(typeof(TDerived))) {

            return (ISequenceWriter<TDerived>)Activator.CreateInstance(typeof(CastingSequenceWriter<,>)
               .MakeGenericType(typeof(TDerived), typeof(TBase)), output);
         }

         throw new RuntimeException($"{typeof(TDerived).FullName} is not compatible with {typeof(TBase).FullName}.");
      }

      public static object
      DefaultInfer() => default(object);

      public static XcstDelegate<TBase>
      CastDelegate<TBase, TDerived>(XcstDelegate<TDerived> del) where TDerived : TBase =>
         (c, o) => del(c, new CastingSequenceWriter<TDerived, TBase>(o));
   }

   class CastingSequenceWriter<TDerived, TBase> : ISequenceWriter<TDerived> where TDerived : TBase {

      readonly ISequenceWriter<TBase>
      output;

      public
      CastingSequenceWriter(ISequenceWriter<TBase> baseWriter) {

         if (baseWriter == null) throw new ArgumentNullException(nameof(baseWriter));

         this.output = baseWriter;
      }

      public void
      WriteObject(TDerived value) =>
         this.output.WriteObject(value);

      public void
      WriteObject(IEnumerable<TDerived> value) {

         if (value != null) {

            foreach (var item in value) {
               WriteObject(item);
            }
         }
      }

      public void
      WriteObject<TDerived2>(IEnumerable<TDerived2> value) where TDerived2 : TDerived =>
         WriteObject(value as IEnumerable<TDerived> ?? value?.Cast<TDerived>());

      public void
      WriteString(TDerived text) => this.output.WriteString(text);

      public void
      WriteRaw(TDerived data) => this.output.WriteRaw(data);

      public void
      WriteComment(string text) => this.output.WriteComment(text);

      public void
      CopyOf(TDerived value) => this.output.CopyOf(value);

      public void
      CopyOf(IEnumerable<TDerived> value) {

         if (value != null) {

            foreach (var item in value) {
               CopyOf(item);
            }
         }
      }

      public void
      CopyOf<TDerived2>(IEnumerable<TDerived2> value) where TDerived2 : TDerived =>
         CopyOf(value as IEnumerable<TDerived> ?? value?.Cast<TDerived>());

      public XcstWriter
      TryCastToDocumentWriter() =>
         this.output.TryCastToDocumentWriter();

      public MapWriter
      TryCastToMapWriter() => this.output.TryCastToMapWriter();
   }
}
