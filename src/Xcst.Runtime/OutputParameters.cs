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
using System.Xml.Linq;

namespace Xcst;

public class OutputParameters {

   readonly Dictionary<XName, object?>
   _parameters = new();

   public object?
   this[XName name] {
      get {
         Argument.NotNull(name);

         if (_parameters.TryGetValue(name, out var value)) {
            return value;
         }

         return null;
      }
      set {
         Argument.NotNull(name);

         if (String.IsNullOrEmpty(name.NamespaceName)) {

            StandardParameters.Parse(name.LocalName);

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

   public IList<XName>?
   CdataSectionElements {
      get {
         var value = (IList<XName>?)this[StandardParameters.CdataSectionElements];

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
   /*
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
   */
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

   public XName?
   Method {
      get => (XName?)this[StandardParameters.Method];
      set {

         if (value != null
            && String.IsNullOrEmpty(value.NamespaceName)) {

            value = Methods.Parse(value.LocalName);
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
   /*
   public IList<XName>?
   SuppressIndentation {
      get {
         var value = (IList<XName>?)this[StandardParameters.SuppressIndentation];

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
   */
   public string?
   Version {
      get => (string?)this[StandardParameters.Version];
      set => _parameters[StandardParameters.Version] = value;
   }

   public
   OutputParameters() { }

   public
   OutputParameters(OutputParameters parameters) {

      Argument.NotNull(parameters);

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

      decimal? value = null/*this.HtmlVersion*/;

      if (value != null) {
         return value;
      }

      if (this.Version != null
         && Decimal.TryParse(this.Version, out var versionValue)) {

         return versionValue;
      }

      return default;
   }

   static class StandardParameters {

      public static readonly XName
      ByteOrderMark = "byte-order-mark";

      public static readonly XName
      CdataSectionElements = "cdata-section-elements";

      public static readonly XName
      DoctypePublic = "doctype-public";

      public static readonly XName
      DoctypeSystem = "doctype-system";

      public static readonly XName
      Encoding = "encoding";

      public static readonly XName
      EscapeUriAttributes = "escape-uri-attributes";
      /*
      public static readonly XName
      HtmlVersion = "html-version";

      public static readonly XName
      IncludeContentType = "include-content-type";
      */
      public static readonly XName
      Indent = "indent";

      public static readonly XName
      IndentSpaces = "indent-spaces";

      public static readonly XName
      ItemSeparator = "item-separator";

      public static readonly XName
      MediaType = "media-type";

      public static readonly XName
      Method = "method";

      public static readonly XName
      OmitXmlDeclaration = "omit-xml-declaration";

      public static readonly XName
      SkipCharacterCheck = "skip-character-check";

      public static readonly XName
      Standalone = "standalone";
      /*
      public static readonly XName
      SuppressIndentation = "suppress-indentation";

      public static readonly XName
      UndeclarePrefixes = "undeclare-prefixes";
      */
      public static readonly XName
      Version = "version";

      public static XName
      Parse(string name) =>
         name switch {
            null => throw Argument.Null(name),
            "byte-order-mark" => ByteOrderMark,
            "cdata-section-elements" => CdataSectionElements,
            "doctype-public" => DoctypePublic,
            "doctype-system" => DoctypeSystem,
            "encoding" => Encoding,
            "escape-uri-attributes" => EscapeUriAttributes,
            /*
            "html-version" => HtmlVersion,
            "include-content-type" => IncludeContentType,
            */
            "indent" => Indent,
            "indent-spaces" => IndentSpaces,
            "item-separator" => ItemSeparator,
            "media-type" => MediaType,
            "method" => Method,
            "omit-xml-declaration" => OmitXmlDeclaration,
            "output-version" => Version,
            "skip-character-check" => SkipCharacterCheck,
            "standalone" => Standalone,
            /*
            "suppress-indentation" => SuppressIndentation,
            "undeclare-prefixes" => UndeclarePrefixes,
            */
            "version" => Version,
            _ => throw new ArgumentException("Invalid standard parameter.", nameof(name))
         };
   }

   public static class Methods {

      public static readonly XName
      Xml = "xml";

      public static readonly XName
      Html = "html";

      public static readonly XName
      Text = "text";

      internal static XName
      Parse(string method) =>
         method switch {
            null => throw Argument.Null(method),
            "xml" => Xml,
            "html" => Html,
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
