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

      readonly XcstWriter
      _output;

      WriteState
      _state = WriteState.Content;

      public override WriteState
      WriteState => _state;

      public
      XcstXmlWriter(XcstWriter writer) {
         _output = writer;
      }

      public override void
      Flush() { }

      public override string
      LookupPrefix(string ns) =>
         throw new NotImplementedException();

      public override void
      WriteBase64(byte[] buffer, int index, int count) =>
         throw new NotImplementedException();

      public override void
      WriteCData(string? text) {
         _output.WriteString(text);
         _state = WriteState.Content;
      }

      public override void
      WriteCharEntity(char ch) =>
         throw new NotImplementedException();

      public override void
      WriteChars(char[] buffer, int index, int count) {

         _output.WriteChars(buffer, index, count);

         if (_state != WriteState.Attribute) {
            _state = WriteState.Content;
         }
      }

      public override void
      WriteComment(string? text) {
         _output.WriteComment(text);
         _state = WriteState.Content;
      }

      public override void
      WriteDocType(string name, string? pubid, string? sysid, string? subset) { }

      public override void
      WriteEndAttribute() {
         _output.WriteEndAttribute();
         _state = WriteState.Element;
      }

      public override void
      WriteEndDocument() {
         _state = WriteState.Content;
      }

      public override void
      WriteEndElement() {
         _output.WriteEndElement();
         _state = WriteState.Content;
      }

      public override void
      WriteEntityRef(string name) =>
         throw new NotImplementedException();

      public override void
      WriteFullEndElement() {
         _output.WriteEndElement();
         _state = WriteState.Content;
      }

      public override void
      WriteProcessingInstruction(string name, string? text) {
         _output.WriteProcessingInstruction(name, text);
         _state = WriteState.Content;
      }

      public override void
      WriteRaw(char[] buffer, int index, int count) =>
         throw new NotImplementedException();

      public override void
      WriteRaw(string data) {
         _output.WriteRaw(data);
      }

      public override void
      WriteStartAttribute(string? prefix, string localName, string? ns) {
         _output.WriteStartAttribute(prefix, localName, ns);
         _state = WriteState.Attribute;
      }

      public override void
      WriteStartDocument() { }

      public override void
      WriteStartDocument(bool standalone) { }

      public override void
      WriteStartElement(string? prefix, string localName, string? ns) {
         _output.WriteStartElement(prefix, localName, ns);
         _state = WriteState.Element;
      }

      public override void
      WriteString(string? text) {

         _output.WriteString(text);

         if (_state != WriteState.Attribute) {
            _state = WriteState.Content;
         }
      }

      public override void
      WriteSurrogateCharEntity(char lowChar, char highChar) =>
         throw new NotImplementedException();

      public override void
      WriteWhitespace(string? ws) {
         _output.WriteString(ws);
         _state = WriteState.Content;
      }
   }
}
