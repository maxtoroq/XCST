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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
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
#if XCST_RUNTIME
   public
#endif
   class SimpleContent {

      static readonly char[]
      whiteSpaceChars = { (char)0x20, (char)0x9, (char)0xD, (char)0xA };

      static readonly ConcurrentDictionary<Type, bool>
      customToString = new ConcurrentDictionary<Type, bool>();

      readonly Func<IFormatProvider>
      formatProviderFn;

      public static SimpleContent
      Invariant { get; } = new SimpleContent(() => CultureInfo.InvariantCulture);

      internal IFormatProvider
      FormatProvider => formatProviderFn();

      public
      SimpleContent(Func<IFormatProvider> formatProviderFn) {

         if (formatProviderFn is null) throw new ArgumentNullException(nameof(formatProviderFn));

         this.formatProviderFn = formatProviderFn;
      }

      public string
      Join(string separator, Array value) =>
         JoinSequence(separator, value);

      public string
      Join(string separator, string[] value) =>
         Join(separator, (IEnumerable<string>)value);

      public string
      Join(string separator, IEnumerable<string> value) {

         if (value is null) {
            return String.Empty;
         }

         return String.Join(separator, value.Where(v => v != null));
      }

      public string
      Join(string separator, object value) {

         if (ValueAsEnumerable(value) is IEnumerable seq) {
            return JoinSequence(separator, seq);
         }

         return Convert(value);
      }

      public string
      Join(string separator, string value) =>
         value ?? String.Empty;

      public string
      Join(string separator, IFormattable value) {

         return value?.ToString(null, this.FormatProvider)
            ?? String.Empty;
      }

      protected string
      JoinSequence(string separator, IEnumerable value) {

         if (value is null) {
            return String.Empty;
         }

         return Join(separator, value
            .Cast<object>()
            .Where(v => v != null)
            .Select(v => Convert(v)));
      }

      internal static IEnumerable?
      ValueAsEnumerable(object? value, bool checkToString = true) {

         if (value is null
            || value is string
            || value is IFormattable) {

            return null;
         }

         Type type;

         if (value is IEnumerable seq
            && ((type = value.GetType()).IsArray
               || (!checkToString || !HasCustomToString(type)))) {

            return seq;
         }

         return null;
      }

      static bool
      HasCustomToString(Type type) =>
         customToString.GetOrAdd(type, HasCustomToStringImpl);

      static bool
      HasCustomToStringImpl(Type type) {

         Type declaringType = type.GetMethod("ToString", Type.EmptyTypes).DeclaringType;

         return declaringType != ((type.IsValueType) ?
            typeof(ValueType) : typeof(object));
      }

      public string
      Format(string format, params object[] args) =>
         String.Format(this.FormatProvider, format, args);

      public string
      FormatValueTemplate(FormattableString value) {

         if (value.ArgumentCount == 0) {
            // Shouldn't be, but just in case...
            return value.ToString(this.FormatProvider);
         }

         object[] args = value.GetArguments();

         for (int i = 0; i < args.Length; i++) {

            if (ValueAsEnumerable(args[i]) is IEnumerable seq) {
               args[i] = Join(" ", seq);
            }
         }

         return Format(value.Format, args);
      }

      public string
      Convert(object value) =>
         System.Convert.ToString(value, this.FormatProvider);

      public static string
      Trim(string? value) {

         if (String.IsNullOrEmpty(value)) {
            return String.Empty;
         }

         return value.Trim(whiteSpaceChars);
      }

      public static string
      NormalizeSpace(string value) {

         if (String.IsNullOrEmpty(value)) {
            return String.Empty;
         }

         StringBuilder? sb = null;
         int idx, idxStart = 0, idxSpace = 0;

         for (idx = 0; idx < value.Length; idx++) {

            if (IsWhiteSpace(value[idx])) {

               if (idx == idxStart) {

                  // Previous character was a whitespace character, so discard this character
                  idxStart++;

               } else if (value[idx] != ' ' || idxSpace == idx) {

                  // Space was previous character or this is a non-space character
                  if (sb is null) {
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

         if (sb is null) {

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

      static bool
      IsWhiteSpace(char c) =>
         Array.IndexOf(whiteSpaceChars, c) != -1;
   }
}
