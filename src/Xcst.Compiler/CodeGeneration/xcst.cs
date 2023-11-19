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
using System.IO;
using System.Xml;
using System.Xml.Linq;

namespace Xcst.Compiler;

partial class XcstCompilerPackage {

   private bool
   V1 => src_target_runtime < 2m;

   private bool
   V2_OR_GREATER => src_target_runtime >= 2m;

   Uri?
   PackageLocation(string packageName, Uri? usingPackageUri) {

      if (src_package_location_resolver != null) {
         return src_package_location_resolver.Invoke(packageName);
      }

      var fileDirectory = src_package_file_directory;
      var fileExtension = src_package_file_extension;

      if (fileDirectory is null
         && usingPackageUri?.IsFile == true) {

         fileDirectory = Path.GetDirectoryName(usingPackageUri.LocalPath);
      }

      if (!String.IsNullOrEmpty(fileDirectory)
         && !String.IsNullOrEmpty(fileExtension)) {

         return FindNamedPackage(packageName, fileDirectory!, fileExtension!);
      }

      return null;
   }

   static Uri?
   FindNamedPackage(string packageName, string directory, string extension) {

      if (packageName is null) throw new ArgumentNullException(nameof(packageName));
      if (packageName.Length == 0) throw new ArgumentException(nameof(packageName));

      var dir = directory;
      var search = "*." + extension;

      if (!Directory.Exists(dir)) {
         return null;
      }

      foreach (var path in Directory.EnumerateFiles(dir, search, SearchOption.AllDirectories)) {

         if (Path.GetFileNameWithoutExtension(path)[0] == '_') {
            continue;
         }

         var readerSettings = new XmlReaderSettings {
            IgnoreComments = true,
            IgnoreProcessingInstructions = true,
            IgnoreWhitespace = true,
            ValidationType = ValidationType.None,
            DtdProcessing = DtdProcessing.Ignore
         };

         using var reader = XmlReader.Create(path, readerSettings);

         while (reader.Read()) {

            if (reader.NodeType == XmlNodeType.Element) {

               if (reader.LocalName == "package"
                  && reader.NamespaceURI == XmlNamespaces.Xcst
                  && trim(reader.GetAttribute("name")) == packageName) {

                  return new Uri(path, UriKind.Absolute);
               }

               break;
            }
         }
      }

      return null;
   }

   object
   ErrorData(XObject node) {

      dynamic data = new System.Dynamic.ExpandoObject();
      data.LineNumber = LineNumber(node);
      data.ModuleUri = ModuleUri(node);

      return data;
   }

   static int
   LineNumber(XObject node) =>
      (node is IXmlLineInfo li) ?
         li.LineNumber
         : -1;

   string
   ModuleUri(XObject node) {

      if (xi_aware
         && node.Annotation<XIncludedAnnotation>() is { } ann) {

         return ann.Location;
      }

      if (node.Parent is { } parent) {
         return ModuleUri(parent);
      }

      return node.Document?.BaseUri ?? node.BaseUri;
   }

   static string
   StringId(string value) =>
      XmlConvert.ToString(GetHashCodeDeterministic(value));
}

enum TypeCardinality {
   ZeroOrMore,
   One
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
