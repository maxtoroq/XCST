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

      readonly Dictionary<QualifiedName, object?>
      _parameters = new Dictionary<QualifiedName, object?>();

      public object?
      this[string name] =>
         this[StandardParameters.Parse(name)];

      public object?
      this[QualifiedName name] {
         get {
            if (name is null) throw new ArgumentNullException(nameof(name));

            if (_parameters.TryGetValue(name, out var value)) {
               return value;
            }

            return null;
         }
         set {
            if (name is null) throw new ArgumentNullException(nameof(name));

            if (String.IsNullOrEmpty(name.Namespace)) {

               StandardParameters.Parse(name.Name);

               throw new ArgumentException("Use the strongly-typed properties to set standard parameters.", nameof(name));
            }

            _parameters[name] = value;
         }
      }

      public bool?
      ByteOrderMark {
         get => (bool?)this[StandardParameters.ByteOrderMark];
         set => _parameters[StandardParameters.ByteOrderMark] = value;
      }

      public IList<QualifiedName>?
      CdataSectionElements {
         get {
            var value = (IList<QualifiedName>?)this[StandardParameters.CdataSectionElements];

            if (value != null) {
               value = value.ToList();
            }

            return value;
         }
         set => _parameters[StandardParameters.CdataSectionElements] = value?.ToList();
      }

      public string?
      DoctypePublic {
         get => (string?)this[StandardParameters.DoctypePublic];
         set => _parameters[StandardParameters.DoctypePublic] = value;
      }

      public string?
      DoctypeSystem {
         get => (string?)this[StandardParameters.DoctypeSystem];
         set => _parameters[StandardParameters.DoctypeSystem] = value;
      }

      public Encoding?
      Encoding {
         get => (Encoding?)this[StandardParameters.Encoding];
         set => _parameters[StandardParameters.Encoding] = value;
      }

      public bool?
      EscapeUriAttributes {
         get => (bool?)this[StandardParameters.EscapeUriAttributes];
         set => _parameters[StandardParameters.EscapeUriAttributes] = value;
      }

      public decimal?
      HtmlVersion {
         get => (decimal?)this[StandardParameters.HtmlVersion];
         set => _parameters[StandardParameters.HtmlVersion] = value;
      }

      public bool?
      IncludeContentType {
         get => (bool?)this[StandardParameters.IncludeContentType];
         set => _parameters[StandardParameters.IncludeContentType] = value;
      }

      public bool?
      Indent {
         get => (bool?)this[StandardParameters.Indent];
         set => _parameters[StandardParameters.Indent] = value;
      }

      public int?
      IndentSpaces {
         get => (int?)this[StandardParameters.IndentSpaces];
         set => _parameters[StandardParameters.IndentSpaces] = value;
      }

      public string?
      ItemSeparator {
         get => (string?)this[StandardParameters.ItemSeparator];
         set => _parameters[StandardParameters.ItemSeparator] = value;
      }

      public string?
      MediaType {
         get => (string?)this[StandardParameters.MediaType];
         set => _parameters[StandardParameters.MediaType] = value;
      }

      public QualifiedName?
      Method {
         get => (QualifiedName?)this[StandardParameters.Method];
         set {

            if (value != null
               && String.IsNullOrEmpty(value.Namespace)) {

               value = Methods.Parse(value.Name);
            }

            _parameters[StandardParameters.Method] = value;
         }
      }

      public bool?
      OmitXmlDeclaration {
         get => (bool?)this[StandardParameters.OmitXmlDeclaration];
         set => _parameters[StandardParameters.OmitXmlDeclaration] = value;
      }

      public bool?
      SkipCharacterCheck {
         get => (bool?)this[StandardParameters.SkipCharacterCheck];
         set => _parameters[StandardParameters.SkipCharacterCheck] = value;
      }

      public XmlStandalone?
      Standalone {
         get => (XmlStandalone?)this[StandardParameters.Standalone];
         set => _parameters[StandardParameters.Standalone] = value;
      }

      public IList<QualifiedName>?
      SuppressIndentation {
         get {
            var value = (IList<QualifiedName>?)this[StandardParameters.SuppressIndentation];

            if (value != null) {
               value = value.ToList();
            }

            return value;
         }
         set => _parameters[StandardParameters.SuppressIndentation] = value?.ToList();
      }

      public bool?
      UndeclarePrefixes {
         get => (bool?)this[StandardParameters.UndeclarePrefixes];
         set => _parameters[StandardParameters.UndeclarePrefixes] = value;
      }

      public string?
      Version {
         get => (string?)this[StandardParameters.Version];
         set => _parameters[StandardParameters.Version] = value;
      }

      public
      OutputParameters() { }

      public
      OutputParameters(OutputParameters parameters) {

         if (parameters is null) throw new ArgumentNullException(nameof(parameters));

         Merge(parameters);
      }

      internal void
      Merge(OutputParameters other) {

         foreach (var pair in other._parameters) {
            _parameters[pair.Key] = pair.Value;
         }
      }

      internal decimal?
      RequestedHtmlVersion() {

         decimal? value = this.HtmlVersion;
         decimal versionValue;

         if (value != null
            || this.Version is null
            || !Decimal.TryParse(this.Version, out versionValue)) {

            return value;
         }

         return versionValue;
      }

      static class StandardParameters {

         public static readonly QualifiedName
         ByteOrderMark = new QualifiedName("byte-order-mark");

         public static readonly QualifiedName
         CdataSectionElements = new QualifiedName("cdata-section-elements");

         public static readonly QualifiedName
         DoctypePublic = new QualifiedName("doctype-public");

         public static readonly QualifiedName
         DoctypeSystem = new QualifiedName("doctype-system");

         public static readonly QualifiedName
         Encoding = new QualifiedName("encoding");

         public static readonly QualifiedName
         EscapeUriAttributes = new QualifiedName("escape-uri-attributes");

         public static readonly QualifiedName
         HtmlVersion = new QualifiedName("html-version");

         public static readonly QualifiedName
         IncludeContentType = new QualifiedName("include-content-type");

         public static readonly QualifiedName
         Indent = new QualifiedName("indent");

         public static readonly QualifiedName
         IndentSpaces = new QualifiedName("indent-spaces");

         public static readonly QualifiedName
         ItemSeparator = new QualifiedName("item-separator");

         public static readonly QualifiedName
         MediaType = new QualifiedName("media-type");

         public static readonly QualifiedName
         Method = new QualifiedName("method");

         public static readonly QualifiedName
         OmitXmlDeclaration = new QualifiedName("omit-xml-declaration");

         public static readonly QualifiedName
         SkipCharacterCheck = new QualifiedName("skip-character-check");

         public static readonly QualifiedName
         Standalone = new QualifiedName("standalone");

         public static readonly QualifiedName
         SuppressIndentation = new QualifiedName("suppress-indentation");

         public static readonly QualifiedName
         UndeclarePrefixes = new QualifiedName("undeclare-prefixes");

         public static readonly QualifiedName
         Version = new QualifiedName("version");

         public static QualifiedName
         Parse(string name) =>
            name switch {
               null => throw new ArgumentNullException(nameof(name)),
               "byte-order-mark" => ByteOrderMark,
               "cdata-section-elements" => CdataSectionElements,
               "doctype-public" => DoctypePublic,
               "doctype-system" => DoctypeSystem,
               "encoding" => Encoding,
               "escape-uri-attributes" => EscapeUriAttributes,
               "html-version" => HtmlVersion,
               "include-content-type" => IncludeContentType,
               "indent" => Indent,
               "indent-spaces" => IndentSpaces,
               "item-separator" => ItemSeparator,
               "media-type" => MediaType,
               "method" => Method,
               "omit-xml-declaration" => OmitXmlDeclaration,
               "output-version" => Version,
               "skip-character-check" => SkipCharacterCheck,
               "standalone" => Standalone,
               "suppress-indentation" => SuppressIndentation,
               "undeclare-prefixes" => UndeclarePrefixes,
               "version" => Version,
               _ => throw new ArgumentException("Invalid standard parameter.", nameof(name))
            };
      }

      public static class Methods {

         public static readonly QualifiedName
         Xml = new QualifiedName("xml");

         public static readonly QualifiedName
         Html = new QualifiedName("html");

         public static readonly QualifiedName
         XHtml = new QualifiedName("xhtml");

         public static readonly QualifiedName
         Text = new QualifiedName("text");

         internal static QualifiedName
         Parse(string method) =>
            method switch {
               null => throw new ArgumentNullException(nameof(method)),
               "xml" => Xml,
               "html" => Html,
               "xhtml" => XHtml,
               "text" => Text,
               _ => throw new ArgumentException("Invalid standard method.", nameof(method))
            };
      }
   }

   public enum XmlStandalone {
      Omit,
      Yes,
      No
   }
}
