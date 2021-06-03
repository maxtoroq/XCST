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
using System.Diagnostics;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Xcst.Runtime {

   public partial class MapWriter {

      // For the default output, create a JsonMapWriter

      public static JsonMapWriter
      Create(XcstWriter output) =>
         new JsonMapWriter(new JsonTextWriter(new XcstTextWriter(output)), output);

      public static JsonMapWriter
      CreateArray(XcstWriter output) =>
         CreateArray((ISequenceWriter<JArray>)output);

      // From object to JObject, create a JsonMapWriter

      public static JsonMapWriter
      Create(ISequenceWriter<JObject> output) {

         if (TryCast(output) is JsonMapWriter mapWriter) {
            return mapWriter;
         }

         if (output.TryCastToDocumentWriter() is XcstWriter docWriter) {
            return Create(docWriter);
         }

         return new JsonMapWriter(output);
      }

      // From object to JArray, create a JsonMapWriter

      public static JsonMapWriter
      CreateArray(ISequenceWriter<JArray> output) {

         if (TryCast(output) is JsonMapWriter mapWriter) {
            return mapWriter;
         }

         if (output.TryCastToDocumentWriter() is XcstWriter docWriter) {
            return Create(docWriter);
         }

         return new JsonMapWriter(output);
      }

      // From object to JProperty, cast to JsonMapWriter

      public static JsonMapWriter
      CastMapEntry(ISequenceWriter<JProperty> output) {

         JsonMapWriter mapWriter = TryCast(output)
            ?? throw new RuntimeException("Could not cast output to JsonMapWriter.");

         return mapWriter;
      }

      static JsonMapWriter?
      TryCast<TItem>(ISequenceWriter<TItem> output) where TItem : JToken {

         MapWriter? mapWriter = output.TryCastToMapWriter();

         if (mapWriter is null) {
            return null;
         }

         if (mapWriter is JsonMapWriter jsonWriter) {
            return jsonWriter;
         }

         throw new RuntimeException("Cannot mix MapWriter implementations.");
      }
   }

   public class JsonMapWriter : MapWriter {

      readonly XcstWriter?
      _docWriter;

      readonly ISequenceWriter<JObject>?
      _mapOutput;

      readonly ISequenceWriter<JArray>?
      _arrayOutput;

      protected JsonWriter
      BaseWriter { get; }

      public
      JsonMapWriter(JsonWriter baseWriter, XcstWriter? docWriter) {

         if (baseWriter is null) throw new ArgumentNullException(nameof(baseWriter));

         this.BaseWriter = baseWriter;
         _docWriter = docWriter;
      }

      public
      JsonMapWriter(ISequenceWriter<JObject> output)
         : this(new JTokenWriter(), null) {

         Debug.Assert(output.TryCastToDocumentWriter() is null);

         _mapOutput = output;
      }

      public
      JsonMapWriter(ISequenceWriter<JArray> output)
         : this(new JTokenWriter(), null) {

         Debug.Assert(output.TryCastToDocumentWriter() is null);

         _arrayOutput = output;
      }

      public override void
      WriteComment(string? text) =>
         this.BaseWriter.WriteComment(text);

      public override void
      WriteEndArray() {

         // WriteEndArray is called in a finally block
         // Checking for error to not overwhelm writer when something goes wrong

         if (this.BaseWriter.WriteState != WriteState.Error) {
            this.BaseWriter.WriteEndArray();
         }
      }

      public override void
      WriteEndMap() {

         // WriteEndMap is called in a finally block
         // Checking for error to not overwhelm writer when something goes wrong

         if (this.BaseWriter.WriteState != WriteState.Error) {
            this.BaseWriter.WriteEndObject();
         }
      }

      public override void
      WriteEndMapEntry() {

         // If WriteState is still Property means no value was written
         // Properties MUST have a value, so writing null

         if (this.BaseWriter.WriteState == WriteState.Property) {
            this.BaseWriter.WriteNull();
         }
      }

      public override void
      WriteStartArray() {

         bool firstCall = this.BaseWriter.WriteState == WriteState.Start;

         this.BaseWriter.WriteStartArray();
         OnItemWritten();

         if (firstCall
            && _arrayOutput != null) {

            _arrayOutput.WriteObject((JArray)((JTokenWriter)this.BaseWriter).Token);
         }
      }

      public override void
      WriteStartMap() {

         bool firstCall = this.BaseWriter.WriteState == WriteState.Start;

         this.BaseWriter.WriteStartObject();
         OnItemWritten();

         if (firstCall
            && _mapOutput != null) {

            _mapOutput.WriteObject((JObject)((JTokenWriter)this.BaseWriter).Token);
         }
      }

      public override void
      WriteStartMapEntry(string key) {
         this.BaseWriter.WritePropertyName(key);
         OnItemWritten();
      }

      public override void
      WriteObject(object? value) {
         this.BaseWriter.WriteValue(value);
         OnItemWritten();
      }

      public override void
      WriteRaw(string? data) {

         this.BaseWriter.WriteRaw(data);

         if (!String.IsNullOrEmpty(data)) {
            OnItemWritten();
         }
      }

      public override XcstWriter?
      TryCastToDocumentWriter() => _docWriter;

      public void
      CopyOf(JToken? value) {

         if (value != null) {
            value.WriteTo(this.BaseWriter);
            OnItemWritten();
         } else {
            WriteObject(default(object));
         }
      }

      public override bool
      TryCopyOf(object? value) {

         if (value is JToken jt) {
            CopyOf(jt);
            return true;
         }

         return base.TryCopyOf(value);
      }
   }
}
