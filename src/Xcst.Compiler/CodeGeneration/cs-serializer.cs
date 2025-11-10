// Copyright 2023 Max Toro Q.
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
using System.Linq;
using System.Xml;
using System.Xml.Linq;

namespace Xcst.Compiler;

partial class CSharpSerializer {

   readonly Dictionary<int, string>
   _indentStrings = new();

   string
   IndentString(int indent) {

      if (indent == 0) {
         return String.Empty;
      }

      if (_indentStrings.TryGetValue(indent, out var str)) {
         return str;
      }

      var value = String.Concat(Enumerable.Repeat(cs_indent, indent));

      _indentStrings[indent] = value;

      return value;
   }

   static object
   ErrorData(XObject node) =>
      new CompileErrorData(LineNumber(node), ModuleUri(node));

   static int
   LineNumber(XObject node) =>
      (node is IXmlLineInfo li) ?
         li.LineNumber
         : -1;

   static string
   ModuleUri(XObject node) {

      if (node.Parent is { } parent) {
         return ModuleUri(parent);
      }

      return node.Document?.BaseUri ?? node.BaseUri;
   }

   static bool
   ParseValueTemplate(string text, XObject contextNode, out int[] quotesToEscape) {

      var quotes = new List<int>();
      var modes = new Stack<ParsingMode>();
      modes.Push(ParsingMode.Text);

      var i = 0;

      char? nextChar() => (i + 1 < text.Length) ?
         text[i + 1]
         : null;

      while (i < text.Length) {

         var currentChar = text[i];
         var currentMode = modes.Peek();

         if (currentMode is ParsingMode.Code) {

            switch (currentChar) {
               case '{':
                  modes.Push(ParsingMode.Code);
                  break;

               case '}':
                  modes.Pop();
                  break;

               case '\'':
                  modes.Push(ParsingMode.Char);
                  break;

               case '"': {

                     var m = text[i - 1] switch {
                        '@' => (i - 2 >= 0 && text[i - 2] == '$') ?
                           ParsingMode.InterpolatedVerbatimString
                           : ParsingMode.VerbatimString,
                        '$' => ParsingMode.InterpolatedString,
                        _ => ParsingMode.String
                     };

                     modes.Push(m);
                     break;
                  }
               case '/':
                  if (nextChar() == '*') {
                     modes.Push(ParsingMode.MultilineComment);
                     i++;
                  }
                  break;
            }

         } else if (currentMode is ParsingMode.Text
               or ParsingMode.InterpolatedString
               or ParsingMode.InterpolatedVerbatimString) {

            switch (currentChar) {
               case '{':
                  if (nextChar() == '{') {
                     i++;
                  } else {
                     modes.Push(ParsingMode.Code);
                  }
                  break;

               case '"':
                  switch (currentMode) {
                     case ParsingMode.Text:
                        quotes.Add(i);
                        break;

                     case ParsingMode.InterpolatedString:
                        modes.Pop();
                        break;

                     case ParsingMode.InterpolatedVerbatimString:
                        if (nextChar() == '"') {
                           i++;
                        } else {
                           modes.Pop();
                        }
                        break;
                  }
                  break;

               case '\\':
                  if (currentMode == ParsingMode.InterpolatedString) {
                     i++;
                  }
                  break;
            }

         } else if (currentMode is ParsingMode.String) {

            switch (currentChar) {
               case '\\':
                  i++;
                  break;

               case '"':
                  modes.Pop();
                  break;
            }

         } else if (currentMode is ParsingMode.VerbatimString) {

            if (currentChar == '"') {
               if (nextChar() == '"') {
                  i++;
               } else {
                  modes.Pop();
               }
            }

         } else if (currentMode is ParsingMode.Char) {

            switch (currentChar) {
               case '\\':
                  i++;
                  break;

               case '\'':
                  modes.Pop();
                  break;
            }

         } else if (currentMode is ParsingMode.MultilineComment) {

            if (currentChar == '*' && nextChar() == '/') {
               modes.Pop();
               i++;
            }
         }

         i++;
      }

      quotesToEscape = quotes.ToArray();

      return modes.Count == 1;
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
