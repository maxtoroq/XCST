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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using Saxon.Api;
using XPathException = net.sf.saxon.trans.XPathException;
using Xcst.Compiler.Reflection;

namespace Xcst.Compiler {

   public class XcstCompiler {

      readonly Lazy<XsltExecutable>
      _compilerExec;

      readonly Lazy<IDictionary<Uri, XcstExtensionLoader>>
      _extensions;

      readonly Processor
      _processor;

      readonly Dictionary<string[], object?>
      _parameters = new Dictionary<string[], object?>();

      readonly ConcurrentDictionary<string, object>
      _packageLibrary = new ConcurrentDictionary<string, object>();

      Type[]?
      _tbaseTypes;

      public string?
      TargetNamespace { get; set; }

      public string?
      TargetClass { get; set; }

      public CodeVisibility
      TargetVisibility { get; set; } = CodeVisibility.Default;

      public string[]?
      TargetBaseTypes { get; set; }

      public bool
      NamedPackage { get; set; }

      public bool
      NullableAnnotate { get; set; }

      public string?
      NullableContext { get; set; }

      public string?
      UsePackageBase { get; set; }

      public Func<string, Type?>?
      PackageTypeResolver { get; set; }

      public Func<string, Uri?>?
      PackageLocationResolver { get; set; }

      public string?
      PackageFileDirectory { get; set; }

      public string?
      PackageFileExtension { get; set; }

      public XmlResolver?
      ModuleResolver { get; set; }

      public bool
      UseLineDirective { get; set; }

      public string?
      NewLineChars { get; set; }

      public string?
      IndentChars { get; set; }

      public bool
      OpenBraceOnNewLine { get; set; }

      public Func<string, TextWriter>?
      CompilationUnitHandler { get; set; }

      internal
      XcstCompiler(
            Func<XsltExecutable> compilerExecFn, Func<IDictionary<Uri, XcstExtensionLoader>> extensionsFn,
            Processor processor) {

         if (compilerExecFn is null) throw new ArgumentNullException(nameof(compilerExecFn));
         if (extensionsFn is null) throw new ArgumentNullException(nameof(extensionsFn));
         if (processor is null) throw new ArgumentNullException(nameof(processor));

         _compilerExec = new Lazy<XsltExecutable>(compilerExecFn);
         _extensions = new Lazy<IDictionary<Uri, XcstExtensionLoader>>(extensionsFn);
         _processor = processor;
      }

      public void
      SetTargetBaseTypes(params Type[]? targetBaseTypes) {
         _tbaseTypes = targetBaseTypes;
      }

      public void
      SetParameter(string ns, string name, object? value) {

         if (ns is null) throw new ArgumentNullException(nameof(ns));
         if (ns.Length == 0) throw new ArgumentException("ns cannot be empty.", nameof(ns));
         if (name is null) throw new ArgumentNullException(nameof(name));
         if (name.Length == 0) throw new ArgumentException("name cannot be empty.", nameof(name));

         _parameters.Add(new[] { ns, name }, ConvertParameter(value));
      }

      object?
      ConvertParameter(object? value) {

         if (value is Type t) {

            DocumentBuilder docBuilder = _processor.NewDocumentBuilder();
            value = CodeTypeReference(t, docBuilder);

         } else if (value is Delegate) {

            value = WrapExternalObject(value);
         }

         return value;
      }

      public void
      AddPackageLibrary(string assemblyLocation) {

         using (Stream assemblySource = File.OpenRead(assemblyLocation)) {
            AddPackageLibrary(assemblySource);
         }
      }

      public void
      AddPackageLibrary(Stream assemblySource) {

         if (assemblySource is null) throw new ArgumentNullException(nameof(assemblySource));

         XmlWriter writerFn(string packageName) {

            Stream manifest = new MemoryStream();

            if (_packageLibrary.TryAdd(packageName, manifest)) {
               return XmlWriter.Create(manifest);
            }

            throw new InvalidOperationException($"Package '{packageName}' has already been registered.");
         };

         MetadataManifestReader.ReadAssembly(assemblySource, writerFn);
      }

      public CompileResult
      Compile(Uri file) {

         if (file is null) throw new ArgumentNullException(nameof(file));

         XmlResolver resolver = GetModuleResolverOrDefault(this.ModuleResolver);

         if (!file.IsAbsoluteUri) {
            file = resolver.ResolveUri(null, file.OriginalString);
         }

         if (!file.IsAbsoluteUri) {
            throw new ArgumentException("file must be an absolute URI.", nameof(file));
         }

         using (var source = (Stream)resolver.GetEntity(file, null, typeof(Stream))) {
            return Compile(source, file);
         }
      }

      public CompileResult
      Compile(Stream source, Uri? baseUri = null) =>
         Compile(docb => docb.Build(source), baseUri);

      public CompileResult
      Compile(TextReader source, Uri? baseUri = null) =>
         Compile(docb => docb.Build(source), baseUri);

      public CompileResult
      Compile(XmlReader source) =>
         Compile(docb => docb.Build(source));

      CompileResult
      Compile(Func<DocumentBuilder, XdmNode> buildFn, Uri? baseUri = null) {

         XmlResolver moduleResolver = GetModuleResolverOrDefault(this.ModuleResolver);

         DocumentBuilder docBuilder = _processor.NewDocumentBuilder();
         docBuilder.XmlResolver = moduleResolver;

         if (baseUri != null) {
            docBuilder.BaseUri = baseUri;
         }

         XdmNode moduleDoc;

         try {
            moduleDoc = buildFn(docBuilder);

         } catch (XPathException ex) {

            var locator = ex.getLocator();

            throw new CompileException(ex.Message,
               errorCode: ErrorCode(ex),
               moduleUri: locator?.getSystemId() ?? baseUri?.OriginalString,
               lineNumber: locator?.getLineNumber() ?? -1
            );
         }

         XsltTransformer compiler = GetCompiler(moduleDoc);
         compiler.InputXmlResolver = moduleResolver;

         var destination = new XdmDestination();

         try {
            compiler.Run(destination);

         } catch (DynamicError ex) {

            XdmValue errorObject = ex.GetErrorObject();
            var errorData = ModuleUriAndLineNumberFromErrorObject(errorObject);

            string? moduleUri = errorData.Item1 ?? ex.ModuleUri;
            int lineNumber = errorData.Item2 ?? ex.LineNumber;

            throw new CompileException(ex.Message,
               errorCode: ErrorCode(ex),
               moduleUri: moduleUri,
               lineNumber: lineNumber
            );
         }

         XdmNode docEl = destination.XdmNode.FirstElementOrSelf();

         var compiled = new {
            language = new QName("language"),
            href = new QName("href"),
            compilationUnit = CompilerQName("compilation-unit"),
            @ref = CompilerQName("ref"),
         };

         var grammar = new {
            packageManifest = new QName(XmlNamespaces.XcstGrammar, "package-manifest"),
            template = new QName(XmlNamespaces.XcstGrammar, "template"),
            visibility = new QName("visibility"),
            name = new QName("name")
         };

         var publicVisibility = new List<string> { "public", "final", "abstract" };

         var result = new CompileResult {
            Language = docEl.GetAttributeValue(compiled.language),
            CompilationUnits = (this.CompilationUnitHandler == null) ?
               docEl.EnumerateAxis(XdmAxis.Child, compiled.compilationUnit)
                  .AsNodes()
                  .Select(n => n.StringValue)
                  .ToArray()
               : Array.Empty<string>(),
            Templates =
               docEl.EnumerateAxis(XdmAxis.Child, grammar.packageManifest)
                  .AsNodes()
                  .Single()
                  .EnumerateAxis(XdmAxis.Child, grammar.template)
                  .AsNodes()
                  .Where(n => publicVisibility.Contains(n.GetAttributeValue(grammar.visibility)))
                  .Select(n => QualifiedName.Parse(n.GetAttributeValue(grammar.name)).ToString())
                  .ToArray()
         };

         return result;
      }

      XsltTransformer
      GetCompiler(XdmNode sourceDoc) {

         XsltTransformer compiler = _compilerExec.Value.Load();
         compiler.InitialMode = CompilerQName("main");
         compiler.InitialContextNode = sourceDoc;

         // Extension params are loaded first

         foreach (var extension in _extensions.Value) {
            foreach (var param in extension.Value.GetParameters()) {
               compiler.SetParameter(new QName(extension.Key.AbsoluteUri, param.Key), ConvertParameter(param.Value).ToXdmValue());
            }
         }

         // User params can override extension params

         foreach (var pair in _parameters) {
            compiler.SetParameter(new QName(pair.Key[0], pair.Key[1]), pair.Value.ToXdmValue());
         }

         // Compiler params always win

         if (this.CompilationUnitHandler != null) {
            compiler.BaseOutputUri = new Uri("urn:foo"); // Saxon fails if null
            compiler.ResultDocumentHandler = new CompilationUnitResultHandler(this.CompilationUnitHandler, _processor);
            compiler.SetParameter(CompilerQName("source-to-result-document"), true.ToXdmItem());
         }

         if (this.TargetNamespace != null) {
            compiler.SetParameter(CompilerQName("namespace"), this.TargetNamespace.ToXdmItem());
         }

         if (this.TargetClass != null) {
            compiler.SetParameter(CompilerQName("class"), this.TargetClass.ToXdmItem());
         }

         compiler.SetParameter(
            CompilerQName("visibility"),
            (this.TargetVisibility == CodeVisibility.Default ? "#default" : this.TargetVisibility.ToString().ToLowerInvariant())
               .ToXdmItem());

         DocumentBuilder baseTypesBuilder = _processor.NewDocumentBuilder();

         compiler.SetParameter(
            CompilerQName("base-types"),
            new XdmValue(
               _tbaseTypes?.Select(t => CodeTypeReference(t, baseTypesBuilder))
                  ?? this.TargetBaseTypes?.Select(t => CodeTypeReference(t, baseTypesBuilder))
                  ?? Enumerable.Empty<XdmNode>()
            )
         );

         compiler.SetParameter(CompilerQName("nullable-annotate"), this.NullableAnnotate.ToXdmItem());

         if (this.NullableContext != null) {
            compiler.SetParameter(CompilerQName("nullable-context"), this.NullableContext.ToXdmItem());
         }

         compiler.SetParameter(CompilerQName("named-package"), this.NamedPackage.ToXdmItem());

         if (this.UsePackageBase != null) {
            compiler.SetParameter(CompilerQName("use-package-base"), this.UsePackageBase.ToXdmItem());
         }

         if (this.PackageTypeResolver != null) {
            compiler.SetParameter(CompilerQName("package-type-resolver"), WrapExternalObject(this.PackageTypeResolver));
         }

         compiler.SetParameter(CompilerQName("package-library"), WrapExternalObject(_packageLibrary));

         if (this.PackageLocationResolver != null) {
            compiler.SetParameter(CompilerQName("package-location-resolver"), WrapExternalObject(this.PackageLocationResolver));
         }

         if (this.PackageFileDirectory != null) {
            compiler.SetParameter(CompilerQName("package-file-directory"), this.PackageFileDirectory.ToXdmItem());
         }

         if (this.PackageFileExtension != null) {
            compiler.SetParameter(CompilerQName("package-file-extension"), this.PackageFileExtension.ToXdmItem());
         }

         compiler.SetParameter(CompilerQName("use-line-directive"), this.UseLineDirective.ToXdmItem());

         if (this.NewLineChars != null) {
            compiler.SetParameter(CompilerQName("new-line"), this.NewLineChars.ToXdmItem());
         }

         if (this.IndentChars != null) {
            compiler.SetParameter(CompilerQName("indent"), this.IndentChars.ToXdmItem());
         }

         compiler.SetParameter(CompilerQName("open-brace-on-new-line"), this.OpenBraceOnNewLine.ToXdmItem());

         return compiler;
      }

      static XdmValue
      WrapExternalObject(object obj) =>
         new XdmExternalObjectValue(obj);

      internal static T
      UnwrapExternalObject<T>(XdmItem item) {

         object obj = ((XdmExternalObjectValue)item).GetExternalObject();

         return (T)obj;
      }

      static XmlResolver
      GetModuleResolverOrDefault(XmlResolver? moduleResolver) =>
         moduleResolver ?? new XmlUrlResolver();

      internal static QName
      CompilerQName(string local) =>
         new QName(XmlNamespaces.XcstCompiled, local);

      internal static Tuple<string?, int?>
      ModuleUriAndLineNumberFromErrorObject(XdmValue errorObject) {

         XdmAtomicValue[] values = errorObject
            .GetEnumerator()
            .AsAtomicValues()
            .ToArray();

         string? moduleUri = values
            .Select(x => x.ToString())
            .FirstOrDefault();

         int? lineNumber = values
            .Skip(1)
            .Select(x => (int?)(long)x.Value)
            .FirstOrDefault();

         return Tuple.Create((string?)moduleUri, lineNumber);
      }

      internal static string?
      ErrorCode(XPathException ex) =>
         ErrorCode(ex.getErrorCodeNamespace(), ex.getErrorCodeLocalPart());

      internal static string?
      ErrorCode(DynamicError ex) =>
         ErrorCode(ex.ErrorCode?.Uri, ex.ErrorCode?.LocalName);

      internal static string?
      ErrorCode(string? ns, string? name) {

         if (name != null) {

            if (!String.IsNullOrEmpty(ns)
               && ns != XmlNamespaces.XcstErrors) {

               return QualifiedName.UriQualifiedName(ns, name);
            }

            return name;
         }

         return null;
      }

      internal static XdmNode
      CodeTypeReference(string typeName, DocumentBuilder docBuilder) {

         void writeFn(XmlWriter writer) {

            const string ns = XmlNamespaces.XcstCode;
            const string prefix = "code";

            writer.WriteStartElement(prefix, "type-reference", ns);
            writer.WriteAttributeString("name", typeName);
            writer.WriteEndElement();
         }

         return CodeTypeReferenceImpl(writeFn, docBuilder);
      }

      internal static XdmNode
      CodeTypeReference(Type type, DocumentBuilder docBuilder) =>
         CodeTypeReferenceImpl(w => TypeManifestReader.WriteTypeReference(type, w), docBuilder);

      internal static XdmNode
      CodeTypeReferenceImpl(Action<XmlWriter> writeFn, DocumentBuilder docBuilder) {

         using (var output = new MemoryStream()) {

            using (XmlWriter writer = XmlWriter.Create(output)) {
               writeFn(writer);
            }

            output.Position = 0;

            docBuilder.BaseUri = docBuilder.BaseUri
               ?? new Uri(String.Empty, UriKind.Relative);

            return docBuilder.Build(output)
               .FirstElementOrSelf();
         }
      }

      class CompilationUnitResultHandler : IResultDocumentHandler {

         readonly Func<string, TextWriter>
         _writerFn;

         readonly Processor
         _processor;

         public
         CompilationUnitResultHandler(Func<string, TextWriter> writerFn, Processor processor) {

            _writerFn = writerFn;
            _processor = processor;
         }

         public XmlDestination
         HandleResultDocument(string href, Uri? baseUri) {

            TextWriter output = _writerFn(href)
               ?? throw new CompileException($"The function of {nameof(XcstCompiler)}.{nameof(CompilationUnitHandler)} must not return null.");

            var serializer = _processor.NewSerializer(output);
            serializer.SetOutputProperty(Serializer.METHOD, "text");

            return serializer;
         }
      }
   }

   public enum CodeVisibility {
      Default,
      Internal,
      Private,
      Public
   }

   /// <summary>
   /// The result of the <see cref="XcstCompiler.Compile"/> method.
   /// </summary>
   public class CompileResult {

#pragma warning disable CS8618
      public string
      Language { get; internal set; }

      public IReadOnlyList<string>
      CompilationUnits { get; internal set; }

      public IReadOnlyList<string>
      Templates { get; internal set; }
#pragma warning restore CS8618
   }
}
