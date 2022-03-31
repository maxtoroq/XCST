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
using Xcst.PackageModel;
using SequenceWriter = Xcst.Runtime.SequenceWriter;

namespace Xcst.Compiler {

   using TypeManifestReader = Reflection.TypeManifestReader;

   partial class XcstCompilerPackage {

      internal XDocument?
      PackageManifest(string packageName, XElement usePackageEl) {

         Type? packageType;
         QualifiedName errorCode = new("XTSE3000", XmlNamespaces.XcstErrors);

         try {
            packageType = src_package_type_resolver?.Invoke(packageName);

         } catch (Exception ex) {

            throw new RuntimeException(ex.Message,
               errorCode: errorCode,
               errorData: ErrorData(usePackageEl)
            );
         }

         if (packageType != null) {

            if (!TypeManifestReader.IsXcstPackage(packageType)) {

               throw new RuntimeException($"{packageType.FullName} is not a valid XCST package.",
                  errorCode: errorCode,
                  errorData: ErrorData(usePackageEl)
               );
            }

            XDocument doc = new();

            using (XmlWriter writer = doc.CreateWriter()) {
               new TypeManifestReader(writer)
                  .WritePackage(packageType);
            }

            return doc;
         }

         if (src_package_library != null
            && src_package_library.TryGetValue(packageName, out var manifest)) {

            return manifest;
         }

         return null;
      }

      internal Uri?
      PackageLocation(string packageName, Uri? usingPackageUri) {

         if (src_package_location_resolver != null) {
            return src_package_location_resolver.Invoke(packageName);
         }

         string? fileDirectory = src_package_file_directory;
         string? fileExtension = src_package_file_extension;

         if (fileDirectory is null
            && usingPackageUri?.IsFile == true) {

            fileDirectory = Path.GetDirectoryName(usingPackageUri.LocalPath);
         }

         if (!String.IsNullOrEmpty(fileDirectory)
            && !String.IsNullOrEmpty(fileExtension)) {

            return FindNamedPackage(packageName, fileDirectory, fileExtension);
         }

         return null;
      }

      static Uri?
      FindNamedPackage(string packageName, string directory, string extension) {

         if (packageName is null) throw new ArgumentNullException(nameof(packageName));
         if (packageName.Length == 0) throw new ArgumentException(nameof(packageName));

         string dir = directory;
         string search = "*." + extension;

         if (!Directory.Exists(dir)) {
            return null;
         }

         foreach (string path in Directory.EnumerateFiles(dir, search, SearchOption.AllDirectories)) {

            if (Path.GetFileNameWithoutExtension(path)[0] == '_') {
               continue;
            }

            XmlReaderSettings readerSettings = new() {
               IgnoreComments = true,
               IgnoreProcessingInstructions = true,
               IgnoreWhitespace = true,
               ValidationType = ValidationType.None,
               DtdProcessing = DtdProcessing.Ignore
            };

            using (var reader = XmlReader.Create(path, readerSettings)) {

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
         }

         return null;
      }

      static object
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

      static string
      ModuleUri(XObject node) =>
         node.Document?.BaseUri ?? node.BaseUri;

      static string
      StringId(string value) =>
         XmlConvert.ToString(value.GetHashCode());

      IXcstPackage?
      ExtensionPackage(XElement el) {

         if (this.src_extensions != null
            && Uri.TryCreate(el.Name.NamespaceName, UriKind.Absolute, out var nsUri)
            && this.src_extensions.TryGetValue(nsUri, out var extPkg)) {

            return extPkg;
         }

         return null;
      }

      static bool
      HasTemplate<TItem>(IXcstPackage pkg, XName mode) =>
         pkg.GetTemplate(new QualifiedName(mode.LocalName, mode.NamespaceName), SequenceWriter.Create<TItem>()) != null;

      static bool
      HasMode<TItem>(IXcstPackage pkg, XName mode) =>
         pkg.GetMode(new QualifiedName(mode.LocalName, mode.NamespaceName), SequenceWriter.Create<TItem>()) != null;
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
}
