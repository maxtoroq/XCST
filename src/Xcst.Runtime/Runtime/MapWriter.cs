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
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;

namespace Xcst.Runtime {

   public abstract partial class MapWriter : ISequenceWriter<object?> {

      // For object, create ExpandoObjectMapWriter
      // Having a default Create restricted to object avoids conflicts with other
      // implementations, like JObject

      public static MapWriter
      Create(ISequenceWriter<object> output) {

         if (output.TryCastToMapWriter() is MapWriter mapWriter) {
            return mapWriter;
         }

         if (output.TryCastToDocumentWriter() is XcstWriter docWriter) {
            return Create(docWriter);
         }

         return new ExpandoMapWriter((ISequenceWriter<ExpandoObject>)output);
      }

      public static MapWriter
      CreateArray(ISequenceWriter<object> output) {

         if (output.TryCastToMapWriter() is MapWriter mapWriter) {
            return mapWriter;
         }

         if (output.TryCastToDocumentWriter() is XcstWriter docWriter) {
            return Create(docWriter);
         }

         return new ExpandoMapWriter(output);
      }

      // For object, cast to abstract MapWriter

      public static MapWriter
      CastMapEntry(ISequenceWriter<object> output) {

         MapWriter mapWriter = output.TryCastToMapWriter()
            ?? throw new RuntimeException("Could not cast output to MapWriter.");

         return mapWriter;
      }


      // ## Maps and Arrays

      public abstract void
      WriteStartMap();

      public abstract void
      WriteStartMapEntry(string key);

      public abstract void
      WriteEndMapEntry();

      public abstract void
      WriteEndMap();

      public abstract void
      WriteStartArray();

      public abstract void
      WriteEndArray();

      public abstract void
      WriteComment(string? text);

      public void
      WriteString(string? text) => WriteObject(text);

      public abstract void
      WriteRaw(string? data);

      void
      ISequenceWriter<object?>.WriteString(object? text) =>
         WriteString((string?)text);

      void
      ISequenceWriter<object?>.WriteRaw(object? data) =>
         WriteRaw((string?)data);


      // ## WriteObject

      public abstract void
      WriteObject(object? value);

      void
      ISequenceWriter<object?>.WriteObject(IEnumerable<object?>? value) =>
         WriteObject((IEnumerable?)value);

      void
      ISequenceWriter<object?>.WriteObject<TDerived>(IEnumerable<TDerived>? value) =>
         WriteObject((IEnumerable?)value);

      // string implements IEnumerable, treat as single value
      // IEnumerable<object> works for reference types only, IEnumerable for any type

      public void
      WriteObject(string? value) => WriteObject((object?)value);

      public void
      WriteObject(IEnumerable? value) {

         if (value != null) {

            foreach (var item in value) {
               WriteObject(item);
            }
         }
      }


      // ## CopyOf

      public virtual void
      CopyOf(object? value) => CopyOfImpl(value, recurse: false);

      void
      ISequenceWriter<object?>.CopyOf(IEnumerable<object?>? value) =>
         CopyOf((object?)value);

      void
      ISequenceWriter<object?>.CopyOf<TDerived>(IEnumerable<TDerived>? value) =>
         CopyOf((object?)value);

      void
      CopyOfImpl(object? value, bool recurse) {

         if (value != null) {

            if (TryCopyOf(value)) {
               return;
            }

            if (!recurse) {

               if (SimpleContent.ValueAsEnumerable(value, checkToString: false) is IEnumerable seq) {
                  CopyOfSequence(seq);
                  return;
               }
            }
         }

         WriteObject(value);
      }

      public virtual bool
      TryCopyOf(object? value) => false;

      void
      CopyOfSequence(IEnumerable? value) {

         if (value != null) {

            foreach (var item in value) {
               CopyOfImpl(item, recurse: true);
            }
         }
      }

      public void
      CopyOf(Array? value) => CopyOfSequence(value);


      // ## Other

      public virtual XcstWriter?
      TryCastToDocumentWriter() => null;

      public MapWriter?
      TryCastToMapWriter() => this;
   }
}
