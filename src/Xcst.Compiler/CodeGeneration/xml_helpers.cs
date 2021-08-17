// Copyright 2021 Max Toro Q.
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
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using Xcst.PackageModel;
using Xcst.Runtime;

namespace Xcst.Compiler {

   partial class XcstCompilerPackage {

      static IEnumerable<XAttribute>
      attributes(XElement node) =>
         node.Attributes()
            .Where(p => !p.IsNamespaceDeclaration);

      static IEnumerable<XAttribute>
      attributes(IEnumerable<XElement> nodes, XName name) =>
         nodes.Select(p => p.Attribute(name))
            .Where(p => p != null)
            .Select(p => p!);

      XDocument
      doc(Uri uri) {

         if (!uri.IsAbsoluteUri) {
            uri = ((IXcstPackage)this).Context.ResolveUri(uri.OriginalString);
         }

         XmlReaderSettings readerSettings = new() {
            XmlResolver = src_module_resolver,
            DtdProcessing = DtdProcessing.Parse
         };

         LoadOptions opts = LoadOptions.PreserveWhitespace
            | LoadOptions.SetLineInfo
            | LoadOptions.SetBaseUri;

         using (var reader = XmlReader.Create(uri.AbsoluteUri, readerSettings)) {
            return XDocument.Load(reader, opts);
         }
      }

      static bool
      empty<T>(T[] p) => p.Length == 0;

      static bool
      empty<T>(IEnumerable<T> p) => !p.Any();

      static string
      name(XObject node) =>
         node switch {
            XAttribute a => substring_before(a.ToString(), '='),
            XElement el => @string(el.Name, el),
            _ => throw new NotImplementedException()
         };

      static string?
      namespace_uri_for_prefix(string? prefix, XElement el) {

         if (String.IsNullOrEmpty(prefix)) {

            XNamespace def = el.GetDefaultNamespace();

            if (def == XNamespace.None) {
               return String.Empty;
            }

            return def.NamespaceName;
         }

         return el.GetNamespaceOfPrefix(prefix)?.NamespaceName;
      }

      static string
      normalize_space(string? str) =>
         SimpleContent.NormalizeSpace(str);

      static string
      replace(string? input, string pattern, string replacement) =>
         Regex.Replace(input ?? String.Empty, pattern, replacement);

      static XName
      resolve_QName(string qname, XElement el) {

         int colonIndex = qname.IndexOf(':');

         if (colonIndex != -1) {

            string local = qname.Substring(colonIndex + 1);
            string prefix = qname.Substring(0, colonIndex);
            string ns = el.GetNamespaceOfPrefix(prefix)?.NamespaceName
               ?? throw new RuntimeException(
                  $"There's no namespace binding for prefix '{prefix}'.",
                  errorCode: new QualifiedName("FONS0004", XmlNamespaces.XcstErrors),
                  errorData: ErrorData(el));

            return XName.Get(local, ns);
         }

         return el.GetDefaultNamespace() + qname;
      }

      static Uri
      resolve_uri(Uri relative, string baseUri) {

         if (!(!String.IsNullOrEmpty(baseUri)
            && Uri.TryCreate(baseUri, UriKind.Absolute, out var uri))) {

            throw new ArgumentException("baseUri is not usable.", nameof(baseUri));
         }

         return new Uri(uri, relative);
      }

      static IEnumerable<XElement>
      select(IEnumerable<XElement> nodes, params object[] names) =>
         nodes.SelectMany(p => select(p, names));

      static IEnumerable<XElement>
      select(XElement? node, params object[] names) {

         if (node is null) {
            return Enumerable.Empty<XElement>();
         }

         IEnumerable<XElement> selected = new XElement[] { node };

         for (int i = 0; i < names.Length; i++) {

            selected = names[i] switch {
               XName name => selected.SelectMany(p => p.Elements(name)),
               XNamespace ns => selected.SelectMany(p => p.Elements().Where(p2 => p2.Name.Namespace == ns)),
               _ => throw new ArgumentOutOfRangeException(),
            };
         }

         return selected;
      }

      static string
      @string(bool value) =>
         (value) ? "true" : "false";

      static string
      @string(int value) => XmlConvert.ToString(value);

      static string
      @string(decimal value) => XmlConvert.ToString(value);

      static string
      @string(XName qname, XElement? context) {

         if (context is null) {
            return qname.LocalName;
         }

         string? prefix = context.GetPrefixOfNamespace(qname.Namespace);

         if (prefix != null) {
            return prefix + ":" + qname.LocalName;
         }

         return qname.LocalName;
      }

      static string
      @string(XObject node) =>
         node switch {
            XAttribute a => a.Value,
            XElement el => el.Value,
            _ => throw new NotImplementedException()
         };

      static string
      substring_after(string str, char c) {

         int i = str.IndexOf(c);
         return str.Substring(i + 1);
      }

      static string
      substring_before(string str, char c) {

         int i = str.IndexOf(c);
         return str.Substring(0, i);
      }

      static string[]
      tokenize(string str) =>
         DataType.List(str, DataType.String)
            .ToArray();

      static string
      trim(string? str) =>
         SimpleContent.Trim(str);

      string?
      unparsed_text(Uri uri) {

         var entity = (Stream?)src_module_resolver.GetEntity(uri, null, typeof(Stream));

         if (entity is null) {
            return null;
         }

         using (entity) {
            return new StreamReader(entity).ReadToEnd();
         }
      }

      static bool
      xs_boolean(XObject node) =>
         XmlConvert.ToBoolean(@string(node));

      static int
      xs_integer(XObject node) =>
         XmlConvert.ToInt32(@string(node));
   }
}
