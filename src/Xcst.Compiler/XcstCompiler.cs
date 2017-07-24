﻿// Copyright 2015 Max Toro Q.
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
using Xcst.Compiler.CodeGeneration;

namespace Xcst.Compiler {

   public class XcstCompiler {

      readonly Lazy<XsltExecutable> compilerExec;
      readonly Processor processor;
      readonly IDictionary<QualifiedName, object> parameters = new Dictionary<QualifiedName, object>();

      public string TargetNamespace { get; set; }

      public string TargetClass { get; set; }

      public string[] TargetBaseTypes { get; set; }

      public bool UseLineDirective { get; set; }

      public string NewLineChars { get; set; }

      public string IndentChars { get; set; }

      public bool OpenBraceOnNewLine { get; set; }

      public bool LibraryPackage { get; set; }

      public string UsePackageBase { get; set; }

      internal XcstCompiler(Func<XsltExecutable> compilerExecFn, Processor processor) {

         if (compilerExecFn == null) throw new ArgumentNullException(nameof(compilerExecFn));

         this.compilerExec = new Lazy<XsltExecutable>(compilerExecFn);
         this.processor = processor;
      }

      public void SetTargetBaseTypes(params Type[] targetBaseTypes) {

         this.TargetBaseTypes = targetBaseTypes?
            .Where(t => t != null)
            .Select(t => CSharpExpression.TypeReference(t))
            .ToArray();
      }

      public void SetParameter(QualifiedName name, object value) {

         if (name == null) throw new ArgumentNullException(nameof(name));
         if (String.IsNullOrEmpty(name.Namespace)) throw new ArgumentException($"{nameof(name)} must be a qualified name.", nameof(name));

         this.parameters.Add(name, value);
      }

      public CompileResult Compile(Uri file) {

         if (file == null) throw new ArgumentNullException(nameof(file));
         if (!file.IsAbsoluteUri) throw new ArgumentException("file must be an absolute URI.", nameof(file));
         if (!file.IsFile) throw new ArgumentException("file must be a file URI", nameof(file));

         using (var source = File.OpenRead(file.LocalPath)) {
            return Compile(source, file);
         }
      }

      public CompileResult Compile(Stream source, Uri baseUri = null) {
         return Compile(docb => docb.Build(source), baseUri);
      }

      public CompileResult Compile(TextReader source, Uri baseUri = null) {
         return Compile(docb => docb.Build(source), baseUri);
      }

      public CompileResult Compile(XmlReader source) {
         return Compile(docb => docb.Build(source));
      }

      CompileResult Compile(Func<DocumentBuilder, XdmNode> buildFn, Uri baseUri = null) {

         var resolver = new LoggingResolver();

         DocumentBuilder docBuilder = this.processor.NewDocumentBuilder();
         docBuilder.XmlResolver = resolver;

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

         var syntax = new {
            packageManifest = new QName(XmlNamespaces.XcstSyntax, "package-manifest"),
            template = new QName(XmlNamespaces.XcstSyntax, "template"),
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
               new HashSet<Uri>(resolver.ResolvedUris
                  .Concat(((IXdmEnumerator)docEl.EnumerateAxis(XdmAxis.Child, compiled.@ref))
                     .AsNodes()
                     .Select(n => new Uri(n.GetAttributeValue(compiled.href), UriKind.Absolute))
                  )
               ).ToArray(),
            Templates =
               ((IXdmEnumerator)((IXdmEnumerator)docEl.EnumerateAxis(XdmAxis.Child, syntax.packageManifest))
                  .AsNodes()
                  .Single()
                  .EnumerateAxis(XdmAxis.Child, syntax.template))
                  .AsNodes()
                  .Where(n => publicVisibility.Contains(n.GetAttributeValue(syntax.visibility)))
                  .Select(n => QualifiedName.Parse(n.GetAttributeValue(syntax.name)))
                  .ToArray()
         };

         return result;
      }

      XsltTransformer GetCompiler(XdmNode sourceDoc) {

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

         compiler.SetParameter(CompilerQName("base-types"), this.TargetBaseTypes.ToXdmValue());

         compiler.SetParameter(CompilerQName("use-line-directive"), this.UseLineDirective.ToXdmValue());

         if (this.NewLineChars != null) {
            compiler.SetParameter(CompilerQName("new-line"), this.NewLineChars.ToXdmItem());
         }

         if (this.IndentChars != null) {
            compiler.SetParameter(CompilerQName("indent"), this.IndentChars.ToXdmItem());
         }

         compiler.SetParameter(CompilerQName("open-brace-on-new-line"), this.OpenBraceOnNewLine.ToXdmItem());

         compiler.SetParameter(CompilerQName("library-package"), this.LibraryPackage.ToXdmItem());

         if (this.UsePackageBase != null) {
            compiler.SetParameter(CompilerQName("use-package-base"), this.UsePackageBase.ToXdmItem());
         }

         return compiler;
      }

      internal static QName CompilerQName(string local) {
         return new QName(XmlNamespaces.XcstCompiled, local);
      }

      internal static Tuple<string, int?> ModuleUriAndLineNumberFromErrorObject(XdmValue errorObject) {

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
   }

   /// <summary>
   /// The result of the <see cref="XcstCompiler.Compile"/> method.
   /// </summary>

   public class CompileResult {

      public string Language { get; internal set; }

      public IReadOnlyList<string> CompilationUnits { get; internal set; }

      public IReadOnlyList<Uri> Dependencies { get; internal set; }

      public IReadOnlyList<QualifiedName> Templates { get; internal set; }
   }
}
