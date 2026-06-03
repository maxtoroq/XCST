// Copyright 2021 Max Toro Q.
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
using System.Collections.Generic;
using System.Xml.Linq;
using Newtonsoft.Json.Linq;

namespace Xcst.Runtime;

public static class DeepCopy {

   public static void
   Copy<TBase>(
         IXcstPackage package,
         TemplateContext context,
         ISequenceWriter<TBase> output) {

      switch (context.Input) {
         case var n and null:
            output.WriteObject((TBase)n!);
            break;

         case TBase item:
            output.CopyOf(item);
            break;

         case IEnumerable<TBase> seq:
            output.CopyOf(seq);
            break;

         default:
            throw new NotImplementedException();
      }
   }

   internal static TItem
   CopyDynamically<TItem>(TItem value) {

      return value switch {
         var v and null => v,
         XNode node => castItem(node switch {
            XElement v => new XElement(v),
            XDocument v => new XDocument(v),
            XCData v => new XCData(v), // XCData is also XText
            XText v => new XText(v),
            XComment v => new XComment(v),
            XProcessingInstruction v => new XProcessingInstruction(v),
            XDocumentType v => new XDocumentType(v),
            _ => throw new NotImplementedException()
         }),
         XAttribute v => castItem(new XAttribute(v)),
         XDeclaration v => castItem(new XDeclaration(v)),
         JToken v => castItem(v.DeepClone()),
         var v and (ValueType or String or Uri) => v,
         _ => throw new NotImplementedException()
      };

      static TItem castItem(object item) => (TItem)item;
   }
}
