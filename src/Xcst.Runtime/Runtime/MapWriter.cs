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
using System.Collections.ObjectModel;
using System.Dynamic;
using Xcst.PackageModel;

namespace Xcst.Runtime {

   public abstract partial class MapWriter : ISequenceWriter<object> {

      // For object, create ExpandoObjectMapWriter
      // Having a default Create restricted to object avoids conflicts with other
      // implementations, like JObject

      public static MapWriter Create(ISequenceWriter<object> output) {

         MapWriter mapWriter = output.TryCastToMapWriter();

         if (mapWriter != null) {
            return mapWriter;
         }

         XcstWriter docWriter = output.TryCastToDocumentWriter();

         if (docWriter != null) {
            return Create(docWriter);
         }

         return new ExpandoMapWriter((ISequenceWriter<ExpandoObject>)output);
      }

      public static MapWriter CreateArray(ISequenceWriter<object> output) {

         MapWriter mapWriter = output.TryCastToMapWriter();

         if (mapWriter != null) {
            return mapWriter;
         }

         XcstWriter docWriter = output.TryCastToDocumentWriter();

         if (docWriter != null) {
            return Create(docWriter);
         }

         return new ExpandoMapWriter(output);
      }

      // For object, cast to abstract MapWriter

      public static MapWriter Cast(ISequenceWriter<object> output) {

         MapWriter mapWriter = output.TryCastToMapWriter();

         if (mapWriter != null) {
            return mapWriter;
         }

         throw new RuntimeException("Could not cast output to MapWriter.");
      }

      public abstract void WriteStartMap();
      public abstract void WriteStartMapEntry(string key);
      public abstract void WriteEndMapEntry();
      public abstract void WriteEndMap();

      public abstract void WriteStartArray();
      public abstract void WriteEndArray();

      public abstract void WriteComment(string text);

      #region ISequenceWriter<object> Members

      public abstract void WriteObject(object value);

      public virtual void WriteObject(IEnumerable<object> value) {

         if (value != null) {

            foreach (var item in value) {
               WriteObject(item);
            }
         }
      }

      public abstract void WriteString(object text);

      public abstract void WriteRaw(object data);

      public abstract XcstWriter TryCastToDocumentWriter();

      public MapWriter TryCastToMapWriter() => this;

      #endregion

      // IEnumerable<object> works for reference types only
      // IEnumerable for any type

      public void WriteObject(IEnumerable value) {

         if (value != null) {

            foreach (var item in value) {
               WriteObject(item);
            }
         }
      }

      // string implements IEnumerable, treat as single value

      public void WriteObject(string value) {
         WriteObject((object)value);
      }
   }
}
