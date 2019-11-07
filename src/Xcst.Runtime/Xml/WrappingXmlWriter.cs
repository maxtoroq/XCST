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
      output;

      public override XmlWriterSettings
      Settings => this.output.Settings;

      public override WriteState
      WriteState => this.output.WriteState;

      public override string
      XmlLang => this.output.XmlLang;

      public override XmlSpace
      XmlSpace => this.output.XmlSpace;

      protected
      WrappingXmlWriter(XmlWriter baseWriter) {

         if (baseWriter is null) throw new ArgumentNullException(nameof(baseWriter));

         this.output = baseWriter;
      }

      public override void
      Close() => this.output.Close();

      protected override void
      Dispose(bool disposing) {

         if (disposing) {
            this.output.Dispose();
         }
      }

      public override void
      Flush() => this.output.Flush();

      public override Task
      FlushAsync() => this.output.FlushAsync();

      public override string
      LookupPrefix(string ns) => this.output.LookupPrefix(ns);

      public override void
      WriteAttributes(XmlReader reader, bool defattr) =>
         this.output.WriteAttributes(reader, defattr);

      public override Task
      WriteAttributesAsync(XmlReader reader, bool defattr) =>
         this.output.WriteAttributesAsync(reader, defattr);

      public override void
      WriteBase64(byte[] buffer, int index, int count) =>
         this.output.WriteBase64(buffer, index, count);

      public override Task
      WriteBase64Async(byte[] buffer, int index, int count) =>
         this.output.WriteBase64Async(buffer, index, count);

      public override void
      WriteBinHex(byte[] buffer, int index, int count) =>
         this.output.WriteBinHex(buffer, index, count);

      public override Task
      WriteBinHexAsync(byte[] buffer, int index, int count) =>
         this.output.WriteBinHexAsync(buffer, index, count);

      public override void
      WriteCData(string text) => this.output.WriteCData(text);

      public override Task
      WriteCDataAsync(string text) => this.output.WriteCDataAsync(text);

      public override void
      WriteCharEntity(char ch) => this.output.WriteCharEntity(ch);

      public override Task
      WriteCharEntityAsync(char ch) => this.output.WriteCharEntityAsync(ch);

      public override void
      WriteChars(char[] buffer, int index, int count) =>
         this.output.WriteChars(buffer, index, count);

      public override Task
      WriteCharsAsync(char[] buffer, int index, int count) =>
         this.output.WriteCharsAsync(buffer, index, count);

      public override void
      WriteComment(string text) => this.output.WriteComment(text);

      public override Task
      WriteCommentAsync(string text) =>
         this.output.WriteCommentAsync(text);

      public override void
      WriteDocType(string name, string? pubid, string? sysid, string? subset) =>
         this.output.WriteDocType(name, pubid, sysid, subset);

      public override Task
      WriteDocTypeAsync(string name, string pubid, string sysid, string subset) =>
         this.output.WriteDocTypeAsync(name, pubid, sysid, subset);

      public override void
      WriteEndAttribute() => this.output.WriteEndAttribute();

      public override void
      WriteEndDocument() => this.output.WriteEndDocument();

      public override Task
      WriteEndDocumentAsync() => this.output.WriteEndDocumentAsync();

      public override void
      WriteEndElement() => this.output.WriteEndElement();

      public override Task
      WriteEndElementAsync() => this.output.WriteEndElementAsync();

      public override void
      WriteEntityRef(string name) => this.output.WriteEntityRef(name);

      public override Task
      WriteEntityRefAsync(string name) => this.output.WriteEntityRefAsync(name);

      public override void
      WriteFullEndElement() => this.output.WriteFullEndElement();

      public override Task
      WriteFullEndElementAsync() => this.output.WriteFullEndElementAsync();

      public override void
      WriteName(string name) => this.output.WriteName(name);

      public override Task
      WriteNameAsync(string name) => this.output.WriteNameAsync(name);

      public override void
      WriteNmToken(string name) => this.output.WriteNmToken(name);

      public override Task
      WriteNmTokenAsync(string name) => this.output.WriteNmTokenAsync(name);

      public override void
      WriteNode(XPathNavigator navigator, bool defattr) =>
         this.output.WriteNode(navigator, defattr);

      public override Task
      WriteNodeAsync(XPathNavigator navigator, bool defattr) =>
         this.output.WriteNodeAsync(navigator, defattr);

      public override void
      WriteNode(XmlReader reader, bool defattr) =>
         this.output.WriteNode(reader, defattr);

      public override Task
      WriteNodeAsync(XmlReader reader, bool defattr) =>
         this.output.WriteNodeAsync(reader, defattr);

      public override void
      WriteProcessingInstruction(string name, string text) =>
         this.output.WriteProcessingInstruction(name, text);

      public override Task
      WriteProcessingInstructionAsync(string name, string text) =>
         this.output.WriteProcessingInstructionAsync(name, text);

      public override void
      WriteQualifiedName(string localName, string ns) =>
         this.output.WriteQualifiedName(localName, ns);

      public override Task
      WriteQualifiedNameAsync(string localName, string ns) =>
         this.output.WriteQualifiedNameAsync(localName, ns);

      public override void
      WriteRaw(string data) => this.output.WriteRaw(data);

      public override Task
      WriteRawAsync(string data) => this.output.WriteRawAsync(data);

      public override void
      WriteRaw(char[] buffer, int index, int count) =>
         this.output.WriteRaw(buffer, index, count);

      public override Task
      WriteRawAsync(char[] buffer, int index, int count) =>
         this.output.WriteRawAsync(buffer, index, count);

      public override void
      WriteStartAttribute(string prefix, string localName, string ns) =>
         this.output.WriteStartAttribute(prefix, localName, ns);

      public override Task
      WriteStartDocumentAsync() => this.output.WriteStartDocumentAsync();

      public override void
      WriteStartDocument() => this.output.WriteStartDocument();

      public override void
      WriteStartDocument(bool standalone) =>
         this.output.WriteStartDocument(standalone);

      public override Task
      WriteStartDocumentAsync(bool standalone) =>
         this.output.WriteStartDocumentAsync(standalone);

      public override void
      WriteStartElement(string prefix, string localName, string ns) =>
         this.output.WriteStartElement(prefix, localName, ns);

      public override Task
      WriteStartElementAsync(string prefix, string localName, string ns) =>
         this.output.WriteStartElementAsync(prefix, localName, ns);

      public override void
      WriteString(string text) => this.output.WriteString(text);

      public override Task
      WriteStringAsync(string text) =>
         this.output.WriteStringAsync(text);

      public override void
      WriteSurrogateCharEntity(char lowChar, char highChar) =>
         this.output.WriteSurrogateCharEntity(lowChar, highChar);

      public override Task
      WriteSurrogateCharEntityAsync(char lowChar, char highChar) =>
         this.output.WriteSurrogateCharEntityAsync(lowChar, highChar);

      public override void WriteValue(bool value) =>
         this.output.WriteValue(value);

      public override void
      WriteValue(DateTime value) => this.output.WriteValue(value);

      public override void
      WriteValue(DateTimeOffset value) => this.output.WriteValue(value);

      public override void
      WriteValue(decimal value) => this.output.WriteValue(value);

      public override void
      WriteValue(double value) => this.output.WriteValue(value);

      public override void
      WriteValue(int value) => this.output.WriteValue(value);

      public override void
      WriteValue(long value) => this.output.WriteValue(value);

      public override void
      WriteValue(object value) => this.output.WriteValue(value);

      public override void
      WriteValue(float value) => this.output.WriteValue(value);

      public override void
      WriteValue(string value) => this.output.WriteValue(value);

      public override void
      WriteWhitespace(string ws) => this.output.WriteWhitespace(ws);

      public override Task
      WriteWhitespaceAsync(string ws) =>
         this.output.WriteWhitespaceAsync(ws);
   }
}
