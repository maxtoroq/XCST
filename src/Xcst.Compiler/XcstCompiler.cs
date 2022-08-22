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
using System.Xml;
using System.Xml.Linq;
using Xcst.Compiler.Reflection;
using DataType = Xcst.Runtime.DataType;

namespace Xcst.Compiler;

public class XcstCompiler {

   readonly Dictionary<string, XDocument>
   _packageLibrary = new();

   readonly List<Func<IXcstPackage>>
   _extensionFactories = new();

   readonly Dictionary<string, object?>
   _tunnelParams = new();

   Type[]?
   _tbaseTypes;

   public decimal
   TargetRuntime { get; set; }

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

   public bool
   XIncludeAware { get; set; } = true;

   public void
   RegisterExtension(Func<IXcstPackage> extensionFactory) {

      if (extensionFactory is null) throw new ArgumentNullException(nameof(extensionFactory));

      _extensionFactories.Add(extensionFactory);
   }

   public void
   SetTargetBaseTypes(params Type[]? targetBaseTypes) {
      _tbaseTypes = targetBaseTypes;
   }

   public void
   AddPackageLibrary(string assemblyLocation) {

      using var assemblySource = File.OpenRead(assemblyLocation);
      AddPackageLibrary(assemblySource);
   }

   public void
   AddPackageLibrary(Stream assemblySource) {

      if (assemblySource is null) throw new ArgumentNullException(nameof(assemblySource));

      MetadataManifestReader.ReadAssembly(assemblySource, CreatePackageLibraryManifestWriter);
   }

   XmlWriter
   CreatePackageLibraryManifestWriter(string packageName) {

      var manifest = new XDocument();

      if (!_packageLibrary.ContainsKey(packageName)) {
         _packageLibrary.Add(packageName, manifest);
         return manifest.CreateWriter();
      }

      throw new InvalidOperationException($"Package '{packageName}' has already been registered.");
   }

   // fine-grained customization for extensions
   // params are cleared after each run
   public void
   SetTunnelParam(string name, object? value) {
      _tunnelParams[name] = value;
   }

   public CompileResult
   Compile(Uri file) {

      if (file is null) throw new ArgumentNullException(nameof(file));

      var resolver = GetModuleResolverOrDefault(this.ModuleResolver);

      if (!file.IsAbsoluteUri) {
         file = resolver.ResolveUri(null, file.OriginalString);
      }

      if (!file.IsAbsoluteUri) {
         throw new ArgumentException("file must be an absolute URI.", nameof(file));
      }

      using var source = resolver.GetEntity(file, null, typeof(Stream)) as Stream
         ?? throw new ArgumentException("file not found.", nameof(file));

      return Compile(
         (settings, baseUri) => XmlReader.Create(source, settings, baseUri),
         file,
         resolver
      );
   }

   public CompileResult
   Compile(Stream source, Uri? baseUri = null) =>
      Compile((settings, baseUri) => XmlReader.Create(source, settings, baseUri), baseUri);

   public CompileResult
   Compile(TextReader source, Uri? baseUri = null) =>
      Compile((settings, baseUri) => XmlReader.Create(source, settings, baseUri), baseUri);

   public CompileResult
   Compile(XmlReader source) =>
      Compile((settings, baseUri) => source, externalReader: true);

   CompileResult
   Compile(Func<XmlReaderSettings, string?, XmlReader> readerFn, Uri? baseUri = null, XmlResolver? moduleResolver = null, bool externalReader = false) {

      moduleResolver ??= GetModuleResolverOrDefault(moduleResolver);

      var settings = new XmlReaderSettings {
         XmlResolver = moduleResolver,
         DtdProcessing = DtdProcessing.Parse
      };

      string? baseUriStr = null;

      if (baseUri != null) {

         if (!baseUri.IsAbsoluteUri) {
            baseUri = moduleResolver.ResolveUri(null, baseUri.OriginalString);
         }

         baseUriStr = baseUri.AbsoluteUri;
      }

      var loadOpts = LoadOptions.PreserveWhitespace
         | LoadOptions.SetBaseUri
         | LoadOptions.SetLineInfo;

      var reader = readerFn(settings, baseUriStr);
      XDocument moduleDoc;

      try {
         moduleDoc = XDocument.Load(reader, loadOpts);

      } finally {

         if (!externalReader) {
            reader.Close();
         }
      }

      var compilerEval = GetCompilerEvaluator(moduleDoc, moduleResolver);
      var resultDoc = new XDocument();

      using (var resultWriter = resultDoc.CreateWriter()) {
         compilerEval
            .OutputTo(resultWriter)
            .Run();
      }

      var docEl = resultDoc.Root!;

      XNamespace src = XmlNamespaces.XcstCompiled;
      XNamespace xcst = XmlNamespaces.XcstGrammar;

      var result = new CompileResult {
         Language = docEl.Attribute("language")!.Value,
         CompilationUnits = (this.CompilationUnitHandler == null) ?
            docEl.Elements(src + "compilation-unit")
               .Select(p => p.Value)
               .ToArray()
            : Array.Empty<string>(),
         Templates =
            (from t in docEl.Element(xcst + "package-manifest")!
               .Elements(xcst + "template")
             where t.Attribute("visibility")!.Value is "public" or "final" or "abstract"
             select DataType.QName(t.Attribute("name")!.Value))
            .ToArray()
      };

      return result;
   }

   XcstTemplateEvaluator
   GetCompilerEvaluator(XDocument sourceDoc, XmlResolver moduleResolver) {

      var compiler = new XcstCompilerPackage();
      var evaluator = XcstEvaluator.Using(compiler);

      if (this.TargetRuntime != default) {
         evaluator.WithParam(nameof(compiler.src_target_runtime), this.TargetRuntime);
      }

      if (this.CompilationUnitHandler != null) {
         evaluator.WithParam(nameof(compiler.src_compilation_unit_handler), this.CompilationUnitHandler);
      }

      if (this.TargetNamespace != null) {
         evaluator.WithParam(nameof(compiler.src_namespace), this.TargetNamespace);
      }

      if (this.TargetClass != null) {
         evaluator.WithParam(nameof(compiler.src_class), this.TargetClass);
      }

      evaluator.WithParam(nameof(compiler.src_visibility),
         (this.TargetVisibility == CodeVisibility.Default ?
            "#default"
            : this.TargetVisibility.ToString().ToLowerInvariant()));

      evaluator.WithParam(
         nameof(compiler.src_base_types),
         _tbaseTypes?.Select(t => CodeTypeReference(t)).ToArray()
            ?? this.TargetBaseTypes?.Select(t => CodeTypeReference(t)).ToArray()
            ?? Array.Empty<XElement>()
      );

      evaluator.WithParam(nameof(compiler.cs_nullable_annotate), this.NullableAnnotate);

      if (this.NullableContext != null) {
         evaluator.WithParam(nameof(compiler.cs_nullable_context), this.NullableContext);
      }

      evaluator.WithParam(nameof(compiler.src_named_package), this.NamedPackage);

      if (this.UsePackageBase != null) {
         evaluator.WithParam(nameof(compiler.src_use_package_base), this.UsePackageBase);
      }

      evaluator.WithParam(nameof(compiler.src_module_resolver), moduleResolver);

      if (this.PackageTypeResolver != null) {
         evaluator.WithParam(nameof(compiler.src_package_type_resolver), this.PackageTypeResolver);
      }

      evaluator.WithParam(nameof(compiler.src_package_library), _packageLibrary);

      if (this.PackageLocationResolver != null) {
         evaluator.WithParam(nameof(compiler.src_package_location_resolver), this.PackageLocationResolver);
      }

      if (this.PackageFileDirectory != null) {
         evaluator.WithParam(nameof(compiler.src_package_file_directory), this.PackageFileDirectory);
      }

      if (this.PackageFileExtension != null) {
         evaluator.WithParam(nameof(compiler.src_package_file_extension), this.PackageFileExtension);
      }

      evaluator.WithParam(nameof(compiler.src_use_line_directive), this.UseLineDirective);

      if (this.NewLineChars != null) {
         evaluator.WithParam(nameof(compiler.src_new_line), this.NewLineChars);
      }

      if (this.IndentChars != null) {
         evaluator.WithParam(nameof(compiler.src_indent), this.IndentChars);
      }

      evaluator.WithParam(nameof(compiler.cs_open_brace_on_new_line), this.OpenBraceOnNewLine);
      evaluator.WithParam(nameof(compiler.src_extension_factories), _extensionFactories);
      evaluator.WithParam(nameof(compiler.xi_aware), this.XIncludeAware);

      var tmplEval = evaluator.ApplyTemplates(sourceDoc.Root!);

      foreach (var pair in _tunnelParams) {
         tmplEval.WithParam(pair.Key, pair.Value, tunnel: true);
      }

      _tunnelParams.Clear();

      return tmplEval;
   }

   XmlResolver
   GetModuleResolverOrDefault(XmlResolver? moduleResolver) =>
      moduleResolver ?? this.ModuleResolver ?? new XmlUrlResolver();

   internal static XElement
   CodeTypeReference(string typeName) {

      void writeFn(XmlWriter writer) {

         const string ns = XmlNamespaces.XcstCode;
         const string prefix = "code";

         writer.WriteStartElement(prefix, "type-reference", ns);
         writer.WriteAttributeString("name", typeName);
         writer.WriteEndElement();
      }

      return CodeTypeReferenceImpl(writeFn);
   }

   internal static XElement
   CodeTypeReference(Type type) =>
      CodeTypeReferenceImpl(w => new TypeManifestReader(w).WriteTypeReference(type));

   internal static XElement
   CodeTypeReferenceImpl(Action<XmlWriter> writeFn) {

      var typeRefDoc = new XDocument();

      using (var writer = typeRefDoc.CreateWriter()) {
         writeFn(writer);
      }

      var typeRef = typeRefDoc.Root!;
      typeRef.Remove();

      return typeRef;
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
   Language { get; init; }

   public IReadOnlyList<string>
   CompilationUnits { get; init; }

   public IReadOnlyList<XName>
   Templates { get; init; }
#pragma warning restore CS8618
}
