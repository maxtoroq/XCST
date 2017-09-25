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
using System.Xml;

namespace Xcst.Xml {

   class XcstXmlWriter : XmlWriter {

      readonly XcstWriter writer;
      WriteState state = WriteState.Content;

      public override WriteState WriteState => state;

      public XcstXmlWriter(XcstWriter writer) {
         this.writer = writer;
      }

      public override void Flush() { }

      public override string LookupPrefix(string ns) {
         throw new NotImplementedException();
      }

      public override void WriteBase64(byte[] buffer, int index, int count) {
         throw new NotImplementedException();
      }

      public override void WriteCData(string text) {
         this.writer.WriteString(text);
         this.state = WriteState.Content;
      }

      public override void WriteCharEntity(char ch) {
         throw new NotImplementedException();
      }

      public override void WriteChars(char[] buffer, int index, int count) {

         this.writer.WriteChars(buffer, index, count);

         if (this.state != WriteState.Attribute) {
            this.state = WriteState.Content;
         }
      }

      public override void WriteComment(string text) {
         this.writer.WriteComment(text);
         this.state = WriteState.Content;
      }

      public override void WriteDocType(string name, string pubid, string sysid, string subset) { }

      public override void WriteEndAttribute() {
         this.writer.WriteEndAttribute();
         this.state = WriteState.Element;
      }

      public override void WriteEndDocument() {
         this.state = WriteState.Content;
      }

      public override void WriteEndElement() {
         this.writer.WriteEndElement();
         this.state = WriteState.Content;
      }

      public override void WriteEntityRef(string name) {
         throw new NotImplementedException();
      }

      public override void WriteFullEndElement() {
         this.writer.WriteEndElement();
         this.state = WriteState.Content;
      }

      public override void WriteProcessingInstruction(string name, string text) {
         this.writer.WriteProcessingInstruction(name, text);
         this.state = WriteState.Content;
      }

      public override void WriteRaw(char[] buffer, int index, int count) {
         throw new NotImplementedException();
      }

      public override void WriteRaw(string data) {
         this.writer.WriteRaw(data);
      }

      public override void WriteStartAttribute(string prefix, string localName, string ns) {
         this.writer.WriteStartAttribute(prefix, localName, ns);
         this.state = WriteState.Attribute;
      }

      public override void WriteStartDocument() { }

      public override void WriteStartDocument(bool standalone) { }

      public override void WriteStartElement(string prefix, string localName, string ns) {
         this.writer.WriteStartElement(prefix, localName, ns);
         this.state = WriteState.Element;
      }

      public override void WriteString(string text) {

         this.writer.WriteString(text);

         if (this.state != WriteState.Attribute) {
            this.state = WriteState.Content;
         }
      }

      public override void WriteSurrogateCharEntity(char lowChar, char highChar) {
         throw new NotImplementedException();
      }

      public override void WriteWhitespace(string ws) {
         this.writer.WriteString(ws);
         this.state = WriteState.Content;
      }
   }
}
