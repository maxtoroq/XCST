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
using System.Text;

namespace Xcst.Compiler.CodeGeneration {

   static class Helpers {

      public static string LocalPath(Uri uri) {

         if (uri == null) throw new ArgumentNullException(nameof(uri));

         if (!uri.IsAbsoluteUri) {
            return uri.OriginalString;
         }

         if (uri.IsFile) {
            return uri.LocalPath;
         }

         return uri.AbsoluteUri;
      }

      public static Uri MakeRelativeUri(Uri current, Uri compare) {
         return current.MakeRelativeUri(compare);
      }
   }

   static class ValueTemplateEscaper {

      public static string EscapeValueTemplate(string valueTemplate) {

         if (valueTemplate == null) throw new ArgumentNullException(nameof(valueTemplate));

         var quoteIndexes = new List<int>();

         var modeStack = new Stack<ParsingMode>();
         modeStack.Push(ParsingMode.Text);

         Func<ParsingMode> currentMode = () => modeStack.Peek();

         for (int i = 0; i < valueTemplate.Length; i++) {

            char c = valueTemplate[i];
            Func<char?> nextChar = () =>
               i + 1 < valueTemplate.Length ? valueTemplate[i + 1]
               : default(char?);

            switch (currentMode()) {
               case ParsingMode.Code:
                  switch (c) {
                     case '{':
                        modeStack.Push(ParsingMode.Code);
                        break;

                     case '}':
                        modeStack.Pop();
                        break;

                     case '\'':
                        modeStack.Push(ParsingMode.Char);
                        break;

                     case '"':
                        ParsingMode stringMode = ParsingMode.String;

                        switch (valueTemplate[i - 1]) {
                           case '@':
                              if (i - 2 >= 0 && valueTemplate[i - 2] == '$') {
                                 stringMode = ParsingMode.InterpolatedVerbatimString;
                              } else {
                                 stringMode = ParsingMode.VerbatimString;
                              }
                              break;

                           case '$':
                              stringMode = ParsingMode.InterpolatedString;
                              break;
                        }

                        modeStack.Push(stringMode);
                        break;

                     case '/':
                        if (nextChar() == '*') {
                           modeStack.Push(ParsingMode.MultilineComment);
                           i++;
                        }
                        break;
                  }
                  break;

               case ParsingMode.Text:
               case ParsingMode.InterpolatedString:
               case ParsingMode.InterpolatedVerbatimString:
                  switch (c) {
                     case '{':
                        if (nextChar() == '{') {
                           i++;
                        } else {
                           modeStack.Push(ParsingMode.Code);
                        }
                        break;

                     case '"':
                        switch (currentMode()) {
                           case ParsingMode.Text:
                              quoteIndexes.Add(i);
                              break;

                           case ParsingMode.InterpolatedString:
                              modeStack.Pop();
                              break;

                           case ParsingMode.InterpolatedVerbatimString:
                              if (nextChar() == '"') {
                                 i++;
                              } else {
                                 modeStack.Pop();
                              }
                              break;
                        }
                        break;

                     case '\\':
                        if (currentMode() == ParsingMode.InterpolatedString) {
                           i++;
                        }
                        break;
                  }
                  break;

               case ParsingMode.String:
                  switch (c) {
                     case '\\':
                        i++;
                        break;

                     case '"':
                        modeStack.Pop();
                        break;
                  }
                  break;

               case ParsingMode.VerbatimString:
                  if (c == '"') {
                     if (nextChar() == '"') {
                        i++;
                     } else {
                        modeStack.Pop();
                     }
                  }
                  break;

               case ParsingMode.Char:
                  switch (c) {
                     case '\\':
                        i++;
                        break;
                     case '\'':
                        modeStack.Pop();
                        break;
                  }
                  break;

               case ParsingMode.MultilineComment:
                  if (c == '*') {
                     if (nextChar() == '/') {
                        modeStack.Pop();
                        i++;
                     }
                  }
                  break;
            }
         }

         var sb = new StringBuilder(valueTemplate, valueTemplate.Length + quoteIndexes.Count);

         for (int i = 0; i < quoteIndexes.Count; i++) {
            sb.Insert(quoteIndexes[i] + i, '"');
         }

         return sb.ToString();
      }

      enum ParsingMode {
         Text,
         Code,
         InterpolatedString,
         InterpolatedVerbatimString,
         String,
         VerbatimString,
         Char,
         MultilineComment
      }
   }
}


