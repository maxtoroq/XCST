// Copyright 2015 Max Toro Q.
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
using System.Xml;

namespace Xcst.Xml {

   class XmlXcstWriter : XcstWriter {

      readonly XmlWriter writer;

      public XmlXcstWriter(XmlWriter writer, Uri outputUri)
         : base(outputUri) {

         this.writer = writer;
      }

      public override void WriteComment(string text) {
         this.writer.WriteComment(text);
      }

      public override void WriteEndAttribute() {
         this.writer.WriteEndAttribute();
      }

      public override void WriteEndElement() {
         this.writer.WriteEndElement();
      }

      public override void WriteProcessingInstruction(string name, string text) {
         this.writer.WriteProcessingInstruction(name, text);
      }

      public override void WriteRaw(string data) {
         this.writer.WriteRaw(data);
      }

      public override void WriteStartAttribute(string prefix, string localName, string ns) {
         this.writer.WriteStartAttribute(prefix, localName, ns);
      }

      public override void WriteStartElement(string prefix, string localName, string ns) {
         this.writer.WriteStartElement(prefix, localName, ns);
      }

      public override void WriteString(string text) {
         this.writer.WriteString(text);
      }

      public override void Flush() {
         this.writer.Flush();
      }

      protected override void Dispose(bool disposing) {

         if (disposing) {
            this.writer.Dispose();
         }

         base.Dispose(disposing);
      }
   }
}
