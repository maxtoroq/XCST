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
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using Saxon.Api;

namespace Xcst.Compiler {

   static class SaxonExtensions {

      public static IEnumerable<XdmItem> AsItems(this IXdmEnumerator enumerator) {

         if (enumerator == null) {
            yield break;
         }

         while (enumerator.MoveNext()) {
            yield return (XdmItem)enumerator.Current;
         }
      }

      public static IEnumerable<XdmNode> AsNodes(this IXdmEnumerator enumerator) {

         if (enumerator == null) {
            yield break;
         }

         while (enumerator.MoveNext()) {
            yield return (XdmNode)enumerator.Current;
         }
      }

      public static IEnumerable<XdmAtomicValue> AsAtomicValues(this IXdmEnumerator enumerator) {

         if (enumerator == null) {
            yield break;
         }

         while (enumerator.MoveNext()) {
            yield return (XdmAtomicValue)enumerator.Current;
         }
      }

      public static IXdmEnumerator GetXdmEnumerator(this XdmValue value) {

         if (value == null) {
            return EmptyEnumerator.INSTANCE;
         }

         return (IXdmEnumerator)value.GetEnumerator();
      }

      public static XdmValue ToXdmValue(this IEnumerable<XdmItem> value) {
         return new XdmValue(value);
      }

      public static XdmValue ToXdmValue(this string value) {
         return (value != null) ? (XdmValue)ToXdmItem(value) : XdmEmptySequence.INSTANCE;
      }

      public static XdmValue ToXdmValue(this Boolean value) {
         return ToXdmAtomicValue(value);
      }

      public static XdmValue ToXdmValue(this Int16 value) {
         return ToXdmAtomicValue(value);
      }

      public static XdmValue ToXdmValue(this Int32 value) {
         return ToXdmAtomicValue(value);
      }

      public static XdmValue ToXdmValue(this Int64 value) {
         return ToXdmAtomicValue(value);
      }

      public static XdmValue ToXdmValue(this Byte value) {
         return ToXdmAtomicValue(value);
      }

      public static XdmValue ToXdmValue(this SByte value) {
         return ToXdmAtomicValue(value);
      }

      public static XdmValue ToXdmValue(this UInt16 value) {
         return ToXdmAtomicValue(value);
      }

      public static XdmValue ToXdmValue(this UInt32 value) {
         return ToXdmAtomicValue(value);
      }

      public static XdmValue ToXdmValue(this UInt64 value) {
         return ToXdmAtomicValue(value);
      }

      public static XdmValue ToXdmValue(this Single value) {
         return ToXdmAtomicValue(value);
      }

      public static XdmValue ToXdmValue(this Double value) {
         return ToXdmAtomicValue(value);
      }

      public static XdmValue ToXdmValue(this Decimal value) {
         return ToXdmAtomicValue(value);
      }

      public static XdmValue ToXdmValue(this Uri value) {
         return (value != null) ? (XdmValue)ToXdmItem(value) : XdmEmptySequence.INSTANCE;
      }

      public static XdmValue ToXdmValue(this QName value) {
         return (value != null) ? (XdmValue)ToXdmItem(value) : XdmEmptySequence.INSTANCE;
      }

      public static XdmValue ToXdmValue(this XmlQualifiedName value) {
         return (value != null) ? (XdmValue)ToXdmItem(value) : XdmEmptySequence.INSTANCE;
      }

      public static XdmValue ToXdmValue(this IEnumerable<string> value) {

         if (value == null) {
            return XdmEmptySequence.INSTANCE;
         }

         return new XdmValue(value.Select(s => ToXdmValue(s)));
      }

      public static XdmItem ToXdmItem(this string value) {
         return ToXdmAtomicValue(value);
      }

      public static XdmItem ToXdmItem(this Boolean value) {
         return ToXdmAtomicValue(value);
      }

      public static XdmItem ToXdmItem(this Int16 value) {
         return ToXdmAtomicValue(value);
      }

      public static XdmItem ToXdmItem(this Int32 value) {
         return ToXdmAtomicValue(value);
      }

      public static XdmItem ToXdmItem(this Int64 value) {
         return ToXdmAtomicValue(value);
      }

      public static XdmItem ToXdmItem(this Byte value) {
         return ToXdmAtomicValue(value);
      }

      public static XdmItem ToXdmItem(this SByte value) {
         return ToXdmAtomicValue(value);
      }

      public static XdmItem ToXdmItem(this UInt16 value) {
         return ToXdmAtomicValue(value);
      }

      public static XdmItem ToXdmItem(this UInt32 value) {
         return ToXdmAtomicValue(value);
      }

      public static XdmItem ToXdmItem(this UInt64 value) {
         return ToXdmAtomicValue(value);
      }

      public static XdmItem ToXdmItem(this Single value) {
         return ToXdmAtomicValue(value);
      }

      public static XdmItem ToXdmItem(this Double value) {
         return ToXdmAtomicValue(value);
      }

      public static XdmItem ToXdmItem(this Decimal value) {
         return ToXdmAtomicValue(value);
      }

      public static XdmItem ToXdmItem(this Uri value) {
         return ToXdmAtomicValue(value);
      }

      public static XdmItem ToXdmItem(this QName value) {
         return ToXdmAtomicValue(value);
      }

      public static XdmItem ToXdmItem(this XmlQualifiedName value) {
         return ToXdmAtomicValue(value);
      }

      public static XdmAtomicValue ToXdmAtomicValue(this string value) {
         return new XdmAtomicValue(value);
      }

      public static XdmAtomicValue ToXdmAtomicValue(this Boolean value) {
         return new XdmAtomicValue(value);
      }

      public static XdmAtomicValue ToXdmAtomicValue(this Int16 value) {
         return ToXdmAtomicValue((Int64)value);
      }

      public static XdmAtomicValue ToXdmAtomicValue(this Int32 value) {
         return ToXdmAtomicValue((Int64)value);
      }

      public static XdmAtomicValue ToXdmAtomicValue(this Int64 value) {
         return new XdmAtomicValue(value);
      }

      public static XdmAtomicValue ToXdmAtomicValue(this Byte value) {
         return ToXdmAtomicValue((Int64)value);
      }

      public static XdmAtomicValue ToXdmAtomicValue(this SByte value) {
         return ToXdmAtomicValue((Int64)value);
      }

      public static XdmAtomicValue ToXdmAtomicValue(this UInt16 value) {
         return ToXdmAtomicValue((Int64)value);
      }

      public static XdmAtomicValue ToXdmAtomicValue(this UInt32 value) {
         return ToXdmAtomicValue((Int64)value);
      }

      public static XdmAtomicValue ToXdmAtomicValue(this UInt64 value) {
         return ToXdmAtomicValue((Int64)value);
      }

      public static XdmAtomicValue ToXdmAtomicValue(this Single value) {
         return new XdmAtomicValue(value);
      }

      public static XdmAtomicValue ToXdmAtomicValue(this Double value) {
         return new XdmAtomicValue(value);
      }

      public static XdmAtomicValue ToXdmAtomicValue(this Decimal value) {
         return new XdmAtomicValue(value);
      }

      public static XdmAtomicValue ToXdmAtomicValue(this Uri value) {
         return new XdmAtomicValue(value);
      }

      public static XdmAtomicValue ToXdmAtomicValue(this QName value) {
         return new XdmAtomicValue(value);
      }

      public static XdmAtomicValue ToXdmAtomicValue(this XmlQualifiedName value) {
         return ToXdmAtomicValue(new QName(value));
      }

      public static XdmValue FirstElementOrSelf(this XdmValue value) {

         if (value == null) throw new ArgumentNullException(nameof(value));

         var node = value as XdmNode;

         if (node == null) {
            return value;
         }

         return FirstElementOrSelf(node);
      }

      public static XdmNode FirstElementOrSelf(this XdmNode value) {

         if (value == null) throw new ArgumentNullException(nameof(value));

         if (value.NodeKind == XmlNodeType.Element) {
            return value;
         }

         return ((IXdmEnumerator)value.EnumerateAxis(XdmAxis.Child)).AsNodes().SingleOrDefault(n => n.NodeKind == XmlNodeType.Element)
            ?? value;
      }

      public static XdmValue GetErrorObject(this DynamicError error) {
         return XdmValue.Wrap(error.UnderlyingException.getErrorObject());
      }

      public static int GetLineNumber(this XdmNode value) {
         return value.Implementation.getLineNumber();
      }

      public static QualifiedName ToQualifiedName(this QName qname) {
         return new QualifiedName(qname.LocalName, qname.Uri);
      }
   }
}
