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

      readonly XmlWriter
      _output;

      readonly bool
      _outputXmlDecl;

      readonly XmlStandalone
      _standalone;

      bool
      _xmlDeclWritten;

      public
      XmlXcstWriter(XmlWriter writer, Uri outputUri, OutputParameters parameters)
         : base(outputUri) {

         _output = writer;
         _outputXmlDecl = !parameters.OmitXmlDeclaration.GetValueOrDefault()
            && (parameters.Method is null
               || parameters.Method == OutputParameters.Methods.Xml
               || parameters.Method == OutputParameters.Methods.XHtml);

         _standalone = parameters.Standalone.GetValueOrDefault();
      }

      void
      WriteXmlDeclaration() {

         if (_outputXmlDecl
            && !_xmlDeclWritten
            && _output.WriteState == WriteState.Start) {

            if (_standalone == XmlStandalone.Omit) {
               _output.WriteStartDocument();
            } else {
               _output.WriteStartDocument(_standalone == XmlStandalone.Yes);
            }

            _xmlDeclWritten = true;
         }
      }

      public override void
      WriteComment(string? text) {

         WriteXmlDeclaration();
         _output.WriteComment(text);
         OnItemWritten();
      }

      public override void
      WriteEndAttribute() {

         // WriteEndAttribute is called in a finally block
         // Checking for error to not overwhelm writer when something goes wrong

         if (_output.WriteState != WriteState.Error) {
            _output.WriteEndAttribute();
         }
      }

      public override void
      WriteEndElement() {

         // WriteEndElement is called in a finally block
         // Checking for error to not overwhelm writer when something goes wrong

         if (_output.WriteState != WriteState.Error) {
            _output.WriteEndElement();
         }
      }

      public override void
      WriteProcessingInstruction(string name, string? text) {

         WriteXmlDeclaration();
         _output.WriteProcessingInstruction(name, text);
         OnItemWritten();
      }

      public override void
      WriteRaw(string? data) {

         WriteXmlDeclaration();
         _output.WriteRaw(data);

         if (!String.IsNullOrEmpty(data)) {
            OnItemWritten();
         }
      }

      public override void
      WriteStartAttribute(string? prefix, string localName, string? ns, string? separator) {
         _output.WriteStartAttribute(prefix, localName, ns);
      }

      public override void
      WriteStartElement(string? prefix, string localName, string? ns) {

         WriteXmlDeclaration();
         _output.WriteStartElement(prefix, localName, ns);
         OnItemWritten();
      }

      public override void
      WriteString(string? text) {

         WriteXmlDeclaration();
         _output.WriteString(text);

         if (!String.IsNullOrEmpty(text)) {
            OnItemWritten();
         }
      }

      public override void
      WriteChars(char[] buffer, int index, int count) {
         WriteXmlDeclaration();
         _output.WriteChars(buffer, index, count);
      }

      public override void
      Flush() => _output.Flush();

      protected override void
      Dispose(bool disposing) {

         if (disposing) {
            _output.Dispose();
         }

         base.Dispose(disposing);
      }
   }
}
