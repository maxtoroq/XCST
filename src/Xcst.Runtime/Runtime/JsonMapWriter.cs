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
using System.Diagnostics;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xcst.PackageModel;

namespace Xcst.Runtime {

   public partial class MapWriter {

      // For the default output, create a JsonMapWriter

      public static JsonMapWriter Create(XcstWriter output) {
         return new JsonMapWriter(new JsonTextWriter(new XcstTextWriter(output)), output);
      }

      public static JsonMapWriter CreateArray(XcstWriter output) {
         return CreateArray((ISequenceWriter<JArray>)output);
      }

      // From object to JObject, create a JsonMapWriter

      public static JsonMapWriter Create(ISequenceWriter<JObject> output) {

         JsonMapWriter mapWriter = TryCast(output);

         if (mapWriter != null) {
            return mapWriter;
         }

         XcstWriter docWriter = output.TryCastToDocumentWriter();

         if (docWriter != null) {
            return Create(docWriter);
         }

         return new JsonMapWriter(output);
      }

      // From object to JArray, create a JsonMapWriter

      public static JsonMapWriter CreateArray(ISequenceWriter<JArray> output) {

         JsonMapWriter mapWriter = TryCast(output);

         if (mapWriter != null) {
            return mapWriter;
         }

         XcstWriter docWriter = output.TryCastToDocumentWriter();

         if (docWriter != null) {
            return Create(docWriter);
         }

         return new JsonMapWriter(output);
      }

      // From object to JProperty, cast to JsonMapWriter

      public static JsonMapWriter CastMapEntry(ISequenceWriter<JProperty> output) {

         JsonMapWriter mapWriter = TryCast(output);

         if (mapWriter != null) {
            return mapWriter;
         }

         throw new RuntimeException("Could not cast output to JsonMapWriter.");
      }

      static JsonMapWriter/*?*/ TryCast<TItem>(ISequenceWriter<TItem> output) where TItem : JToken {

         MapWriter mapWriter = output.TryCastToMapWriter();

         if (mapWriter == null) {
            return null;
         }

         var jsonWriter = mapWriter as JsonMapWriter;

         if (jsonWriter != null) {
            return jsonWriter;
         }

         throw new RuntimeException("Cannot mix MapWriter implementations.");
      }
   }

   public class JsonMapWriter : MapWriter {

      readonly XcstWriter docWriter;
      readonly ISequenceWriter<JObject> mapOutput;
      readonly ISequenceWriter<JArray> arrayOutput;

      protected JsonWriter BaseWriter { get; }

      public JsonMapWriter(JsonWriter baseWriter, XcstWriter/*?*/ docWriter) {

         if (baseWriter == null) throw new ArgumentNullException(nameof(baseWriter));

         this.BaseWriter = baseWriter;
         this.docWriter = docWriter;
      }

      public JsonMapWriter(ISequenceWriter<JObject> output)
         : this(new JTokenWriter(), null) {

         Debug.Assert(output.TryCastToDocumentWriter() == null);

         this.mapOutput = output;
      }

      public JsonMapWriter(ISequenceWriter<JArray> output)
         : this(new JTokenWriter(), null) {

         Debug.Assert(output.TryCastToDocumentWriter() == null);

         this.arrayOutput = output;
      }

      public override void WriteComment(string text) {
         this.BaseWriter.WriteComment(text);
      }

      public override void WriteEndArray() {

         // WriteEndArray is called in a finally block
         // Checking for error to not overwhelm writer when something goes wrong

         if (this.BaseWriter.WriteState != WriteState.Error) {
            this.BaseWriter.WriteEndArray();
         }
      }

      public override void WriteEndMap() {

         // WriteEndMap is called in a finally block
         // Checking for error to not overwhelm writer when something goes wrong

         if (this.BaseWriter.WriteState != WriteState.Error) {
            this.BaseWriter.WriteEndObject();
         }
      }

      public override void WriteEndMapEntry() {

         // If WriteState is still Property means no value was written
         // Properties MUST have a value, so writing null

         if (this.BaseWriter.WriteState == WriteState.Property) {
            this.BaseWriter.WriteNull();
         }
      }

      public override void WriteStartArray() {

         bool firstCall = this.BaseWriter.WriteState == WriteState.Start;

         this.BaseWriter.WriteStartArray();

         if (firstCall
            && this.arrayOutput != null) {

            this.arrayOutput.WriteObject((JArray)((JTokenWriter)this.BaseWriter).Token);
         }
      }

      public override void WriteStartMap() {

         bool firstCall = this.BaseWriter.WriteState == WriteState.Start;

         this.BaseWriter.WriteStartObject();

         if (firstCall
            && this.mapOutput != null) {

            this.mapOutput.WriteObject((JObject)((JTokenWriter)this.BaseWriter).Token);
         }
      }

      public override void WriteStartMapEntry(string key) {
         this.BaseWriter.WritePropertyName(key);
      }

      public override void WriteObject(object value) {
         this.BaseWriter.WriteValue(value);
      }

      public override void WriteRaw(string data) {
         this.BaseWriter.WriteRaw(data);
      }

      public override XcstWriter TryCastToDocumentWriter() {
         return this.docWriter;
      }

      public void CopyOf(JToken value) {

         if (value != null) {
            value.WriteTo(this.BaseWriter);
         } else {
            WriteObject(default(object));
         }
      }

      public override bool TryCopyOf(object value) {

         JToken jt = value as JToken;

         if (jt != null) {
            CopyOf(jt);
            return true;
         }

         return base.TryCopyOf(value);
      }
   }
}
