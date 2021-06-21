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

   public abstract class BaseSequenceWriter<TItem> : ISequenceWriter<TItem> {

      Stack<SequenceConstructor.State>?
      _trackStack;

      public abstract void
      WriteObject(TItem value);

      public void
      WriteObject(IEnumerable<TItem>? value) {

         if (value != null) {

            foreach (var item in value) {
               WriteObject(item);
            }
         }
      }

      public void
      WriteObject<TDerived>(IEnumerable<TDerived>? value) where TDerived : TItem =>
         WriteObject(value as IEnumerable<TItem> ?? value?.Cast<TItem>());

      public void
      WriteString(TItem text) => WriteObject(text);

      public void
      WriteRaw(TItem data) => WriteObject(data);

      public void
      WriteComment(string? text) { }

      public abstract void
      CopyOf(TItem value);

      public void
      CopyOf(IEnumerable<TItem>? value) {

         if (value != null) {

            foreach (var item in value) {
               CopyOf(item);
            }
         }
      }

      public void
      CopyOf<TDerived>(IEnumerable<TDerived>? value) where TDerived : TItem =>
         CopyOf(value as IEnumerable<TItem> ?? value?.Cast<TItem>());

      public virtual XcstWriter?
      TryCastToDocumentWriter() => null;

      public virtual MapWriter?
      TryCastToMapWriter() => null;

      public virtual void
      BeginTrack(char cardinality) =>
         SequenceConstructor.BeginTrack(cardinality, ref _trackStack);

      protected void
      OnItemWritten() => SequenceConstructor.OnItemWritten(_trackStack);

      public virtual bool
      OnEmpty() => SequenceConstructor.OnEmpty(_trackStack);

      public virtual void
      EndOfConstructor() => SequenceConstructor.EndOfConstructor(_trackStack);

      public virtual void
      EndTrack() => SequenceConstructor.EndTrack(_trackStack);
   }

   /// <exclude/>
   public class SequenceWriter<TItem> : BaseSequenceWriter<TItem> {

      readonly ICollection<TItem>
      _buffer;

      public
      SequenceWriter()
         : this(new List<TItem>()) { }

      public
      SequenceWriter(ICollection<TItem> buffer) {

         if (buffer is null) throw new ArgumentNullException(nameof(buffer));

         _buffer = buffer;
      }

      public override void
      WriteObject(TItem value) {
         _buffer.Add(value);
         OnItemWritten();
      }

      public override void
      CopyOf(TItem value) =>
         WriteObject(DeepCopy.CopyDynamically(value));

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
            Func<TDerived>? forTypeInference = null) where TDerived : TItem {

         ISequenceWriter<TDerived> derivedWriter = SequenceWriter.AdjustWriter<TItem, TDerived>(this);

         template(context, derivedWriter);

         return this;
      }

      public SequenceWriter<TItem>
      WriteTemplateWithParams<TParams>(
            Action<TemplateContext<TParams>, ISequenceWriter<TItem>> template,
            TemplateContext<TParams> context) {

         template(context, this);

         return this;
      }

      public SequenceWriter<TItem>
      WriteTemplateWithParams<TDerived, TParams>(
            Action<TemplateContext<TParams>, ISequenceWriter<TDerived>> template,
            TemplateContext<TParams> context,
            Func<TDerived>? forTypeInference = null) where TDerived : TItem {

         ISequenceWriter<TDerived> derivedWriter = SequenceWriter.AdjustWriter<TItem, TDerived>(this);

         template(context, derivedWriter);

         return this;
      }

      public TItem[]
      Flush() {

         TItem[] seq = (_buffer as List<TItem>)?.ToArray()
            ?? _buffer.ToArray();

         _buffer.Clear();

         return seq;
      }

      public TItem
      FlushSingle() => Flush().Single();
   }

   /// <exclude/>
   public static class SequenceWriter {

      public static SequenceWriter<TItem>
      Create<TItem>(Func<TItem>? forTypeInference = null) =>
         new SequenceWriter<TItem>();

      public static ISequenceWriter<TDerived>
      AdjustWriter<TBase, TDerived>(
            ISequenceWriter<TBase> output,
            Func<TDerived>? forTypeInference = null) where TDerived : TBase {

         if (output is null) throw new ArgumentNullException(nameof(output));

         return output as ISequenceWriter<TDerived>
            ?? new DerivedSequenceWriter<TDerived, TBase>(output);
      }

      public static ISequenceWriter<TDerived>
      AdjustWriterDynamically<TBase, TDerived>(
            ISequenceWriter<TBase> output,
            Func<TDerived>? forTypeInference = null) {

         if (output is null) throw new ArgumentNullException(nameof(output));

         if (output is ISequenceWriter<TDerived> derivedWriter) {
            return derivedWriter;
         }

         if (typeof(TBase).IsAssignableFrom(typeof(TDerived))) {

            return (ISequenceWriter<TDerived>)Activator.CreateInstance(typeof(DerivedSequenceWriter<,>)
               .MakeGenericType(typeof(TDerived), typeof(TBase)), output);
         }

         throw new RuntimeException($"{typeof(TDerived).FullName} is not compatible with {typeof(TBase).FullName}.");
      }

      public static object?
      DefaultInfer() => throw DynamicError.InferMethodIsNotMeantToBeCalled();

      public static XcstDelegate<TBase>
      CastDelegate<TBase, TDerived>(XcstDelegate<TDerived> del) where TDerived : TBase =>
         (c, o) => del(c, new DerivedSequenceWriter<TDerived, TBase>(o));
   }

   class DerivedSequenceWriter<TDerived, TBase> : BaseSequenceWriter<TDerived> where TDerived : TBase {

      readonly ISequenceWriter<TBase>
      _output;

      public
      DerivedSequenceWriter(ISequenceWriter<TBase> baseWriter) {

         if (baseWriter is null) throw new ArgumentNullException(nameof(baseWriter));

         _output = baseWriter;
      }

      public override void
      WriteObject(TDerived value) =>
         _output.WriteObject(value);

      public override void
      CopyOf(TDerived value) =>
         _output.CopyOf(value);

      public override XcstWriter?
      TryCastToDocumentWriter() =>
         _output.TryCastToDocumentWriter();

      public override MapWriter?
      TryCastToMapWriter() => _output.TryCastToMapWriter();

      public override void
      BeginTrack(char cardinality) => _output.BeginTrack(cardinality);

      public override bool
      OnEmpty() => _output.OnEmpty();

      public override void
      EndOfConstructor() => _output.EndOfConstructor();

      public override void
      EndTrack() => _output.EndTrack();
   }

   class StreamedSequenceWriter<TItem> : BaseSequenceWriter<TItem> {

      readonly Action<TItem>
      _outputFn;

      public
      StreamedSequenceWriter(Action<TItem> outputFn) {

         if (outputFn is null) throw new ArgumentNullException(nameof(outputFn));

         _outputFn = outputFn;
      }

      public override void
      WriteObject(TItem value) {
         OnItemWritten();
         _outputFn(value);
      }

      public override void
      CopyOf(TItem value) =>
         WriteObject(DeepCopy.CopyDynamically(value));
   }
}
