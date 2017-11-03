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
using System.Xml.Linq;
using Xcst.PackageModel;

namespace Xcst.Runtime {

   public static class DocumentWriter {

      public static XcstWriter CreateDocument(IXcstPackage package, ISequenceWriter<object> output) {
         return CreateDocument(package, (ISequenceWriter<XDocument>)output);
      }

      public static XcstWriter CreateDocument(IXcstPackage package, ISequenceWriter<XDocument> output) {

         var doc = new XDocument();
         output.WriteObject(doc);

         var defaultParams = new OutputParameters {
            OmitXmlDeclaration = true
         };

         return WriterFactory.CreateWriter(doc.CreateWriter(), null)
            (defaultParams, null, package.Context);
      }

      public static XcstWriter CreateDocument(IXcstPackage package, ISequenceWriter<XmlDocument> output) {

         var doc = new XmlDocument();
         output.WriteObject(doc);

         var defaultParams = new OutputParameters {
            OmitXmlDeclaration = true
         };

         return WriterFactory.CreateWriter(doc.CreateNavigator().AppendChild(), null)
            (defaultParams, null, package.Context);
      }

      // Sadly, cannot create writer for c:element
      // XNodeBuilder does not support top level attribute and text

      public static XcstWriter CastElement(ISequenceWriter<object> output) {
         return Cast(output);
      }

      public static XcstWriter CastElement(ISequenceWriter<XElement> output) {
         return Cast(output);
      }

      public static XcstWriter CastElement(ISequenceWriter<XmlElement> output) {
         return Cast(output);
      }

      public static XcstWriter CastAttribute(ISequenceWriter<object> output) {
         return Cast(output);
      }

      public static XcstWriter CastAttribute(ISequenceWriter<XAttribute> output) {
         return Cast(output);
      }

      public static XcstWriter CastAttribute(ISequenceWriter<XmlAttribute> output) {
         return Cast(output);
      }

      public static XcstWriter CastProcessingInstruction(ISequenceWriter<object> output) {
         return Cast(output);
      }

      public static XcstWriter CastProcessingInstruction(ISequenceWriter<XProcessingInstruction> output) {
         return Cast(output);
      }

      public static XcstWriter CastProcessingInstruction(ISequenceWriter<XmlProcessingInstruction> output) {
         return Cast(output);
      }

      static XcstWriter Cast<TItem>(ISequenceWriter<TItem> output) {

         XcstWriter docWriter = output.TryCastToDocumentWriter();

         if (docWriter != null) {
            return docWriter;
         }

         throw new RuntimeException("Could not cast output to XcstWriter.");
      }
   }
}
