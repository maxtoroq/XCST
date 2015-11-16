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
using System.Linq;
using XsltFunctions = System.Xml.Xsl.Runtime.XsltFunctions;

namespace Xcst.Runtime {

   /// <exclude/>
   public static class SimpleContent {

      public static string Join(string separator, IEnumerable value) {

         if (value == null) {
            return String.Empty;
         }

         return String.Join(separator, value.Cast<object>().Where(v => v != null));
      }

      public static string Join(string separator, params object[] value) {

         if (value == null) {
            return String.Empty;
         }

         return String.Join(separator, value.Where(v => v != null));
      }

      public static string Join(string separator, params string[] value) {

         if (value == null) {
            return String.Empty;
         }

         return String.Join(separator, value.Where(v => v != null));
      }

      public static string Join(string separator, string value) {
         return value ?? String.Empty;
      }

      public static string Join(string separator, object value) {
         return Convert.ToString(value);
      }

      public static string NormalizeSpace(string value) {

         if (String.IsNullOrEmpty(value)) {
            return String.Empty;
         }

         return XsltFunctions.NormalizeSpace(value);
      }
   }
}