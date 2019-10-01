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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using Saxon.Api;

namespace Xcst.Compiler {

   static class SaxonExtensions {

      public static IEnumerable<XdmItem>
      AsItems(this IXdmEnumerator enumerator) {

         if (enumerator == null) {
            yield break;
         }

         while (enumerator.MoveNext()) {
            yield return (XdmItem)enumerator.Current;
         }
      }

      public static IEnumerable<XdmNode>
      AsNodes(this IXdmEnumerator enumerator) {

         if (enumerator == null) {
            yield break;
         }

         while (enumerator.MoveNext()) {
            yield return (XdmNode)enumerator.Current;
         }
      }

      public static IEnumerable<XdmAtomicValue>
      AsAtomicValues(this IXdmEnumerator enumerator) {

         if (enumerator == null) {
            yield break;
         }

         while (enumerator.MoveNext()) {
            yield return (XdmAtomicValue)enumerator.Current;
         }
      }

      public static IXdmEnumerator
      GetXdmEnumerator(this XdmValue value) {

         if (value == null) {
            return EmptyEnumerator.INSTANCE;
         }

         return (IXdmEnumerator)value.GetEnumerator();
      }

      // ## ToXdmValue

      public static XdmValue
      ToXdmValue(this string value) =>
         (value != null) ? (XdmValue)ToXdmItem(value) : XdmEmptySequence.INSTANCE;

      public static XdmValue
      ToXdmValue(this Boolean value) =>
         ToXdmAtomicValue(value);

      public static XdmValue
      ToXdmValue(this Int16 value) =>
         ToXdmAtomicValue(value);

      public static XdmValue
      ToXdmValue(this Int32 value) =>
         ToXdmAtomicValue(value);

      public static XdmValue
      ToXdmValue(this Int64 value) =>
         ToXdmAtomicValue(value);

      public static XdmValue
      ToXdmValue(this Byte value) =>
         ToXdmAtomicValue(value);

      public static XdmValue
      ToXdmValue(this SByte value) =>
         ToXdmAtomicValue(value);

      public static XdmValue
      ToXdmValue(this UInt16 value) =>
         ToXdmAtomicValue(value);

      public static XdmValue
      ToXdmValue(this UInt32 value) =>
         ToXdmAtomicValue(value);

      public static XdmValue
      ToXdmValue(this UInt64 value) =>
         ToXdmAtomicValue(value);

      public static XdmValue
      ToXdmValue(this Single value) =>
         ToXdmAtomicValue(value);

      public static XdmValue
      ToXdmValue(this Double value) =>
         ToXdmAtomicValue(value);

      public static XdmValue
      ToXdmValue(this Decimal value) =>
         ToXdmAtomicValue(value);

      public static XdmValue
      ToXdmValue(this Uri value) =>
         (value != null) ? (XdmValue)ToXdmItem(value) : XdmEmptySequence.INSTANCE;

      public static XdmValue
      ToXdmValue(this XmlQualifiedName value) =>
         (value != null) ? (XdmValue)ToXdmItem(value) : XdmEmptySequence.INSTANCE;

      public static XdmValue
      ToXdmValue(this QualifiedName value) =>
         (value != null) ? (XdmValue)ToXdmItem(value) : XdmEmptySequence.INSTANCE;

      public static XdmValue
      ToXdmValue(this QName value) =>
         (value != null) ? (XdmValue)ToXdmItem(value) : XdmEmptySequence.INSTANCE;

      public static XdmValue
      ToXdmValue(this IEnumerable<string> value) {

         if (value == null) {
            return XdmEmptySequence.INSTANCE;
         }

         return new XdmValue(value.Select(s => ToXdmValue(s)));
      }

      public static XdmValue
      ToXdmValue(this IEnumerable<XdmItem> value) =>
         new XdmValue(value);

      public static XdmValue
      ToXdmValue(this IEnumerable value) {

         if (value == null) {
            return XdmEmptySequence.INSTANCE;
         }

         XdmValue result = XdmEmptySequence.INSTANCE;

         foreach (object item in value) {
            result = result.Append(ToXdmValue(item));
         }

         return result;
      }

      public static XdmValue
      ToXdmValue(this object value) {

         if (value == null) {
            return XdmEmptySequence.INSTANCE;
         }

         // Must check for string before checking for IEnumerable

         if (value is string str) {
            return ToXdmAtomicValue(str);
         }

         if (value is XdmValue xdmVal) {
            return xdmVal;
         }

         Type type = value.GetType();

         if (type.IsArray
            || typeof(IEnumerable).IsAssignableFrom(type)) {

            return ToXdmValue((IEnumerable)value);
         }

         return ToXdmItem(value);
      }

      // ## ToXdmItem

      public static XdmItem
      ToXdmItem(this string value) =>
         ToXdmAtomicValue(value);

      public static XdmItem
      ToXdmItem(this Boolean value) =>
         ToXdmAtomicValue(value);

      public static XdmItem
      ToXdmItem(this Int16 value) =>
         ToXdmAtomicValue(value);

      public static XdmItem
      ToXdmItem(this Int32 value) =>
         ToXdmAtomicValue(value);

      public static XdmItem
      ToXdmItem(this Int64 value) =>
         ToXdmAtomicValue(value);

      public static XdmItem
      ToXdmItem(this Byte value) =>
         ToXdmAtomicValue(value);

      public static XdmItem
      ToXdmItem(this SByte value) =>
         ToXdmAtomicValue(value);

      public static XdmItem
      ToXdmItem(this UInt16 value) =>
         ToXdmAtomicValue(value);

      public static XdmItem
      ToXdmItem(this UInt32 value) =>
         ToXdmAtomicValue(value);

      public static XdmItem
      ToXdmItem(this UInt64 value) =>
         ToXdmAtomicValue(value);

      public static XdmItem
      ToXdmItem(this Single value) =>
         ToXdmAtomicValue(value);

      public static XdmItem
      ToXdmItem(this Double value) =>
         ToXdmAtomicValue(value);

      public static XdmItem
      ToXdmItem(this Decimal value) =>
         ToXdmAtomicValue(value);

      public static XdmItem
      ToXdmItem(this Uri value) =>
         ToXdmAtomicValue(value);

      public static XdmItem
      ToXdmItem(this XmlQualifiedName value) =>
         ToXdmAtomicValue(value);

      public static XdmItem
      ToXdmItem(this QualifiedName value) =>
         ToXdmAtomicValue(value);

      public static XdmItem
      ToXdmItem(this QName value) =>
         ToXdmAtomicValue(value);

      public static XdmItem
      ToXdmItem(this object value) {

         if (value == null) throw new ArgumentNullException(nameof(value));

         return ToXdmAtomicValue(value);
      }

      // ## ToXdmAtomicValue

      public static XdmAtomicValue
      ToXdmAtomicValue(this string value) =>
         new XdmAtomicValue(value);

      public static XdmAtomicValue
      ToXdmAtomicValue(this Boolean value) =>
         new XdmAtomicValue(value);

      public static XdmAtomicValue
      ToXdmAtomicValue(this Int16 value) =>
         ToXdmAtomicValue((Int64)value);

      public static XdmAtomicValue
      ToXdmAtomicValue(this Int32 value) =>
         ToXdmAtomicValue((Int64)value);

      public static XdmAtomicValue
      ToXdmAtomicValue(this Int64 value) =>
         new XdmAtomicValue(value);

      public static XdmAtomicValue
      ToXdmAtomicValue(this Byte value) =>
         ToXdmAtomicValue((Int64)value);

      public static XdmAtomicValue
      ToXdmAtomicValue(this SByte value) =>
         ToXdmAtomicValue((Int64)value);

      public static XdmAtomicValue
      ToXdmAtomicValue(this UInt16 value) =>
         ToXdmAtomicValue((Int64)value);

      public static XdmAtomicValue
      ToXdmAtomicValue(this UInt32 value) =>
         ToXdmAtomicValue((Int64)value);

      public static XdmAtomicValue
      ToXdmAtomicValue(this UInt64 value) =>
         ToXdmAtomicValue((Int64)value);

      public static XdmAtomicValue
      ToXdmAtomicValue(this Single value) =>
         new XdmAtomicValue(value);

      public static XdmAtomicValue
      ToXdmAtomicValue(this Double value) =>
         new XdmAtomicValue(value);

      public static XdmAtomicValue
      ToXdmAtomicValue(this Decimal value) =>
         new XdmAtomicValue(value);

      public static XdmAtomicValue
      ToXdmAtomicValue(this Uri value) =>
         new XdmAtomicValue(value);

      public static XdmAtomicValue
      ToXdmAtomicValue(this XmlQualifiedName value) =>
         ToXdmAtomicValue(new QName(value));

      public static XdmAtomicValue
      ToXdmAtomicValue(this QualifiedName value) {

         if (value == null) throw new ArgumentNullException(nameof(value));

         return ToXdmAtomicValue(ToQName(value));
      }

      public static XdmAtomicValue
      ToXdmAtomicValue(this QName value) =>
         new XdmAtomicValue(value);

      public static XdmAtomicValue
      ToXdmAtomicValue(this object value) {

         if (value == null) throw new ArgumentNullException(nameof(value));

         Type type = value.GetType();

         return ToXdmAtomicValue(value, type, Type.GetTypeCode(type));
      }

      static XdmAtomicValue
      ToXdmAtomicValue(this object value, Type type, TypeCode typeCode) {

         switch (typeCode) {
            case TypeCode.Boolean:
               return ToXdmAtomicValue((Boolean)value);

            case TypeCode.Int16:
               return ToXdmAtomicValue((Int16)value);

            case TypeCode.Int32:
               return ToXdmAtomicValue((Int32)value);

            case TypeCode.Int64:
               return ToXdmAtomicValue((Int64)value);

            case TypeCode.Byte:
               return ToXdmAtomicValue((Byte)value);

            case TypeCode.SByte:
               return ToXdmAtomicValue((SByte)value);

            case TypeCode.UInt16:
               return ToXdmAtomicValue((UInt16)value);

            case TypeCode.UInt32:
               return ToXdmAtomicValue((UInt32)value);

            case TypeCode.UInt64:
               return ToXdmAtomicValue((UInt64)value);

            case TypeCode.Char:
            case TypeCode.String:
               return ToXdmAtomicValue(value.ToString());

            case TypeCode.Decimal:
               return ToXdmAtomicValue((Decimal)value);

            case TypeCode.Double:
               return ToXdmAtomicValue((Double)value);

            case TypeCode.DBNull:
            case TypeCode.Empty:
               throw new ArgumentException($"{nameof(value)} cannot be null or empty.", nameof(value));

            case TypeCode.Single:
               return ToXdmAtomicValue((Single)value);

            default:
               break;
         }

         if (typeof(Uri).IsAssignableFrom(type)) {
            return ToXdmAtomicValue((Uri)value);
         }

         if (typeof(XmlQualifiedName).IsAssignableFrom(type)) {
            return ToXdmAtomicValue((XmlQualifiedName)value);
         }

         if (typeof(QualifiedName).IsAssignableFrom(type)) {
            return ToXdmAtomicValue((QualifiedName)value);
         }

         if (typeof(QName).IsAssignableFrom(type)) {
            return ToXdmAtomicValue((QName)value);
         }

         if (typeof(XdmAtomicValue).IsAssignableFrom(type)) {
            return (XdmAtomicValue)value;
         }

         throw new ArgumentException($"{nameof(value)} of type {type.FullName} is not supported.", nameof(value));
      }

      // Other

      public static XdmValue
      FirstElementOrSelf(this XdmValue value) {

         if (value == null) throw new ArgumentNullException(nameof(value));

         if (value is XdmNode node) {
            return FirstElementOrSelf(node);
         }

         return value;
      }

      public static XdmNode
      FirstElementOrSelf(this XdmNode value) {

         if (value == null) throw new ArgumentNullException(nameof(value));

         if (value.NodeKind == XmlNodeType.Element) {
            return value;
         }

         return ((IXdmEnumerator)value.EnumerateAxis(XdmAxis.Child))
            .AsNodes()
            .SingleOrDefault(n => n.NodeKind == XmlNodeType.Element)
            ?? value;
      }

      public static XdmValue
      GetErrorObject(this DynamicError error) =>
         XdmValue.Wrap(error.UnderlyingException.getErrorObject());

      public static int
      GetLineNumber(this XdmNode value) =>
         value.Implementation.getLineNumber();

      public static QualifiedName
      ToQualifiedName(this QName value) =>
         new QualifiedName(value.LocalName, value.Uri);

      public static QName
      ToQName(this QualifiedName value) =>
         new QName(value.Namespace, value.Name);
   }
}
