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

#region generate_id is based on code from .NET Framework
//------------------------------------------------------------------------------
// <copyright file="XmlQueryRuntime.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// <owner current="true" primary="true">Microsoft</owner>
//------------------------------------------------------------------------------
#endregion

using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Xml.Linq;

namespace Xcst.Compiler {

   partial class XcstCompilerPackage {

      static readonly char[]
      _uniqueIdTbl = new char[32] {
         'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I', 'J', 'K', 'L', 'M', 'N', 'O', 'P', 'Q',
         'R', 'S', 'T', 'U', 'V', 'W', 'X', 'Y', 'Z', '1', '2', '3', '4', '5', '6'
      };

      readonly List<XDocument>
      _documents = new();

      string
      generate_id(XObject node) {

         // logic from System.Xml.Xsl.Runtime.XmlQueryRuntime.GenerateId()

         return "ID"
            + doc_index(node).ToString(CultureInfo.InvariantCulture)
            + unique_id(node);

         int
         doc_index(XObject node) {

            var doc = node.Parent?.Document;

            if (doc is null) {
               return 0;
            }

            var i = _documents.IndexOf(doc);

            if (i != -1) {
               return i + 1;
            }

            _documents.Add(doc);

            return _documents.Count;
         }

         static string
         unique_id(XObject node) {

            // logic from XPathNavigator.UniqueId

            var sb = new StringBuilder();
            sb.Append(node_type_letter(node));

            XObject? current = node;

            while (true) {

               var num = index_in_parent(current);

               if ((current = current?.Parent) is null) {
                  break;
               }

               if (num <= 31) {
                  sb.Append(_uniqueIdTbl[num]);
                  continue;
               }

               sb.Append('0');

               do {
                  sb.Append(_uniqueIdTbl[num & 0x1F]);
                  num >>= 5;
               } while (num != 0);

               sb.Append('0');
            }

            return sb.ToString();
         }

         static char
         node_type_letter(XObject node) =>
            node switch {
               XAttribute => 'A',
               XElement => 'E',
               XText => 'T',
               _ => 'X'
            };

         static uint
         index_in_parent(XObject node) {

            // logic from XPathNavigator.IndexInParent

            var num = 0u;

            if (node is XAttribute attr) {

               XAttribute? nextAttr = attr;

               while ((nextAttr = nextAttr.NextAttribute) != null) {
                  num++;
               }

            } else {

               var n = (XNode)node;
               XNode? nextNode = n;

               while ((nextNode = nextNode.NextNode) != null) {
                  num++;
               }
            }

            return num;
         }
      }
   }
}
