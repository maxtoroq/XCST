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
using System.Text;

#region NormalizeSpace is based on code from .NET Framework
//------------------------------------------------------------------------------
// <copyright file="XsltFunctions.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// <owner current="true" primary="true">[....]</owner>
//------------------------------------------------------------------------------
#endregion

namespace Xcst.Runtime {

   /// <exclude/>

   public class SimpleContent {

      readonly IFormatProvider formatProvider;

      public SimpleContent(IFormatProvider formatProvider) {

         if (formatProvider == null) throw new ArgumentNullException(nameof(formatProvider));

         this.formatProvider = formatProvider;
      }

      public string Join(string separator, IEnumerable value) {

         if (value == null) {
            return String.Empty;
         }

         return Join(separator, value.Cast<object>());
      }

      public string Join(string separator, params object[] value) {
         return Join(separator, value as IEnumerable<object>);
      }

      public string Join(string separator, IEnumerable<object> value) {

         if (value == null) {
            return String.Empty;
         }

         return Join(separator, value.Where(v => v != null).Select(v => Convert(v)));
      }

      public string Join(string separator, params string[] value) {
         return Join(separator, value as IEnumerable<string>);
      }

      public string Join(string separator, IEnumerable<string> value) {

         if (value == null) {
            return String.Empty;
         }

         return String.Join(separator, value.Where(v => v != null));
      }

      public string Join(string separator, string value) {
         return value ?? String.Empty;
      }

      public string Join(string separator, object value) {
         return Convert(value);
      }

      public string Format(string format, params object[] args) {
         return String.Format(this.formatProvider, format, args);
      }

      public string FormatValueTemplate(IFormattable value) {
         return value.ToString(null, this.formatProvider);
      }

      public string Convert(object value) {
         return System.Convert.ToString(value, this.formatProvider);
      }

      public static string Trim(string value) {

         if (String.IsNullOrEmpty(value)) {
            return String.Empty;
         }

         return value.Trim();
      }

      public static string NormalizeSpace(string value) {

         if (String.IsNullOrEmpty(value)) {
            return String.Empty;
         }

         StringBuilder sb = null;
         int idx, idxStart = 0, idxSpace = 0;

         for (idx = 0; idx < value.Length; idx++) {

            if (IsWhiteSpace(value[idx])) {

               if (idx == idxStart) {

                  // Previous character was a whitespace character, so discard this character
                  idxStart++;

               } else if (value[idx] != ' ' || idxSpace == idx) {

                  // Space was previous character or this is a non-space character
                  if (sb == null) {
                     sb = new StringBuilder(value.Length);
                  } else {
                     sb.Append(' ');
                  }

                  // Copy non-space characters into string builder
                  if (idxSpace == idx) {
                     sb.Append(value, idxStart, idx - idxStart - 1);
                  } else {
                     sb.Append(value, idxStart, idx - idxStart);
                  }

                  idxStart = idx + 1;

               } else {
                  // Single whitespace character doesn't cause normalization, but mark its position
                  idxSpace = idx + 1;
               }
            }
         }

         if (sb == null) {

            // Check for string that is entirely composed of whitespace
            if (idxStart == idx) {
               return String.Empty;
            }

            // If string does not end with a space, then it must already be normalized
            if (idxStart == 0 && idxSpace != idx) {
               return value;
            }

            sb = new StringBuilder(value.Length);

         } else if (idx != idxStart) {
            sb.Append(' ');
         }

         // Copy non-space characters into string builder
         if (idxSpace == idx) {
            sb.Append(value, idxStart, idx - idxStart - 1);
         } else {
            sb.Append(value, idxStart, idx - idxStart);
         }

         return sb.ToString();
      }

      static bool IsWhiteSpace(char c) {
         return c == 0x20
            || c == 0x9
            || c == 0xD
            || c == 0xA;
      }
   }
}