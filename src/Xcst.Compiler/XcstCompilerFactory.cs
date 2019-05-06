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
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Xml;
using Saxon.Api;
using Xcst.Compiler.CodeGeneration;

namespace Xcst.Compiler {

   public class XcstCompilerFactory {

      readonly Processor processor;
      readonly Lazy<XsltExecutable> executable;
      readonly Dictionary<Uri, Func<Stream>> extensions = new Dictionary<Uri, Func<Stream>>();

      public bool EnableExtensions { get; set; }

      public XcstCompilerFactory() {

         this.processor = new Processor();
         this.processor.SetProperty("http://saxon.sf.net/feature/linenumbering", "on");
         this.processor.SetProperty("http://saxon.sf.net/feature/xinclude-aware", "on");

         this.processor.ErrorWriter = TextWriter.Null;

         this.processor.RegisterExtensionFunction(new DocWithUrisFunction(this.processor));
         this.processor.RegisterExtensionFunction(new LineNumberFunction());
         this.processor.RegisterExtensionFunction(new LocalPathFunction());
         this.processor.RegisterExtensionFunction(new MakeRelativeUriFunction());
         this.processor.RegisterExtensionFunction(new PackageLocationFunction());
         this.processor.RegisterExtensionFunction(new PackageManifestFunction(this.processor));
         this.processor.RegisterExtensionFunction(new QNameIdFunction());

         this.executable = new Lazy<XsltExecutable>(CreateCompilerExec);
      }

      XsltExecutable CreateCompilerExec() {

         Type thisType = typeof(XcstCompiler);

         XsltCompiler xsltCompiler = this.processor.NewXsltCompiler();

         Uri baseUri = new UriBuilder {
            Scheme = CompilerResolver.UriSchemeClires,
            Host = null,
            Path = $"{thisType.Assembly.GetName().Name}/{nameof(CodeGeneration)}/xcst-compile.xsl"
         }.Uri;

         Stream zipSource = thisType.Assembly
            .GetManifestResourceStream(typeof(CodeGeneration.PackageManifest), "xcst-xsl.zip");

         using (var archive = new ZipArchive(zipSource, ZipArchiveMode.Read)) {

            XmlResolver resolver = new CompilerResolver(archive, baseUri, LoadExtensionsModule, LoadExtension);

            xsltCompiler.BaseUri = baseUri;
            xsltCompiler.XmlResolver = resolver;
            xsltCompiler.ErrorList = new ArrayList();

            using (var compilerSource = (Stream)resolver.GetEntity(baseUri, null, typeof(Stream))) {

               try {
                  return xsltCompiler.Compile(compilerSource);

               } catch (StaticError ex) {

                  string message;

                  if (xsltCompiler.ErrorList.Count > 0) {

                     StaticError error = xsltCompiler.ErrorList[0] as StaticError;

                     if (error != null) {
                        message = $"{error.Message}{Environment.NewLine}Module URI: {error.ModuleUri}{Environment.NewLine}Line Number: {error.LineNumber}";
                     } else {
                        message = xsltCompiler.ErrorList[0].ToString();
                     }

                  } else {
                     message = ex.Message;
                  }

                  throw new InvalidOperationException(message);
               }
            }
         }
      }

      public XcstCompiler CreateCompiler() {
         return new XcstCompiler(() => this.executable.Value, this.processor);
      }

      public void RegisterExtension(Uri extensionNamespace, Func<Stream> extensionLoader) {

         if (extensionNamespace == null) {
            throw new ArgumentNullException(nameof(extensionNamespace));
         }

         if (!extensionNamespace.IsAbsoluteUri) {
            throw new ArgumentException($"{nameof(extensionNamespace)} must be an absolute URI.", nameof(extensionNamespace));
         }

         if (extensionNamespace.Scheme.Equals(CompilerResolver.UriSchemeClires, StringComparison.OrdinalIgnoreCase)) {
            throw new ArgumentException("Invalid URI.", nameof(extensionNamespace));
         }

         if (extensionLoader == null) {
            throw new ArgumentNullException(nameof(extensionLoader));
         }

         this.extensions[extensionNamespace] = extensionLoader;
      }

      public void RegisterExtensionsForAssembly(Assembly assembly) {

         if (assembly == null) throw new ArgumentNullException(nameof(assembly));

         var attribs = assembly.GetCustomAttributes<XcstExtensionAttribute>();

         foreach (var item in attribs) {

            var loader = (XcstExtensionLoader)Activator.CreateInstance(item.ExtensionLoaderType);

            RegisterExtension(item.ExtensionNamespace, loader.LoadSource);
         }
      }

      Stream LoadExtensionsModule() {

         var stream = new MemoryStream();

         using (var writer = XmlWriter.Create(stream)) {
            BuildExtensionsModule(writer);
         }

         stream.Position = 0;

         return stream;
      }

      void BuildExtensionsModule(XmlWriter writer) {

         const string xsltNs = "http://www.w3.org/1999/XSL/Transform";

         writer.WriteStartElement("stylesheet", xsltNs);
         writer.WriteAttributeString("version", "2.0");

         if (this.EnableExtensions) {
            foreach (Uri ns in this.extensions.Keys) {
               writer.WriteStartElement("import", xsltNs);
               writer.WriteAttributeString("href", ns.AbsoluteUri);
               writer.WriteEndElement();
            }
         }

         writer.WriteEndElement();
      }

      Stream LoadExtension(Uri ns) {

         Func<Stream> loader;

         if (!this.EnableExtensions
            || !this.extensions.TryGetValue(ns, out loader)) {

            return null;
         }

         return loader();
      }

      class CompilerResolver : XmlResolver {

         public static readonly string UriSchemeClires = "clires";

         readonly ZipArchive archive;
         readonly Uri principalModuleUri;
         readonly Uri extensionsModuleUri;

         readonly Func<Stream> loadExtensionsModuleFn;
         readonly Func<Uri, Stream> loadExtensionXslt;

         public override ICredentials Credentials { set { } }

         public CompilerResolver(ZipArchive archive, Uri principalModuleUri, Func<Stream> loadExtensionsModuleFn, Func<Uri, Stream> loadExtensionXslt) {

            if (archive == null) throw new ArgumentNullException(nameof(archive));
            if (principalModuleUri == null) throw new ArgumentNullException(nameof(principalModuleUri));
            if (loadExtensionsModuleFn == null) throw new ArgumentNullException(nameof(loadExtensionsModuleFn));
            if (loadExtensionXslt == null) throw new ArgumentNullException(nameof(loadExtensionXslt));

            this.archive = archive;
            this.principalModuleUri = principalModuleUri;
            this.extensionsModuleUri = new Uri(principalModuleUri, "xcst-extensions.xsl");

            this.loadExtensionsModuleFn = loadExtensionsModuleFn;
            this.loadExtensionXslt = loadExtensionXslt;
         }

         public override object GetEntity(Uri absoluteUri, string role, Type ofObjectToReturn) {

            if (absoluteUri == null) throw new ArgumentNullException(nameof(absoluteUri));

            if (absoluteUri.AbsolutePath.Length <= 1) {
               throw new ArgumentException("The embedded resource name must be specified in the AbsolutePath portion of the supplied Uri.", nameof(absoluteUri));
            }

            if (absoluteUri.Scheme != UriSchemeClires) {
               return this.loadExtensionXslt(absoluteUri);

            } else if (absoluteUri == this.extensionsModuleUri) {
               return loadExtensionsModuleFn();
            }

            Uri relativeUri = this.principalModuleUri.MakeRelativeUri(absoluteUri);
            string fileName = relativeUri.OriginalString;

            if (fileName.Length == 0) {
               fileName = this.principalModuleUri.AbsoluteUri.Split('/').Last();
            }

            return this.archive.GetEntry(fileName)
               .Open();
         }
      }
   }
}
