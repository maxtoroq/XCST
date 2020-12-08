#region NormalizeSpace is based on code from .NET Framework
//------------------------------------------------------------------------------
// <copyright file="XsltFunctions.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// <owner current="true" primary="true">[....]</owner>
//------------------------------------------------------------------------------
#endregion

using System;
using System.Text;

namespace Xcst.Runtime {

   partial class SimpleContent {

      public static string
      NormalizeSpace(string? value) {

         if (String.IsNullOrEmpty(value)) {
            return String.Empty;
         }

         StringBuilder? sb = null;
         int idx, idxStart = 0, idxSpace = 0;

         for (idx = 0; idx < value!.Length; idx++) {

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
         Array.IndexOf(_whiteSpaceChars, c) != -1;
   }
}
