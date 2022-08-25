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

namespace Xcst.Compiler.Reflection;

class TypeManifestReader {

   const string
   _ns = XmlNamespaces.XcstGrammar;

   const string
   _prefix = "xcst";

   readonly XmlWriter
   _writer;

   public
   TypeManifestReader(XmlWriter writer) {
      _writer = writer;
   }

   internal void
   WriteTypeReference(Type type) {

      const string ns = XmlNamespaces.XcstCode;
      const string prefix = "code";

      _writer.WriteStartElement(prefix, "type-reference", ns);

      if (type.IsArray) {

         _writer.WriteAttributeString("array-dimensions", XmlConvert.ToString(type.GetArrayRank()));

         WriteTypeReference(type.GetElementType()!);

      } else {

         var typeArguments = type.GetGenericArguments();

         var name = (typeArguments.Length > 0) ?
            type.Name.Substring(0, type.Name.IndexOf('`'))
            : type.Name;

         _writer.WriteAttributeString("name", name);

         if (type.IsInterface) {
            _writer.WriteAttributeString("interface", "true");
         }

         if (type.IsNested) {
            WriteTypeReference(type.DeclaringType!);
         } else {
            _writer.WriteAttributeString("namespace", type.Namespace);
         }

         if (typeArguments.Length > 0) {

            _writer.WriteStartElement(prefix, "type-arguments", ns);

            for (int i = 0; i < typeArguments.Length; i++) {
               WriteTypeReference(typeArguments[i]);
            }

            _writer.WriteEndElement();
         }
      }

      _writer.WriteEndElement();
   }
}
