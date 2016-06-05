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

namespace Xcst.Runtime {

   /// <exclude/>
   public static class DataType {

      public static bool Boolean(string boolean) {

         if (boolean == null) throw new ArgumentNullException(nameof(boolean));

         switch (boolean.Trim()) {
            case "yes":
            case "true":
            case "True":
            case "1":
               return true;

            case "no":
            case "false":
            case "False":
            case "0":
               return false;

            default:
               throw new RuntimeException("Invalid boolean value.", DynamicError.Code("XTDE0030"));
         }
      }

      public static bool SortOrderDescending(string order) {

         if (order == null) throw new ArgumentNullException(nameof(order));

         switch (order.Trim()) {
            case "ascending":
               return false;

            case "descending":
               return true;

            default:
               throw new RuntimeException("Invalid order value.", DynamicError.Code("XTDE0030"));
         }
      }

      public static QualifiedName QName(string localOrUriQualifiedName) {
         return QualifiedName.Parse(localOrUriQualifiedName);
      }

      public static QualifiedName QName(string ns, string localName) {
         return new QualifiedName(localName, ns);
      }

      public static Uri Uri(string uri) {
         return new Uri(uri, UriKind.RelativeOrAbsolute);
      }
   }
}