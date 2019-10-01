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
using System.Linq;
using System.Xml;
using Saxon.Api;

namespace Xcst.Compiler {

   public class XcstCompiler {

      readonly Lazy<XsltExecutable>
      compilerExec;

      readonly Processor
      processor;

      readonly Dictionary<QualifiedName, object>
      parameters = new Dictionary<QualifiedName, object>();

      Type[]
      tbaseTypes;

      public string
      TargetNamespace { get; set; }

      public string
      TargetClass { get; set; }

      public string
      TargetVisibility { get; set; }

      public string[]
      TargetBaseTypes { get; set; }

      public bool
      ClsCompliant { get; set; }

      public bool
      NamedPackage { get; set; }

      public string
      UsePackageBase { get; set; }

      public Func<string, Type>
      PackageTypeResolver { get; set; }

      public Func<string, Uri>
      PackageLocationResolver { get; set; }

      public string
      PackagesLocation { get; set; }

      public string
      PackageFileExtension { get; set; }

      public XmlResolver
      ModuleResolver { get; set; }

      public bool
      UseLineDirective { get; set; }

      public string
      NewLineChars { get; set; }

      public string
      IndentChars { get; set; }

      public bool
      OpenBraceOnNewLine { get; set; }

      internal
      XcstCompiler(Func<XsltExecutable> compilerExecFn, Processor processor) {

         if (compilerExecFn == null) throw new ArgumentNullException(nameof(compilerExecFn));

         this.compilerExec = new Lazy<XsltExecutable>(compilerExecFn);
         this.processor = processor;
      }

      public void
      SetTargetBaseTypes(params Type[] targetBaseTypes) {
         this.tbaseTypes = targetBaseTypes;
      }

      public void
      SetParameter(QualifiedName name, object value) {

         if (name == null) throw new ArgumentNullException(nameof(name));
         if (String.IsNullOrEmpty(name.Namespace)) throw new ArgumentException($"{nameof(name)} must be a qualified name.", nameof(name));

         this.parameters.Add(name, value);
      }

      public void
      SetParameter(QualifiedName name, Type value) {

         object objValue = null;

         if (value != null) {
            DocumentBuilder docBuilder = this.processor.NewDocumentBuilder();
            objValue = CodeTypeReference(value, docBuilder);
         }

         SetParameter(name, objValue);
      }

      public CompileResult
      Compile(Uri file) {

         if (file == null) throw new ArgumentNullException(nameof(file));

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
      Compile(Stream source, Uri baseUri = null) =>
         Compile(docb => docb.Build(source), baseUri);

      public CompileResult
      Compile(TextReader source, Uri baseUri = null) =>
         Compile(docb => docb.Build(source), baseUri);

      public CompileResult
      Compile(XmlReader source) =>
         Compile(docb => docb.Build(source));

      CompileResult
      Compile(Func<DocumentBuilder, XdmNode> buildFn, Uri baseUri = null) {

         var moduleResolver = GetModuleResolverOrDefault(this.ModuleResolver);
         var loggingResolver = new LoggingResolver(moduleResolver);

         DocumentBuilder docBuilder = this.processor.NewDocumentBuilder();
         docBuilder.XmlResolver = loggingResolver;

         if (baseUri != null) {
            docBuilder.BaseUri = baseUri;
         }

         XdmNode moduleDoc;

         try {
            moduleDoc = buildFn(docBuilder);

         } catch (net.sf.saxon.trans.XPathException ex) {

            var locator = ex.getLocator();

            QualifiedName errorCode = null;
            string errorLocal = ex.getErrorCodeLocalPart();

            if (!String.IsNullOrEmpty(errorLocal)) {
               errorCode = new QualifiedName(errorLocal, ex.getErrorCodeNamespace());
            }

            throw new CompileException(ex.Message,
               errorCode: errorCode,
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

            string moduleUri = errorData.Item1 ?? ex.ModuleUri;
            int lineNumber = errorData.Item2 ?? ex.LineNumber;

            throw new CompileException(ex.Message,
               errorCode: ex.ErrorCode?.ToQualifiedName(),
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
            CompilationUnits =
               ((IXdmEnumerator)docEl.EnumerateAxis(XdmAxis.Child, compiled.compilationUnit))
                  .AsNodes()
                  .Select(n => n.StringValue)
                  .ToArray(),
            Dependencies =
               new HashSet<Uri>(loggingResolver.ResolvedUris
                  .Concat(((IXdmEnumerator)docEl.EnumerateAxis(XdmAxis.Child, compiled.@ref))
                     .AsNodes()
                     .Select(n => new Uri(n.GetAttributeValue(compiled.href), UriKind.Absolute))
                  )
               ).ToArray(),
            Templates =
               ((IXdmEnumerator)((IXdmEnumerator)docEl.EnumerateAxis(XdmAxis.Child, grammar.packageManifest))
                  .AsNodes()
                  .Single()
                  .EnumerateAxis(XdmAxis.Child, grammar.template))
                  .AsNodes()
                  .Where(n => publicVisibility.Contains(n.GetAttributeValue(grammar.visibility)))
                  .Select(n => QualifiedName.Parse(n.GetAttributeValue(grammar.name)))
                  .ToArray()
         };

         return result;
      }

      XsltTransformer
      GetCompiler(XdmNode sourceDoc) {

         XsltTransformer compiler = this.compilerExec.Value.Load();
         compiler.InitialMode = CompilerQName("main");
         compiler.InitialContextNode = sourceDoc;

         foreach (var pair in this.parameters) {
            compiler.SetParameter(pair.Key.ToQName(), pair.Value.ToXdmValue());
         }

         if (this.TargetNamespace != null) {
            compiler.SetParameter(CompilerQName("namespace"), this.TargetNamespace.ToXdmItem());
         }

         if (this.TargetClass != null) {
            compiler.SetParameter(CompilerQName("class"), this.TargetClass.ToXdmItem());
         }

         if (this.TargetVisibility != null) {
            compiler.SetParameter(CompilerQName("visibility"), this.TargetVisibility.ToXdmItem());
         }

         DocumentBuilder baseTypesBuilder = this.processor.NewDocumentBuilder();

         compiler.SetParameter(
            CompilerQName("base-types"),
            new XdmValue(
               this.tbaseTypes?.Select(t => CodeTypeReference(t, baseTypesBuilder))
                  ?? this.TargetBaseTypes?.Select(t => CodeTypeReference(t, baseTypesBuilder))
                  ?? Enumerable.Empty<XdmNode>()
            )
         );

         compiler.SetParameter(CompilerQName("cls-compliant"), this.ClsCompliant.ToXdmValue());

         compiler.SetParameter(CompilerQName("use-line-directive"), this.UseLineDirective.ToXdmValue());

         if (this.NewLineChars != null) {
            compiler.SetParameter(CompilerQName("new-line"), this.NewLineChars.ToXdmItem());
         }

         if (this.IndentChars != null) {
            compiler.SetParameter(CompilerQName("indent"), this.IndentChars.ToXdmItem());
         }

         compiler.SetParameter(CompilerQName("open-brace-on-new-line"), this.OpenBraceOnNewLine.ToXdmItem());

         compiler.SetParameter(CompilerQName("named-package"), this.NamedPackage.ToXdmItem());

         if (this.UsePackageBase != null) {
            compiler.SetParameter(CompilerQName("use-package-base"), this.UsePackageBase.ToXdmItem());
         }

         if (this.PackageTypeResolver != null) {
            compiler.SetParameter(CompilerQName("package-type-resolver"), WrapExternalObject(this.PackageTypeResolver));
         }

         if (this.PackageLocationResolver != null) {
            compiler.SetParameter(CompilerQName("package-location-resolver"), WrapExternalObject(this.PackageLocationResolver));
         }

         if (this.PackagesLocation != null) {
            compiler.SetParameter(CompilerQName("packages-location"), this.PackagesLocation.ToXdmItem());
         }

         if (this.PackageFileExtension != null) {
            compiler.SetParameter(CompilerQName("package-file-extension"), this.PackageFileExtension.ToXdmItem());
         }

         if (this.ModuleResolver != null) {
            compiler.SetParameter(CompilerQName("module-resolver"), WrapExternalObject(this.ModuleResolver));
         }

         return compiler;
      }

      static XdmValue
      WrapExternalObject(object obj) =>
         new XdmExternalObjectValue(obj);

      internal static T
      UnwrapExternalObject<T>(XdmItem item) {

         object obj = ((XdmExternalObjectValue)item).GetExternalObject();

         // See <https://saxonica.plan.io/issues/3359>

         if (obj is net.sf.saxon.value.ObjectValue objValue) {
            obj = objValue.getObject();
         }

         return (T)obj;
      }

      internal static XmlResolver
      GetModuleResolverOrDefault(XmlResolver moduleResolver) =>
         moduleResolver ?? new XmlUrlResolver();

      internal static QName
      CompilerQName(string local) =>
         new QName(XmlNamespaces.XcstCompiled, local);

      internal static Tuple<string, int?>
      ModuleUriAndLineNumberFromErrorObject(XdmValue errorObject) {

         XdmAtomicValue[] values = errorObject
            .GetXdmEnumerator()
            .AsAtomicValues()
            .ToArray();

         string moduleUri = values
            .Select(x => x.ToString())
            .FirstOrDefault();

         int? lineNumber = values
            .Skip(1)
            .Select(x => (int?)(long)x.Value)
            .FirstOrDefault();

         return Tuple.Create(moduleUri, lineNumber);
      }

      internal static XdmNode
      CodeTypeReference(string typeName, DocumentBuilder docBuilder) {

         Action<XmlWriter> writeFn = writer => {

            const string ns = XmlNamespaces.XcstCode;
            const string prefix = "code";

            writer.WriteStartElement(prefix, "type-reference", ns);
            writer.WriteAttributeString("name", typeName);
            writer.WriteEndElement();
         };

         return CodeTypeReferenceImpl(writeFn, docBuilder);
      }

      internal static XdmNode
      CodeTypeReference(Type type, DocumentBuilder docBuilder) =>
         CodeTypeReferenceImpl(w => WriteTypeReference(type, w), docBuilder);

      internal static XdmNode
      CodeTypeReferenceImpl(Action<XmlWriter> writeFn, DocumentBuilder docBuilder) {

         using (var output = new MemoryStream()) {

            using (XmlWriter writer = XmlWriter.Create(output)) {
               writeFn(writer);
            }

            output.Position = 0;

            docBuilder.BaseUri = docBuilder.BaseUri ?? new Uri("", UriKind.Relative);

            return docBuilder.Build(output)
               .FirstElementOrSelf();
         }
      }

      internal static void
      WriteTypeReference(Type type, XmlWriter writer) {

         const string ns = XmlNamespaces.XcstCode;
         const string prefix = "code";

         writer.WriteStartElement(prefix, "type-reference", ns);

         if (type.IsArray) {

            writer.WriteAttributeString("array-dimensions", XmlConvert.ToString(type.GetArrayRank()));
            WriteTypeReference(type.GetElementType(), writer);

         } else {

            Type[] typeArguments = type.GetGenericArguments();

            string name = (typeArguments.Length > 0) ?
               type.Name.Substring(0, type.Name.IndexOf('`'))
               : type.Name;

            writer.WriteAttributeString("name", name);

            if (type.IsInterface) {
               writer.WriteAttributeString("interface", "true");
            }

            if (type.IsNested) {
               WriteTypeReference(type.DeclaringType, writer);
            } else {
               writer.WriteAttributeString("namespace", type.Namespace);
            }

            if (typeArguments.Length > 0) {

               writer.WriteStartElement(prefix, "type-arguments", ns);

               for (int i = 0; i < typeArguments.Length; i++) {
                  WriteTypeReference(typeArguments[i], writer);
               }

               writer.WriteEndElement();
            }
         }

         writer.WriteEndElement();
      }
   }

   /// <summary>
   /// The result of the <see cref="XcstCompiler.Compile"/> method.
   /// </summary>
   public class CompileResult {

      public string
      Language { get; internal set; }

      public IReadOnlyList<string>
      CompilationUnits { get; internal set; }

      public IReadOnlyList<Uri>
      Dependencies { get; internal set; }

      public IReadOnlyList<QualifiedName>
      Templates { get; internal set; }
   }
}
