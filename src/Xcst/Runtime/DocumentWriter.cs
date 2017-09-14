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

         var defaultParams = new OutputParameters();

         return WriterFactory.CreateWriter(doc.CreateWriter(), null)
            (defaultParams, null, package.Context);
      }

      public static XcstWriter CreateDocument(IXcstPackage package, ISequenceWriter<XmlDocument> output) {

         var doc = new XmlDocument();
         output.WriteObject(doc);

         var defaultParams = new OutputParameters();

         return WriterFactory.CreateWriter(doc.CreateNavigator().AppendChild(), null)
            (defaultParams, null, package.Context);
      }
   }
}
