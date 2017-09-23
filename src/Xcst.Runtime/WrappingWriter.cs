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
using Xcst.Runtime;

namespace Xcst {

   abstract class WrappingWriter : XcstWriter {

      readonly XcstWriter baseWriter;

      public override Uri OutputUri => baseWriter.OutputUri;

      public override SimpleContent SimpleContent {
         get {
            return baseWriter.SimpleContent;
         }
         internal set {
            baseWriter.SimpleContent = value;
         }
      }

      protected WrappingWriter(XcstWriter baseWriter)
         : base(baseWriter.OutputUri) {

         if (baseWriter == null) throw new ArgumentNullException(nameof(baseWriter));

         this.baseWriter = baseWriter;
      }

      public override void WriteChars(char[] buffer, int index, int count) {
         this.baseWriter.WriteChars(buffer, index, count);
      }

      public override void WriteComment(string text) {
         this.baseWriter.WriteComment(text);
      }

      public override void WriteEndAttribute() {
         this.baseWriter.WriteEndAttribute();
      }

      public override void WriteEndElement() {
         this.baseWriter.WriteEndElement();
      }

      public override void WriteProcessingInstruction(string name, string text) {
         this.baseWriter.WriteProcessingInstruction(name, text);
      }

      public override void WriteRaw(string data) {
         this.baseWriter.WriteRaw(data);
      }

      public override void WriteStartAttribute(string prefix, string localName, string ns, string separator) {
         this.baseWriter.WriteStartAttribute(prefix, localName, ns, separator);
      }

      public override void WriteStartElement(string prefix, string localName, string ns) {
         this.baseWriter.WriteStartElement(prefix, localName, ns);
      }

      public override void WriteString(string text) {
         this.baseWriter.WriteString(text);
      }

      protected internal override void WriteItem(object value) {
         this.baseWriter.WriteItem(value);
      }

      public override void Flush() {
         this.baseWriter.Flush();
      }

      protected override void Dispose(bool disposing) {

         if (disposing) {
            this.baseWriter.Dispose();
         }

         base.Dispose(disposing);
      }
   }
}
