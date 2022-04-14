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
using System.Xml.Linq;
using Newtonsoft.Json.Linq;

namespace Xcst.Runtime {

   class DeepCopy {

      static readonly dynamic
      _dynamicInstance = new DeepCopy();

      public static TItem
      CopyDynamically<TItem>(dynamic? value) {

         if (value is null) {
            return default!;
         }

         return (TItem)_dynamicInstance.Copy(value);
      }

      public Boolean
      Copy(Boolean value) => value;

      public Byte
      Copy(Byte value) => value;

      public DateTime
      Copy(DateTime value) => value;

      public DateTimeOffset
      Copy(DateTimeOffset value) => value;

      public Decimal
      Copy(Decimal value) => value;

      public Double
      Copy(Double value) => value;

      public Guid
      Copy(Guid value) => value;

      public Int16
      Copy(Int16 value) => value;

      public Int32
      Copy(Int32 value) => value;

      public Int64
      Copy(Int64 value) => value;

      public JToken
      Copy(JToken value) => value.DeepClone();

      public SByte
      Copy(SByte value) => value;

      public Single
      Copy(Single value) => value;

      public String
      Copy(String value) => value;

      public TimeSpan
      Copy(TimeSpan value) => value;

      public UInt16
      Copy(UInt16 value) => value;

      public UInt32
      Copy(UInt32 value) => value;

      public UInt64
      Copy(UInt64 value) => value;

      public Uri
      Copy(Uri value) => value;

      public XAttribute
      Copy(XAttribute value) => new XAttribute(value);

      public XDeclaration
      Copy(XDeclaration value) => new XDeclaration(value);

      public XNode
      Copy(XNode value) =>
         value switch {
            XElement v => new XElement(v),
            XDocument v => new XDocument(v),
            XCData v => new XCData(v), // XCData is also XText
            XText v => new XText(v),
            XComment v => new XComment(v),
            XProcessingInstruction v => new XProcessingInstruction(v),
            XDocumentType v => new XDocumentType(v),
            _ => throw new NotImplementedException()
         };
   }
}
