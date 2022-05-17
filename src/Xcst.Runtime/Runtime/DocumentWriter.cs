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
using Xcst.Xml;

namespace Xcst.Runtime;

public static class DocumentWriter {

   public static XcstWriter
   CreateDocument(IXcstPackage package, ISequenceWriter<object> output) =>
      CreateDocument(package, (ISequenceWriter<XDocument>)output);

   public static XcstWriter
   CreateDocument(IXcstPackage package, ISequenceWriter<XDocument> output) {

      var doc = new XDocument();
      output.WriteObject(doc);

      var defaultParams = new OutputParameters {
         OmitXmlDeclaration = true
      };

      return WriterFactory.CreateWriter(doc.CreateWriter(), WriterFactory.AbsentOutputUri)
         (defaultParams, null, package.Context);
   }

   public static XcstWriter
   CreateDocument(IXcstPackage package, ISequenceWriter<XmlDocument> output) {

      var doc = new XmlDocument();
      output.WriteObject(doc);

      var defaultParams = new OutputParameters {
         OmitXmlDeclaration = true
      };

      return WriterFactory.CreateWriter(doc.CreateNavigator()!.AppendChild(), WriterFactory.AbsentOutputUri)
         (defaultParams, null, package.Context);
   }

   internal static XcstWriter
   CastDocument(IXcstPackage package, ISequenceWriter<XDocument> output) =>
      output.TryCastToDocumentWriter()
          ?? CreateDocument(package, output);

   public static XcstWriter
   CastElement(IXcstPackage package, ISequenceWriter<object> output) =>
      CastElement(package, (ISequenceWriter<XElement>)output);

   public static XcstWriter
   CastElement(IXcstPackage package, ISequenceWriter<XElement> output) {

      if (output.TryCastToDocumentWriter() is XcstWriter docWriter) {
         return docWriter;
      }

      var doc = new XDocument();

      var defaultParams = new OutputParameters {
         OmitXmlDeclaration = true
      };

      return WriterFactory.CreateWriter(new XElementWriter(doc, output), WriterFactory.AbsentOutputUri)
         (defaultParams, null, package.Context);
   }

   public static XcstWriter
   CastElement(IXcstPackage package, ISequenceWriter<XmlElement> output) => Cast(output);

   public static XcstWriter
   CastNamespace(ISequenceWriter<object> output) => Cast(output);

   public static XcstWriter
   CastAttribute(ISequenceWriter<object> output) => Cast(output);

   public static XcstWriter
   CastAttribute(ISequenceWriter<XAttribute> output) => Cast(output);

   public static XcstWriter
   CastAttribute(ISequenceWriter<XmlAttribute> output) => Cast(output);

   public static XcstWriter
   CastProcessingInstruction(ISequenceWriter<object> output) => Cast(output);

   public static XcstWriter
   CastProcessingInstruction(ISequenceWriter<XProcessingInstruction> output) =>
      Cast(output);

   public static XcstWriter
   CastProcessingInstruction(ISequenceWriter<XmlProcessingInstruction> output) =>
      Cast(output);

   static XcstWriter
   Cast<TItem>(ISequenceWriter<TItem> output) {

      var docWriter = output.TryCastToDocumentWriter()
         ?? throw new RuntimeException("Could not cast output to XcstWriter.");

      return docWriter;
   }
}
