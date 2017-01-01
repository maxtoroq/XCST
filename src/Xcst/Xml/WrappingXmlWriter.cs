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

      readonly XmlWriter baseWriter;

      protected WrappingXmlWriter(XmlWriter baseWriter) {

         if (baseWriter == null) throw new ArgumentNullException(nameof(baseWriter));

         this.baseWriter = baseWriter;
      }

      public override XmlWriterSettings Settings {
         get { return this.baseWriter.Settings; }
      }

      public override WriteState WriteState {
         get { return this.baseWriter.WriteState; }
      }

      public override string XmlLang {
         get { return this.baseWriter.XmlLang; }
      }

      public override XmlSpace XmlSpace {
         get { return this.baseWriter.XmlSpace; }
      }

      public override void Close() {
         this.baseWriter.Close();
      }

      protected override void Dispose(bool disposing) {

         if (disposing) {
            this.baseWriter.Dispose();
         }
      }

      public override void Flush() {
         this.baseWriter.Flush();
      }

      public override Task FlushAsync() {
         return this.baseWriter.FlushAsync();
      }

      public override string LookupPrefix(string ns) {
         return this.baseWriter.LookupPrefix(ns);
      }

      public override void WriteAttributes(XmlReader reader, bool defattr) {
         this.baseWriter.WriteAttributes(reader, defattr);
      }

      public override Task WriteAttributesAsync(XmlReader reader, bool defattr) {
         return this.baseWriter.WriteAttributesAsync(reader, defattr);
      }

      public override void WriteBase64(byte[] buffer, int index, int count) {
         this.baseWriter.WriteBase64(buffer, index, count);
      }

      public override Task WriteBase64Async(byte[] buffer, int index, int count) {
         return this.baseWriter.WriteBase64Async(buffer, index, count);
      }

      public override void WriteBinHex(byte[] buffer, int index, int count) {
         this.baseWriter.WriteBinHex(buffer, index, count);
      }

      public override Task WriteBinHexAsync(byte[] buffer, int index, int count) {
         return this.baseWriter.WriteBinHexAsync(buffer, index, count);
      }

      public override void WriteCData(string text) {
         this.baseWriter.WriteCData(text);
      }

      public override Task WriteCDataAsync(string text) {
         return this.baseWriter.WriteCDataAsync(text);
      }

      public override void WriteCharEntity(char ch) {
         this.baseWriter.WriteCharEntity(ch);
      }

      public override Task WriteCharEntityAsync(char ch) {
         return this.baseWriter.WriteCharEntityAsync(ch);
      }

      public override void WriteChars(char[] buffer, int index, int count) {
         this.baseWriter.WriteChars(buffer, index, count);
      }

      public override Task WriteCharsAsync(char[] buffer, int index, int count) {
         return this.baseWriter.WriteCharsAsync(buffer, index, count);
      }

      public override void WriteComment(string text) {
         this.baseWriter.WriteComment(text);
      }

      public override Task WriteCommentAsync(string text) {
         return this.baseWriter.WriteCommentAsync(text);
      }

      public override void WriteDocType(string name, string pubid, string sysid, string subset) {
         this.baseWriter.WriteDocType(name, pubid, sysid, subset);
      }

      public override Task WriteDocTypeAsync(string name, string pubid, string sysid, string subset) {
         return this.baseWriter.WriteDocTypeAsync(name, pubid, sysid, subset);
      }

      public override void WriteEndAttribute() {
         this.baseWriter.WriteEndAttribute();
      }

      public override void WriteEndDocument() {
         this.baseWriter.WriteEndDocument();
      }

      public override Task WriteEndDocumentAsync() {
         return this.baseWriter.WriteEndDocumentAsync();
      }

      public override void WriteEndElement() {
         this.baseWriter.WriteEndElement();
      }

      public override Task WriteEndElementAsync() {
         return this.baseWriter.WriteEndElementAsync();
      }

      public override void WriteEntityRef(string name) {
         this.baseWriter.WriteEntityRef(name);
      }

      public override Task WriteEntityRefAsync(string name) {
         return this.baseWriter.WriteEntityRefAsync(name);
      }

      public override void WriteFullEndElement() {
         this.baseWriter.WriteFullEndElement();
      }

      public override Task WriteFullEndElementAsync() {
         return this.baseWriter.WriteFullEndElementAsync();
      }

      public override void WriteName(string name) {
         this.baseWriter.WriteName(name);
      }

      public override Task WriteNameAsync(string name) {
         return this.baseWriter.WriteNameAsync(name);
      }

      public override void WriteNmToken(string name) {
         this.baseWriter.WriteNmToken(name);
      }

      public override Task WriteNmTokenAsync(string name) {
         return this.baseWriter.WriteNmTokenAsync(name);
      }

      public override void WriteNode(XPathNavigator navigator, bool defattr) {
         this.baseWriter.WriteNode(navigator, defattr);
      }

      public override Task WriteNodeAsync(XPathNavigator navigator, bool defattr) {
         return this.baseWriter.WriteNodeAsync(navigator, defattr);
      }

      public override void WriteNode(XmlReader reader, bool defattr) {
         this.baseWriter.WriteNode(reader, defattr);
      }

      public override Task WriteNodeAsync(XmlReader reader, bool defattr) {
         return this.baseWriter.WriteNodeAsync(reader, defattr);
      }

      public override void WriteProcessingInstruction(string name, string text) {
         this.baseWriter.WriteProcessingInstruction(name, text);
      }

      public override Task WriteProcessingInstructionAsync(string name, string text) {
         return this.baseWriter.WriteProcessingInstructionAsync(name, text);
      }

      public override void WriteQualifiedName(string localName, string ns) {
         this.baseWriter.WriteQualifiedName(localName, ns);
      }

      public override Task WriteQualifiedNameAsync(string localName, string ns) {
         return this.baseWriter.WriteQualifiedNameAsync(localName, ns);
      }

      public override void WriteRaw(string data) {
         this.baseWriter.WriteRaw(data);
      }

      public override Task WriteRawAsync(string data) {
         return this.baseWriter.WriteRawAsync(data);
      }

      public override void WriteRaw(char[] buffer, int index, int count) {
         this.baseWriter.WriteRaw(buffer, index, count);
      }

      public override Task WriteRawAsync(char[] buffer, int index, int count) {
         return this.baseWriter.WriteRawAsync(buffer, index, count);
      }

      public override void WriteStartAttribute(string prefix, string localName, string ns) {
         this.baseWriter.WriteStartAttribute(prefix, localName, ns);
      }

      public override Task WriteStartDocumentAsync() {
         return this.baseWriter.WriteStartDocumentAsync();
      }

      public override void WriteStartDocument() {
         this.baseWriter.WriteStartDocument();
      }

      public override void WriteStartDocument(bool standalone) {
         this.baseWriter.WriteStartDocument(standalone);
      }

      public override Task WriteStartDocumentAsync(bool standalone) {
         return this.baseWriter.WriteStartDocumentAsync(standalone);
      }

      public override void WriteStartElement(string prefix, string localName, string ns) {
         this.baseWriter.WriteStartElement(prefix, localName, ns);
      }

      public override Task WriteStartElementAsync(string prefix, string localName, string ns) {
         return this.baseWriter.WriteStartElementAsync(prefix, localName, ns);
      }

      public override void WriteString(string text) {
         this.baseWriter.WriteString(text);
      }

      public override Task WriteStringAsync(string text) {
         return this.baseWriter.WriteStringAsync(text);
      }

      public override void WriteSurrogateCharEntity(char lowChar, char highChar) {
         this.baseWriter.WriteSurrogateCharEntity(lowChar, highChar);
      }

      public override Task WriteSurrogateCharEntityAsync(char lowChar, char highChar) {
         return this.baseWriter.WriteSurrogateCharEntityAsync(lowChar, highChar);
      }

      public override void WriteValue(bool value) {
         this.baseWriter.WriteValue(value);
      }

      public override void WriteValue(DateTime value) {
         this.baseWriter.WriteValue(value);
      }

      public override void WriteValue(DateTimeOffset value) {
         this.baseWriter.WriteValue(value);
      }

      public override void WriteValue(decimal value) {
         this.baseWriter.WriteValue(value);
      }

      public override void WriteValue(double value) {
         this.baseWriter.WriteValue(value);
      }

      public override void WriteValue(int value) {
         this.baseWriter.WriteValue(value);
      }

      public override void WriteValue(long value) {
         this.baseWriter.WriteValue(value);
      }

      public override void WriteValue(object value) {
         this.baseWriter.WriteValue(value);
      }

      public override void WriteValue(float value) {
         this.baseWriter.WriteValue(value);
      }

      public override void WriteValue(string value) {
         this.baseWriter.WriteValue(value);
      }

      public override void WriteWhitespace(string ws) {
         this.baseWriter.WriteWhitespace(ws);
      }

      public override Task WriteWhitespaceAsync(string ws) {
         return this.baseWriter.WriteWhitespaceAsync(ws);
      }
   }
}