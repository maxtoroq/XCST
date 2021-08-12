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

#region WrappingXmlWriter is based on code from XMLMVP
// BSD License
// 
// Copyright (c) 2005, XMLMVP Project
// All rights reserved.
//
// Redistribution and use in source and binary forms, with or without 
// modification, are permitted provided that the following conditions 
// are met:
// 
// * Redistributions of source code must retain the above copyright 
// notice, this list of conditions and the following disclaimer. 
// * Redistributions in binary form must reproduce the above copyright 
// notice, this list of conditions and the following disclaimer in 
// the documentation and/or other materials provided with the 
// distribution. 
// * Neither the name of the XMLMVP Project nor the names of its 
// contributors may be used to endorse or promote products derived
// from this software without specific prior written permission. 
// 
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS 
// "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT 
// LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS 
// FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE 
// COPYRIGHT OWNER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, 
// INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, 
// BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; 
// LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER 
// CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT 
// LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN 
// ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE 
// POSSIBILITY OF SUCH DAMAGE.
#endregion

using System;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.XPath;

namespace Xcst.Xml {

   abstract class WrappingXmlWriter : XmlWriter {

      readonly XmlWriter
      _output;

      public override XmlWriterSettings?
      Settings => _output.Settings;

      public override WriteState
      WriteState => _output.WriteState;

      public override string?
      XmlLang => _output.XmlLang;

      public override XmlSpace
      XmlSpace => _output.XmlSpace;

      protected
      WrappingXmlWriter(XmlWriter baseWriter) {

         if (baseWriter is null) throw new ArgumentNullException(nameof(baseWriter));

         _output = baseWriter;
      }

      public override void
      Close() => _output.Close();

      protected override void
      Dispose(bool disposing) {

         if (disposing) {
            _output.Dispose();
         }
      }

      public override void
      Flush() => _output.Flush();

      public override Task
      FlushAsync() => _output.FlushAsync();

      public override string?
      LookupPrefix(string ns) => _output.LookupPrefix(ns);

      public override void
      WriteAttributes(XmlReader reader, bool defattr) =>
         _output.WriteAttributes(reader, defattr);

      public override Task
      WriteAttributesAsync(XmlReader reader, bool defattr) =>
         _output.WriteAttributesAsync(reader, defattr);

      public override void
      WriteBase64(byte[] buffer, int index, int count) =>
         _output.WriteBase64(buffer, index, count);

      public override Task
      WriteBase64Async(byte[] buffer, int index, int count) =>
         _output.WriteBase64Async(buffer, index, count);

      public override void
      WriteBinHex(byte[] buffer, int index, int count) =>
         _output.WriteBinHex(buffer, index, count);

      public override Task
      WriteBinHexAsync(byte[] buffer, int index, int count) =>
         _output.WriteBinHexAsync(buffer, index, count);

      public override void
      WriteCData(string? text) => _output.WriteCData(text);

      public override Task
      WriteCDataAsync(string? text) => _output.WriteCDataAsync(text);

      public override void
      WriteCharEntity(char ch) => _output.WriteCharEntity(ch);

      public override Task
      WriteCharEntityAsync(char ch) => _output.WriteCharEntityAsync(ch);

      public override void
      WriteChars(char[] buffer, int index, int count) =>
         _output.WriteChars(buffer, index, count);

      public override Task
      WriteCharsAsync(char[] buffer, int index, int count) =>
         _output.WriteCharsAsync(buffer, index, count);

      public override void
      WriteComment(string? text) => _output.WriteComment(text);

      public override Task
      WriteCommentAsync(string? text) =>
         _output.WriteCommentAsync(text);

      public override void
      WriteDocType(string name, string? pubid, string? sysid, string? subset) =>
         _output.WriteDocType(name, pubid, sysid, subset);

      public override Task
      WriteDocTypeAsync(string name, string? pubid, string? sysid, string? subset) =>
         _output.WriteDocTypeAsync(name, pubid, sysid, subset);

      public override void
      WriteEndAttribute() => _output.WriteEndAttribute();

      public override void
      WriteEndDocument() => _output.WriteEndDocument();

      public override Task
      WriteEndDocumentAsync() => _output.WriteEndDocumentAsync();

      public override void
      WriteEndElement() => _output.WriteEndElement();

      public override Task
      WriteEndElementAsync() => _output.WriteEndElementAsync();

      public override void
      WriteEntityRef(string name) => _output.WriteEntityRef(name);

      public override Task
      WriteEntityRefAsync(string name) => _output.WriteEntityRefAsync(name);

      public override void
      WriteFullEndElement() => _output.WriteFullEndElement();

      public override Task
      WriteFullEndElementAsync() => _output.WriteFullEndElementAsync();

      public override void
      WriteName(string name) => _output.WriteName(name);

      public override Task
      WriteNameAsync(string name) => _output.WriteNameAsync(name);

      public override void
      WriteNmToken(string name) => _output.WriteNmToken(name);

      public override Task
      WriteNmTokenAsync(string name) => _output.WriteNmTokenAsync(name);

      public override void
      WriteNode(XPathNavigator navigator, bool defattr) =>
         _output.WriteNode(navigator, defattr);

      public override Task
      WriteNodeAsync(XPathNavigator navigator, bool defattr) =>
         _output.WriteNodeAsync(navigator, defattr);

      public override void
      WriteNode(XmlReader reader, bool defattr) =>
         _output.WriteNode(reader, defattr);

      public override Task
      WriteNodeAsync(XmlReader reader, bool defattr) =>
         _output.WriteNodeAsync(reader, defattr);

      public override void
      WriteProcessingInstruction(string name, string? text) =>
         _output.WriteProcessingInstruction(name, text);

      public override Task
      WriteProcessingInstructionAsync(string name, string? text) =>
         _output.WriteProcessingInstructionAsync(name, text);

      public override void
      WriteQualifiedName(string localName, string? ns) =>
         _output.WriteQualifiedName(localName, ns);

      public override Task
      WriteQualifiedNameAsync(string localName, string? ns) =>
         _output.WriteQualifiedNameAsync(localName, ns);

      public override void
      WriteRaw(string data) => _output.WriteRaw(data);

      public override Task
      WriteRawAsync(string data) => _output.WriteRawAsync(data);

      public override void
      WriteRaw(char[] buffer, int index, int count) =>
         _output.WriteRaw(buffer, index, count);

      public override Task
      WriteRawAsync(char[] buffer, int index, int count) =>
         _output.WriteRawAsync(buffer, index, count);

      public override void
      WriteStartAttribute(string? prefix, string localName, string? ns) =>
         _output.WriteStartAttribute(prefix, localName, ns);

      public override Task
      WriteStartDocumentAsync() => _output.WriteStartDocumentAsync();

      public override void
      WriteStartDocument() => _output.WriteStartDocument();

      public override void
      WriteStartDocument(bool standalone) =>
         _output.WriteStartDocument(standalone);

      public override Task
      WriteStartDocumentAsync(bool standalone) =>
         _output.WriteStartDocumentAsync(standalone);

      public override void
      WriteStartElement(string? prefix, string localName, string? ns) =>
         _output.WriteStartElement(prefix, localName, ns);

      public override Task
      WriteStartElementAsync(string? prefix, string localName, string? ns) =>
         _output.WriteStartElementAsync(prefix, localName, ns);

      public override void
      WriteString(string? text) => _output.WriteString(text);

      public override Task
      WriteStringAsync(string? text) =>
         _output.WriteStringAsync(text);

      public override void
      WriteSurrogateCharEntity(char lowChar, char highChar) =>
         _output.WriteSurrogateCharEntity(lowChar, highChar);

      public override Task
      WriteSurrogateCharEntityAsync(char lowChar, char highChar) =>
         _output.WriteSurrogateCharEntityAsync(lowChar, highChar);

      public override void
      WriteValue(bool value) => _output.WriteValue(value);

      public override void
      WriteValue(DateTime value) => _output.WriteValue(value);

      public override void
      WriteValue(DateTimeOffset value) => _output.WriteValue(value);

      public override void
      WriteValue(decimal value) => _output.WriteValue(value);

      public override void
      WriteValue(double value) => _output.WriteValue(value);

      public override void
      WriteValue(int value) => _output.WriteValue(value);

      public override void
      WriteValue(long value) => _output.WriteValue(value);

      public override void
      WriteValue(object value) => _output.WriteValue(value);

      public override void
      WriteValue(float value) => _output.WriteValue(value);

      public override void
      WriteValue(string? value) => _output.WriteValue(value);

      public override void
      WriteWhitespace(string? ws) => _output.WriteWhitespace(ws);

      public override Task
      WriteWhitespaceAsync(string? ws) =>
         _output.WriteWhitespaceAsync(ws);
   }
}
