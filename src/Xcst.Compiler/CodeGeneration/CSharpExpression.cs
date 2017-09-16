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
using System.Globalization;
using System.Text;

namespace Xcst.Compiler.CodeGeneration {

   static class CSharpExpression {

      public static string Constant(object value) {

         if (value == null) {
            return "null";
         }

         string str = Convert.ToString(value, CultureInfo.InvariantCulture);

         if (value is string) {
            return $"@\"{str.Replace("\"", "\"\"")}\"";
         }

         if (value is bool) {
            return str.ToLowerInvariant();
         }

         if (value is decimal) {
            return str + "m";
         }

         if (value is long) {
            return str + "L";
         }

         if (value is double) {
            return str + "d";
         }

         if (value is float) {
            return str + "f";
         }

         if (value is uint) {
            return str + "u";
         }

         if (value is ulong) {
            return str + "ul";
         }

         return str;
      }

      public static string TypeReference(Type type, string namespaceAlias = null) {

         var sb = new StringBuilder();
         TypeReference(type, namespaceAlias, sb);

         return sb.ToString();
      }

      static void TypeReference(Type type, string namespaceAlias, StringBuilder sb) {

         if (type.IsNested) {

            TypeReference(type.DeclaringType, namespaceAlias, sb);
            sb.Append(".");

         } else {

            if (namespaceAlias != null) {
               sb.Append(namespaceAlias);
               sb.Append("::");
            }

            sb.Append(type.Namespace);
            sb.Append(".");
         }

         Type[] typeArguments = type.GetGenericArguments();

         if (typeArguments.Length > 0) {

            sb.Append(type.Name.Substring(0, type.Name.IndexOf('`')));
            sb.Append("<");

            for (int i = 0; i < typeArguments.Length; i++) {

               if (i > 0) {
                  sb.Append(", ");
               }

               TypeReference(typeArguments[i], namespaceAlias, sb);
            }

            sb.Append(">");

         } else {
            sb.Append(type.Name);
         }
      }
   }
}
