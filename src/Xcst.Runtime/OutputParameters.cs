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
using System.Linq;
using System.Text;

namespace Xcst {

   public class OutputParameters {

      readonly IDictionary<QualifiedName, object> parameters = new Dictionary<QualifiedName, object>();

      public object this[string name] {
         get { return this[StandardParameters.Parse(name)]; }
      }

      public object this[QualifiedName name] {
         get {
            if (name == null) throw new ArgumentNullException(nameof(name));

            object value;

            if (parameters.TryGetValue(name, out value)) {
               return value;
            }

            return null;
         }
         set {
            if (name == null) throw new ArgumentNullException(nameof(name));

            if (String.IsNullOrEmpty(name.Namespace)) {

               name = StandardParameters.Parse(name.Name);

               throw new ArgumentException("Use the strongly-typed properties to set standard parameters.", nameof(name));
            }

            parameters[name] = value;
         }
      }

      public bool? ByteOrderMark {
         get { return (bool?)this[StandardParameters.ByteOrderMark]; }
         set { parameters[StandardParameters.ByteOrderMark] = value; }
      }

      public IList<QualifiedName> CdataSectionElements {
         get {
            var value = (IList<QualifiedName>)this[StandardParameters.CdataSectionElements];

            if (value != null) {
               value = value.ToList();
            }

            return value;
         }
         set {
            parameters[StandardParameters.CdataSectionElements] = value?.ToList();
         }
      }

      public string DoctypePublic {
         get { return (string)this[StandardParameters.DoctypePublic]; }
         set { parameters[StandardParameters.DoctypePublic] = value; }
      }

      public string DoctypeSystem {
         get { return (string)this[StandardParameters.DoctypeSystem]; }
         set { parameters[StandardParameters.DoctypeSystem] = value; }
      }

      public Encoding Encoding {
         get { return (Encoding)this[StandardParameters.Encoding]; }
         set { parameters[StandardParameters.Encoding] = value; }
      }

      public bool? EscapeUriAttributes {
         get { return (bool?)this[StandardParameters.EscapeUriAttributes]; }
         set { parameters[StandardParameters.EscapeUriAttributes] = value; }
      }

      public decimal? HtmlVersion {
         get { return (decimal?)this[StandardParameters.HtmlVersion]; }
         set { parameters[StandardParameters.HtmlVersion] = value; }
      }

      public bool? IncludeContentType {
         get { return (bool?)this[StandardParameters.IncludeContentType]; }
         set { parameters[StandardParameters.IncludeContentType] = value; }
      }

      public bool? Indent {
         get { return (bool?)this[StandardParameters.Indent]; }
         set { parameters[StandardParameters.Indent] = value; }
      }

      public int? IndentSpaces {
         get { return (int?)this[StandardParameters.IndentSpaces]; }
         set { parameters[StandardParameters.IndentSpaces] = value; }
      }

      public string ItemSeparator {
         get { return (string)this[StandardParameters.ItemSeparator]; }
         set { parameters[StandardParameters.ItemSeparator] = value; }
      }

      public string MediaType {
         get { return (string)this[StandardParameters.MediaType]; }
         set { parameters[StandardParameters.MediaType] = value; }
      }

      public QualifiedName Method {
         get { return (QualifiedName)this[StandardParameters.Method]; }
         set {

            if (value != null
               && String.IsNullOrEmpty(value.Namespace)) {

               value = StandardMethods.Parse(value.Name);
            }

            parameters[StandardParameters.Method] = value;
         }
      }

      public bool? OmitXmlDeclaration {
         get { return (bool?)this[StandardParameters.OmitXmlDeclaration]; }
         set { parameters[StandardParameters.OmitXmlDeclaration] = value; }
      }

      public bool? SkipCharacterCheck {
         get { return (bool?)this[StandardParameters.SkipCharacterCheck]; }
         set { parameters[StandardParameters.SkipCharacterCheck] = value; }
      }

      public XmlStandalone? Standalone {
         get { return (XmlStandalone?)this[StandardParameters.Standalone]; }
         set { parameters[StandardParameters.Standalone] = value; }
      }

      public IList<QualifiedName> SuppressIndentation {
         get {
            var value = (IList<QualifiedName>)this[StandardParameters.SuppressIndentation];

            if (value != null) {
               value = value.ToList();
            }

            return value;
         }
         set {
            parameters[StandardParameters.SuppressIndentation] = value?.ToList();
         }
      }

      public bool? UndeclarePrefixes {
         get { return (bool?)this[StandardParameters.UndeclarePrefixes]; }
         set { parameters[StandardParameters.UndeclarePrefixes] = value; }
      }

      public string Version {
         get { return (string)this[StandardParameters.Version]; }
         set { parameters[StandardParameters.Version] = value; }
      }

      internal void Merge(OutputParameters other) {

         foreach (var pair in other.parameters) {
            this.parameters[pair.Key] = pair.Value;
         }
      }

      internal decimal? RequestedHtmlVersion() {

         decimal? value = this.HtmlVersion;
         decimal versionValue;

         if (value != null
            || this.Version == null
            || !Decimal.TryParse(this.Version, out versionValue)) {

            return value;
         }

         return versionValue;
      }

      static class StandardParameters {

         public static readonly QualifiedName ByteOrderMark = new QualifiedName("byte-order-mark");
         public static readonly QualifiedName CdataSectionElements = new QualifiedName("cdata-section-elements");
         public static readonly QualifiedName DoctypePublic = new QualifiedName("doctype-public");
         public static readonly QualifiedName DoctypeSystem = new QualifiedName("doctype-system");
         public static readonly QualifiedName Encoding = new QualifiedName("encoding");
         public static readonly QualifiedName EscapeUriAttributes = new QualifiedName("escape-uri-attributes");
         public static readonly QualifiedName HtmlVersion = new QualifiedName("html-version");
         public static readonly QualifiedName IncludeContentType = new QualifiedName("include-content-type");
         public static readonly QualifiedName Indent = new QualifiedName("indent");
         public static readonly QualifiedName IndentSpaces = new QualifiedName("indent-spaces");
         public static readonly QualifiedName ItemSeparator = new QualifiedName("item-separator");
         public static readonly QualifiedName MediaType = new QualifiedName("media-type");
         public static readonly QualifiedName Method = new QualifiedName("method");
         public static readonly QualifiedName OmitXmlDeclaration = new QualifiedName("omit-xml-declaration");
         public static readonly QualifiedName SkipCharacterCheck = new QualifiedName("skip-character-check");
         public static readonly QualifiedName Standalone = new QualifiedName("standalone");
         public static readonly QualifiedName SuppressIndentation = new QualifiedName("suppress-indentation");
         public static readonly QualifiedName UndeclarePrefixes = new QualifiedName("undeclare-prefixes");
         public static readonly QualifiedName Version = new QualifiedName("version");

         public static QualifiedName Parse(string name) {

            if (name == null) throw new ArgumentNullException(nameof(name));

            switch (name) {
               case "byte-order-mark":
                  return ByteOrderMark;

               case "cdata-section-elements":
                  return CdataSectionElements;

               case "doctype-public":
                  return DoctypePublic;

               case "doctype-system":
                  return DoctypeSystem;

               case "encoding":
                  return Encoding;

               case "escape-uri-attributes":
                  return EscapeUriAttributes;

               case "html-version":
                  return HtmlVersion;

               case "include-content-type":
                  return IncludeContentType;

               case "indent":
                  return Indent;

               case "indent-spaces":
                  return IndentSpaces;

               case "item-separator":
                  return ItemSeparator;

               case "media-type":
                  return MediaType;

               case "method":
                  return Method;

               case "omit-xml-declaration":
                  return OmitXmlDeclaration;

               case "output-version":
                  return Version;

               case "skip-character-check":
                  return SkipCharacterCheck;

               case "standalone":
                  return Standalone;

               case "suppress-indentation":
                  return SuppressIndentation;

               case "undeclare-prefixes":
                  return UndeclarePrefixes;

               case "version":
                  return Version;

               default:
                  throw new ArgumentException("Invalid standard parameter.", nameof(name));
            }
         }
      }

      internal static class StandardMethods {

         public static readonly QualifiedName Xml = new QualifiedName("xml");
         public static readonly QualifiedName Html = new QualifiedName("html");
         public static readonly QualifiedName XHtml = new QualifiedName("xhtml");
         public static readonly QualifiedName Text = new QualifiedName("text");

         public static QualifiedName Parse(string method) {

            if (method == null) throw new ArgumentNullException(nameof(method));

            switch (method) {
               case "xml":
                  return Xml;

               case "html":
                  return Html;

               case "xhtml":
                  return XHtml;

               case "text":
                  return Text;

               default:
                  throw new ArgumentException("Invalid standard method.", nameof(method));
            }
         }
      }
   }

   public enum XmlStandalone {
      Omit,
      Yes,
      No
   }
}