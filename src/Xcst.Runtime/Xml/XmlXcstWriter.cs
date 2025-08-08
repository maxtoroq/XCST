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

namespace Xcst.Xml;

class XmlXcstWriter : XcstWriter {

   readonly XmlWriter
   _output;

   bool
   _outputXmlDecl;

   readonly XmlStandalone
   _standalone;

   bool
   _outputHtml5Doctype;

   int
   _depth;

   protected internal override int
   Depth => _depth;

   public
   XmlXcstWriter(XmlWriter writer, Uri outputUri, OutputParameters parameters)
      : base(outputUri) {

      _output = writer;
      _outputXmlDecl = !parameters.OmitXmlDeclaration.GetValueOrDefault()
         && (parameters.Method is null
            || parameters.Method == OutputParameters.Methods.Xml);

      _standalone = parameters.Standalone.GetValueOrDefault();

      _outputHtml5Doctype = parameters.Method == OutputParameters.Methods.Html
         && parameters.DoctypePublic is null
         && parameters.DoctypeSystem is null
         && parameters.RequestedHtmlVersion() >= 5m;
   }

   void
   WriteXmlDeclaration() {

      if (_outputXmlDecl
         && _output.WriteState == WriteState.Start) {

         if (_standalone == XmlStandalone.Omit) {
            _output.WriteStartDocument();
         } else {
            _output.WriteStartDocument(_standalone == XmlStandalone.Yes);
         }

         _outputXmlDecl = false;
      }
   }

   void
   WriteHtml5Doctype(string? prefix, string localName) {

      if (_outputHtml5Doctype) {

         if (_output.WriteState is WriteState.Start or WriteState.Prolog) {

            var name = !String.IsNullOrEmpty(prefix) ?
               (prefix + ":" + localName)
               : localName;

            _output.WriteDocType(name, null, null, null);
         }

         _outputHtml5Doctype = false;
      }
   }

   public override void
   WriteStartElement(string? prefix, string localName, string? ns) {

      WriteXmlDeclaration();
      WriteHtml5Doctype(prefix, localName);

      OnItemWritting();
      _output.WriteStartElement(prefix, localName, ns);
      _depth++;
   }

   public override void
   WriteEndElement() {

      // WriteEndElement is called in a finally block
      // Checking for error to not overwhelm writer when something goes wrong

      if (_output.WriteState != WriteState.Error) {
         _output.WriteEndElement();
         _depth--;
         OnItemWritten();
      }
   }

   public override void
   WriteStartAttribute(string? prefix, string localName, string? ns, string? separator) {

      //OnItemWritting();
      _output.WriteStartAttribute(prefix, localName, ns);
      _depth++;
   }

   public override void
   WriteEndAttribute() {

      // WriteEndAttribute is called in a finally block
      // Checking for error to not overwhelm writer when something goes wrong

      if (_output.WriteState != WriteState.Error) {
         _output.WriteEndAttribute();
         _depth--;
         //OnItemWritten();
      }
   }

   public override void
   WriteComment(string? text) {

      WriteXmlDeclaration();
      OnItemWritting();
      _output.WriteComment(text);
      OnItemWritten();
   }

   public override void
   WriteProcessingInstruction(string name, string? text) {

      WriteXmlDeclaration();
      OnItemWritting();
      _output.WriteProcessingInstruction(name, text);
      OnItemWritten();
   }

   public override void
   WriteString(string? text) {

      if (!String.IsNullOrEmpty(text)) {

         WriteXmlDeclaration();
         OnItemWritting();
         _output.WriteString(text);
         OnItemWritten();
      }
   }

   public override void
   WriteChars(char[] buffer, int index, int count) {

      if (buffer != null
         && buffer.Length > 0) {

         WriteXmlDeclaration();
         OnItemWritting();
         _output.WriteChars(buffer, index, count);
         OnItemWritten();
      }
   }

   public override void
   WriteRaw(string? data) {

      if (!String.IsNullOrEmpty(data)) {
         WriteXmlDeclaration();
         OnItemWritting();
         _output.WriteRaw(data);
         OnItemWritten();
      }
   }

   public override void
   EndTrack() {

      if (_output.WriteState != WriteState.Error) {
         base.EndTrack();
      }
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
