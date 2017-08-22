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

      public static string InterpolatedString(string text) {

         if (text == null) throw new ArgumentNullException(nameof(text));

         var quoteIndexes = new List<int>();

         var modeStack = new Stack<ParsingMode>();
         modeStack.Push(ParsingMode.Text);

         Func<ParsingMode> currentMode = () => modeStack.Peek();

         for (int i = 0; i < text.Length; i++) {

            char c = text[i];
            Func<char?> nextChar = () =>
               i + 1 < text.Length ? text[i + 1]
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

                        switch (text[i - 1]) {
                           case '@':
                              if (i - 2 >= 0 && text[i - 2] == '$') {
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

         var sb = new StringBuilder(text, text.Length + quoteIndexes.Count);

         for (int i = 0; i < quoteIndexes.Count; i++) {
            sb.Insert(quoteIndexes[i] + i, '"');
         }

         return "$@\"" + sb.ToString() + "\"";
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
