﻿// Copyright 2021 Max Toro Q.
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
using Xcst.Runtime;

namespace Xcst.Compiler;

partial class XcstCompilerPackage {

   public static IEnumerable<XAttribute>
   attributes(XElement node) =>
      node.Attributes()
         .Where(p => !p.IsNamespaceDeclaration);

   public static IEnumerable<XAttribute>
   attributes(IEnumerable<XElement> nodes, XName name) =>
      nodes.Select(p => p.Attribute(name))
         .Where(p => p != null)
         .Select(p => p!);

   XDocument
   fn_doc(Uri uri) {

      if (!uri.IsAbsoluteUri) {
         uri = ((IXcstPackage)this).Context.ResolveUri(uri.OriginalString);
      }

      var readerSettings = new XmlReaderSettings {
         XmlResolver = src_module_resolver,
         DtdProcessing = DtdProcessing.Parse
      };

      var opts = LoadOptions.PreserveWhitespace
         | LoadOptions.SetLineInfo
         | LoadOptions.SetBaseUri;

      using var reader = XmlReader.Create(uri.AbsoluteUri, readerSettings);

      return XDocument.Load(reader, opts);
   }

   public static bool
   fn_empty<T>(T[] p) => p.Length == 0;

   public static bool
   fn_empty<T>(IEnumerable<T> p) => !p.Any();

   static string
   fn_name(XObject node) =>
      node switch {
         XAttribute a => fn_substring_before(a.ToString(), '='),
         XElement el => fn_string(el.Name, el),
         _ => throw new NotImplementedException()
      };

   static string?
   fn_namespace_uri_for_prefix(string? prefix, XElement el) {

      if (String.IsNullOrEmpty(prefix)) {

         // System.Xml.Linq.XElement.GetDefaultNamespace() returns a value
         // even if no declaration for the default namespace exists

         var namespaceOfPrefixInScope = GetNamespaceOfPrefixInScope(el, "xmlns", null);

         if (namespaceOfPrefixInScope is null) {
            return null;
         }

         return XNamespace.Get(namespaceOfPrefixInScope).NamespaceName;
      }

      return el.GetNamespaceOfPrefix(prefix)?.NamespaceName;

      static string?
      GetNamespaceOfPrefixInScope(XElement el, string prefix, XElement? outOfScope) {

         // see System.Xml.Linq.XElement.GetNamespaceOfPrefixInScope()

         var e = el;

         while (e != outOfScope) {

            var a = e.LastAttribute;

            while (a != null) {

               if (a.IsNamespaceDeclaration
                  && a.Name.LocalName == prefix) {

                  return a.Value;
               }

               a = a.PreviousAttribute;
            }

            e = e.Parent;
         }

         return null;
      }
   }

   static string
   fn_normalize_space(string? str) =>
      SimpleContent.NormalizeSpace(str);

   public static IEnumerable<XElement>
   preceding_sibling(XNode node, object name) {

      var result = node.ElementsBeforeSelf()
         .Reverse();

      result = name switch {
         XName xname => result.Where(p => p.Name == xname),
         XNamespace ns => result.Where(p => p.Name.Namespace == ns),
         _ => throw new ArgumentOutOfRangeException(),
      };

      return result;
   }

   static string
   fn_replace(string? input, string pattern, string replacement) =>
      Regex.Replace(input ?? String.Empty, pattern, replacement);

   XName
   fn_resolve_QName(string qname, XElement el) {

      var colonIndex = qname.IndexOf(':');

      if (colonIndex != -1) {

         var local = qname.Substring(colonIndex + 1);
         var prefix = qname.Substring(0, colonIndex);
         var ns = el.GetNamespaceOfPrefix(prefix)?.NamespaceName
            ?? throw new RuntimeException(
               $"There's no namespace binding for prefix '{prefix}'.",
               errorCode: XName.Get("FONS0004", XmlNamespaces.XcstErrors),
               errorData: ErrorData(el));

         return XName.Get(local, ns);
      }

      return el.GetDefaultNamespace() + qname;
   }

   static Uri
   fn_resolve_uri(Uri relative, string baseUri) {

      if (!(!String.IsNullOrEmpty(baseUri)
         && Uri.TryCreate(baseUri, UriKind.Absolute, out var uri))) {

         throw new ArgumentException("baseUri is not usable.", nameof(baseUri));
      }

      return new Uri(uri, relative);
   }

   public static IEnumerable<XElement>
   select(IEnumerable<XElement> nodes, params object[] names) =>
      nodes.SelectMany(p => select(p, names));

   public static IEnumerable<XElement>
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
   fn_string(bool value) =>
      (value) ? "true" : "false";

   static string
   fn_string(int value) => XmlConvert.ToString(value);

   static string
   fn_string(decimal value) => XmlConvert.ToString(value);

   static string
   fn_string(XName qname, XElement? context) {

      if (context is null) {
         return qname.LocalName;
      }

      var prefix = context.GetPrefixOfNamespace(qname.Namespace);

      if (prefix != null) {
         return prefix + ":" + qname.LocalName;
      }

      return qname.LocalName;
   }

   static string
   fn_string(XObject node) =>
      node switch {
         XAttribute a => a.Value,
         XElement el => el.Value,
         _ => throw new NotImplementedException()
      };

   static string
   fn_substring_after(string str, char c) {

      var i = str.IndexOf(c);
      return str.Substring(i + 1);
   }

   static string
   fn_substring_before(string str, char c) {

      var i = str.IndexOf(c);
      return str.Substring(0, i);
   }

   public static string[]
   fn_tokenize(string str) =>
      DataType.List(str, DataType.String)
         .ToArray();

   static string
   trim(string? str) =>
      SimpleContent.Trim(str);

   string?
   fn_unparsed_text(Uri uri) {

      if (src_module_resolver.GetEntity(uri, null, typeof(Stream)) is Stream entity) {
         using (entity) {
            return new StreamReader(entity).ReadToEnd();
         }
      }

      return null;
   }

   public static bool
   xs_boolean(XObject node) =>
      XmlConvert.ToBoolean(fn_string(node));

   public static int
   xs_integer(string str) =>
      XmlConvert.ToInt32(str);

   public static int
   xs_integer(XObject node) =>
      xs_integer(fn_string(node));
}
