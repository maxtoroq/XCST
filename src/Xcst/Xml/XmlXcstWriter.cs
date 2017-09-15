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
      readonly bool outputXmlDecl;
      readonly XmlStandalone standalone;
      bool xmlDeclWritten;

      public XmlXcstWriter(XmlWriter writer, Uri outputUri, OutputParameters parameters)
         : base(outputUri) {

         this.writer = writer;
         this.outputXmlDecl = !parameters.OmitXmlDeclaration.GetValueOrDefault()
            && (parameters.Method == null
               || parameters.Method == OutputParameters.StandardMethods.Xml
               || parameters.Method == OutputParameters.StandardMethods.XHtml);

         this.standalone = parameters.Standalone.GetValueOrDefault();
      }

      void WriteXmlDeclaration() {

         if (this.outputXmlDecl
            && !this.xmlDeclWritten
            && this.writer.WriteState == WriteState.Start) {

            if (this.standalone == XmlStandalone.Omit) {
               this.writer.WriteStartDocument();
            } else {
               this.writer.WriteStartDocument(this.standalone == XmlStandalone.Yes);
            }

            this.xmlDeclWritten = true;
         }
      }

      public override void WriteComment(string text) {
         WriteXmlDeclaration();
         this.writer.WriteComment(text);
      }

      public override void WriteEndAttribute() {

         // WriteEndAttribute is called in a finally block
         // Checking for error to not overwhelm writer when something goes wrong

         if (this.writer.WriteState != WriteState.Error) {
            this.writer.WriteEndAttribute();
         }
      }

      public override void WriteEndElement() {

         // WriteEndElement is called in a finally block
         // Checking for error to not overwhelm writer when something goes wrong

         if (this.writer.WriteState != WriteState.Error) {
            this.writer.WriteEndElement();
         }
      }

      public override void WriteProcessingInstruction(string name, string text) {
         WriteXmlDeclaration();
         this.writer.WriteProcessingInstruction(name, text);
      }

      public override void WriteRaw(string data) {
         WriteXmlDeclaration();
         this.writer.WriteRaw(data);
      }

      public override void WriteStartAttribute(string prefix, string localName, string ns, string separator) {
         this.writer.WriteStartAttribute(prefix, localName, ns);
      }

      public override void WriteStartElement(string prefix, string localName, string ns) {
         WriteXmlDeclaration();
         this.writer.WriteStartElement(prefix, localName, ns);
      }

      public override void WriteString(string text) {
         WriteXmlDeclaration();
         this.writer.WriteString(text);
      }

      public override void WriteChars(char[] buffer, int index, int count) {
         WriteXmlDeclaration();
         this.writer.WriteChars(buffer, index, count);
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
