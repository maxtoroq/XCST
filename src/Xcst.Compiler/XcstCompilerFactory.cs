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
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Xml;
using Saxon.Api;
using Xcst.Compiler.CodeGeneration;
using XPathException = net.sf.saxon.trans.XPathException;

namespace Xcst.Compiler {

   public class XcstCompilerFactory {

      static readonly IDictionary<Uri, XcstExtensionLoader>
      _emptyExtensions = new Dictionary<Uri, XcstExtensionLoader>(0);

      readonly Processor
      _processor;

      readonly Lazy<XsltExecutable>
      _executable;

      readonly Dictionary<Uri, XcstExtensionLoader>
      _extensions = new Dictionary<Uri, XcstExtensionLoader>();

      bool
      _enableExtensions;

      public bool
      EnableExtensions {
         get => _enableExtensions;
         set {
            EnsureExecNotCreated();
            _enableExtensions = value;
         }
      }

      public bool
      ProcessXInclude {
         set =>
            _processor.SetProperty("http://saxon.sf.net/feature/xinclude-aware", (value) ? "on" : "off");
      }

      public
      XcstCompilerFactory() {

         _processor = new Processor();
         _processor.SetProperty("http://saxon.sf.net/feature/linenumbering", "on");

         _processor.ErrorWriter = TextWriter.Null;

         _processor.RegisterExtensionFunction(new InvokeExternalFunctionFunction());
         _processor.RegisterExtensionFunction(new LineNumberFunction());
         _processor.RegisterExtensionFunction(new LocalPathFunction());
         _processor.RegisterExtensionFunction(new PackageLocationFunction());
         _processor.RegisterExtensionFunction(new PackageManifestFunction(_processor));
         _processor.RegisterExtensionFunction(new StringIdFunction());

         _executable = new Lazy<XsltExecutable>(CreateCompilerExec);
      }

      XsltExecutable
      CreateCompilerExec() {

         Assembly thisAssembly = GetType().Assembly;

         XsltCompiler xsltCompiler = _processor.NewXsltCompiler();

         Uri baseUri = new UriBuilder {
            Scheme = CompilerResolver.UriSchemeClires,
            Host = null,
            Path = $"{thisAssembly.GetName().Name}/{nameof(CodeGeneration)}/xcst-compile.xsl"
         }.Uri;

         Stream zipSource = thisAssembly
            .GetManifestResourceStream(typeof(CodeGeneration.PackageManifest), "xcst-xsl.zip");

         using (var archive = new ZipArchive(zipSource, ZipArchiveMode.Read)) {

            XmlResolver resolver = new CompilerResolver(archive, baseUri, LoadExtensionsModule, LoadExtension);

            xsltCompiler.BaseUri = baseUri;
            xsltCompiler.XmlResolver = resolver;
            xsltCompiler.ErrorList = new List<StaticError>();

            using (var compilerSource = (Stream)resolver.GetEntity(baseUri, null, typeof(Stream))) {

               try {
                  return xsltCompiler.Compile(compilerSource);

               } catch (Exception ex) when (ex is StaticError || ex is XPathException) {

                  StaticError? error = xsltCompiler.ErrorList
                     .FirstOrDefault(e => !e.IsWarning);

                  string message;

                  if (error != null) {

                     message = error.Message + Environment.NewLine
                        + "Module URI: " + error.ModuleUri + Environment.NewLine
                        + "Line Number: " + error.LineNumber;

                  } else {
                     message = ex.Message;
                  }

                  throw new InvalidOperationException(message);
               }
            }
         }
      }

      void
      EnsureExecNotCreated() {
         if (_executable.IsValueCreated) {
            throw new InvalidOperationException();
         }
      }

      public XcstCompiler
      CreateCompiler() =>
         new XcstCompiler(() => _executable.Value, GetExtensions, _processor);

      public void
      RegisterExtension(XcstExtensionLoader extensionLoader) {

         if (extensionLoader is null) throw new ArgumentNullException(nameof(extensionLoader));

         Type loaderType = extensionLoader.GetType();
         Assembly assembly = loaderType.Assembly;

         var attrib = assembly.GetCustomAttributes<XcstExtensionAttribute>()
            .Where(x => x.ExtensionLoaderType == loaderType)
            .SingleOrDefault()
            ?? throw new ArgumentException($"Couldn't find extension namespace for loader type '{loaderType}'. "
               + $"Use {nameof(RegisterExtension)}({nameof(Uri)}, {nameof(XcstExtensionLoader)}) instead.", nameof(extensionLoader));

         RegisterExtension(attrib.ExtensionNamespace, extensionLoader);
      }

      public void
      RegisterExtension(Uri extensionNamespace, XcstExtensionLoader extensionLoader) {

         if (extensionNamespace is null) throw new ArgumentNullException(nameof(extensionNamespace));
         if (extensionLoader is null) throw new ArgumentNullException(nameof(extensionLoader));

         EnsureExecNotCreated();

         if (!extensionNamespace.IsAbsoluteUri) {
            throw new ArgumentException($"{nameof(extensionNamespace)} must be an absolute URI.", nameof(extensionNamespace));
         }

         if (extensionNamespace.Scheme.Equals(CompilerResolver.UriSchemeClires, StringComparison.OrdinalIgnoreCase)) {
            throw new ArgumentException("Invalid URI.", nameof(extensionNamespace));
         }

         _extensions[extensionNamespace] = extensionLoader;
      }

      public void
      RegisterExtensionsForAssembly(Assembly assembly) {

         if (assembly is null) throw new ArgumentNullException(nameof(assembly));

         var attribs = assembly.GetCustomAttributes<XcstExtensionAttribute>();

         foreach (var item in attribs) {

            var loader = (XcstExtensionLoader)Activator.CreateInstance(item.ExtensionLoaderType);

            RegisterExtension(item.ExtensionNamespace, loader);
         }
      }

      Stream
      LoadExtensionsModule() {

         var stream = new MemoryStream();

         using (var writer = XmlWriter.Create(stream)) {
            BuildExtensionsModule(writer);
         }

         stream.Position = 0;

         return stream;
      }

      void
      BuildExtensionsModule(XmlWriter writer) {

         const string xsltNs = "http://www.w3.org/1999/XSL/Transform";

         writer.WriteStartElement("stylesheet", xsltNs);
         writer.WriteAttributeString("version", "2.0");

         if (this.EnableExtensions) {
            foreach (Uri ns in _extensions.Keys) {
               writer.WriteStartElement("import", xsltNs);
               writer.WriteAttributeString("href", ns.AbsoluteUri);
               writer.WriteEndElement();
            }
         }

         writer.WriteEndElement();
      }

      Stream?
      LoadExtension(Uri ns) {

         if (this.EnableExtensions
            && _extensions.TryGetValue(ns, out var loader)) {

            return loader.LoadSource();
         }

         return null;
      }

      IDictionary<Uri, XcstExtensionLoader>
      GetExtensions() {

         if (this.EnableExtensions) {
            return _extensions;
         }

         return _emptyExtensions;
      }

      class CompilerResolver : XmlResolver {

         public static readonly string
         UriSchemeClires = "clires";

         readonly ZipArchive
         _archive;

         readonly Uri
         _principalModuleUri;

         readonly Uri
         _extensionsModuleUri;

         readonly Func<Stream>
         _loadExtensionsModuleFn;

         readonly Func<Uri, Stream?>
         _loadExtensionXslt;

         public override ICredentials
         Credentials { set { } }

         public
         CompilerResolver(ZipArchive archive, Uri principalModuleUri, Func<Stream> loadExtensionsModuleFn, Func<Uri, Stream?> loadExtensionXslt) {

            if (archive is null) throw new ArgumentNullException(nameof(archive));
            if (principalModuleUri is null) throw new ArgumentNullException(nameof(principalModuleUri));
            if (loadExtensionsModuleFn is null) throw new ArgumentNullException(nameof(loadExtensionsModuleFn));
            if (loadExtensionXslt is null) throw new ArgumentNullException(nameof(loadExtensionXslt));

            _archive = archive;
            _principalModuleUri = principalModuleUri;
            _extensionsModuleUri = new Uri(principalModuleUri, "xcst-extensions.xsl");

            _loadExtensionsModuleFn = loadExtensionsModuleFn;
            _loadExtensionXslt = loadExtensionXslt;
         }

         public override object?
         GetEntity(Uri absoluteUri, string role, Type ofObjectToReturn) {

            if (absoluteUri is null) throw new ArgumentNullException(nameof(absoluteUri));

            if (absoluteUri.AbsolutePath.Length <= 1) {
               throw new ArgumentException("The embedded resource name must be specified in the AbsolutePath portion of the supplied Uri.", nameof(absoluteUri));
            }

            if (absoluteUri.Scheme != UriSchemeClires) {
               return _loadExtensionXslt(absoluteUri);

            } else if (absoluteUri == _extensionsModuleUri) {
               return _loadExtensionsModuleFn();
            }

            Uri relativeUri = _principalModuleUri.MakeRelativeUri(absoluteUri);
            string fileName = relativeUri.OriginalString;

            if (fileName.Length == 0) {
               fileName = _principalModuleUri.AbsoluteUri.Split('/').Last();
            }

            return _archive
               .GetEntry(fileName)
               .Open();
         }
      }
   }

   [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
   public sealed class XcstExtensionAttribute : Attribute {

      public Uri
      ExtensionNamespace { get; }

      public Type
      ExtensionLoaderType { get; }

      public
      XcstExtensionAttribute(string extensionNamespace, Type extensionLoaderType) {

         if (extensionNamespace is null) throw new ArgumentNullException(nameof(extensionNamespace));
         if (extensionLoaderType is null) throw new ArgumentNullException(nameof(extensionLoaderType));

         this.ExtensionNamespace = new Uri(extensionNamespace, UriKind.Absolute);

         Type expectedType = typeof(XcstExtensionLoader);

         if (!expectedType.IsAssignableFrom(extensionLoaderType)) {
            throw new ArgumentException($"extensionLoaderType must inherit from '{expectedType}'.");
         }

         this.ExtensionLoaderType = extensionLoaderType;
      }
   }

   public abstract class XcstExtensionLoader {

      public abstract Stream
      LoadSource();

      public virtual IEnumerable<KeyValuePair<string, object?>>
      GetParameters() => Enumerable.Empty<KeyValuePair<string, object?>>();
   }
}
