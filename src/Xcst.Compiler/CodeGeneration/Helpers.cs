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
using System.Reflection;
using System.Text;
using System.Xml;
using static Xcst.Compiler.XcstCompiler;

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

      public static void PackageManifest(Type packageType, XmlWriter writer) {

         const string ns = XmlNamespaces.XcstSyntax;
         const string prefix = "xcst";

         Func<MethodBase, string> methodVisibility = m =>
            (m.IsAbstract) ? "abstract"
            : (m.IsVirtual) ? "public"
            : "final";

         Func<MemberInfo, string> memberVisibility = m =>
            methodVisibility(m as MethodBase ?? ((PropertyInfo)m).GetGetMethod());

         writer.WriteStartElement(prefix, "package-manifest", ns);
         writer.WriteAttributeString("package-type", packageType.FullName);
         writer.WriteAttributeString("qualified-types", "true");

         foreach (MemberInfo member in packageType.GetMembers(BindingFlags.Instance | BindingFlags.Public)) {

            XcstComponentAttribute attr = member.GetCustomAttribute<XcstComponentAttribute>(inherit: true);

            if (attr == null) {
               continue;
            }

            switch (attr.ComponentKind) {
               case XcstComponentKind.AttributeSet:

                  writer.WriteStartElement(prefix, "attribute-set", ns);
                  writer.WriteAttributeString("name", attr.Name);
                  writer.WriteAttributeString("visibility", memberVisibility(member));
                  writer.WriteAttributeString("member-name", member.Name);

                  break;

               case XcstComponentKind.Function:

                  writer.WriteStartElement(prefix, "function", ns);
                  writer.WriteAttributeString("name", attr.Name ?? member.Name);
                  writer.WriteAttributeString("visibility", memberVisibility(member));
                  writer.WriteAttributeString("member-name", member.Name);

                  MethodInfo method = ((MethodInfo)member);

                  if (method.ReturnType != typeof(void)) {
                     writer.WriteAttributeString("as", TypeReferenceExpression(method.ReturnType));
                  }

                  foreach (ParameterInfo param in method.GetParameters()) {

                     writer.WriteStartElement(prefix, "param", ns);
                     writer.WriteAttributeString("name", param.Name);
                     writer.WriteAttributeString("as", TypeReferenceExpression(param.ParameterType));

                     if (param.IsOptional) {
                        writer.WriteAttributeString("value", Constant(param.RawDefaultValue));
                     }

                     writer.WriteEndElement();
                  }

                  break;

               case XcstComponentKind.Parameter:
                  writer.WriteStartElement(prefix, "param", ns);
                  writer.WriteAttributeString("name", attr.Name ?? member.Name);
                  writer.WriteAttributeString("as", TypeReferenceExpression(((PropertyInfo)member).PropertyType));
                  writer.WriteAttributeString("visibility", memberVisibility(member));
                  writer.WriteAttributeString("member-name", member.Name);
                  break;

               case XcstComponentKind.Template:

                  writer.WriteStartElement(prefix, "template", ns);
                  writer.WriteAttributeString("name", attr.Name);
                  writer.WriteAttributeString("visibility", memberVisibility(member));
                  writer.WriteAttributeString("member-name", member.Name);

                  foreach (var param in member.GetCustomAttributes<XcstTemplateParameterAttribute>(inherit: true)) {

                     writer.WriteStartElement(prefix, "param", ns);
                     writer.WriteAttributeString("name", param.Name);
                     writer.WriteAttributeString("required", XmlConvert.ToString(param.Required));
                     writer.WriteAttributeString("tunnel", XmlConvert.ToString(param.Tunnel));
                     writer.WriteEndElement();
                  }

                  break;

               case XcstComponentKind.Type:

                  Type type = (Type)member;

                  writer.WriteStartElement(prefix, "type", ns);
                  writer.WriteAttributeString("name", attr.Name ?? member.Name);

                  writer.WriteAttributeString("visibility",
                     type.IsAbstract ? "abstract"
                     : type.IsSealed ? "final"
                     : "public");

                  break;

               case XcstComponentKind.Variable:
                  writer.WriteStartElement(prefix, "variable", ns);
                  writer.WriteAttributeString("name", attr.Name ?? member.Name);
                  writer.WriteAttributeString("as", TypeReferenceExpression(((PropertyInfo)member).PropertyType));
                  writer.WriteAttributeString("visibility", memberVisibility(member));
                  writer.WriteAttributeString("member-name", member.Name);
                  break;
            }

            writer.WriteEndElement();
         }

         writer.WriteEndElement();
      }

      public static int QNameId(QualifiedName name) {
         return name.ToUriQualifiedName().GetHashCode();
      }

      static string Constant(object value) {

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


