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
using System.Text;
using System.Xml;
using Saxon.Api;

namespace Xcst.Compiler {

   public class XcstCompiler {

      readonly Lazy<XsltExecutable> compilerExec;
      readonly Processor processor;
      readonly IDictionary<QualifiedName, object> parameters = new Dictionary<QualifiedName, object>();

      public string TargetNamespace { get; set; }

      public string TargetClass { get; set; }

      public string[] TargetBaseTypes { get; set; }

      public bool OmitAssertions { get; set; }

      public string AlternateFirstBaseType { get; set; }

      public string AlternateFirstBaseTypeIfExistsType { get; set; }

      public bool UseLineDirective { get; set; }

      public string NewLineChars { get; set; }

      public string IndentChars { get; set; }

      public bool OpenBraceOnNewLine { get; set; }

      internal XcstCompiler(Func<XsltExecutable> compilerExecFn, Processor processor) {

         if (compilerExecFn == null) throw new ArgumentNullException(nameof(compilerExecFn));

         this.compilerExec = new Lazy<XsltExecutable>(compilerExecFn);
         this.processor = processor;
      }

      public void SetTargetBaseTypes(params Type[] targetBaseTypes) {

         this.TargetBaseTypes = targetBaseTypes?
            .Where(t => t != null)
            .Select(t => TypeToCSharp(t, fullName: true))
            .ToArray();
      }

      static string TypeToCSharp(Type type, bool fullName) {

         var sb = new StringBuilder();
         return TypeToCSharp(type, fullName, sb);
      }

      static string TypeToCSharp(Type type, bool fullName, StringBuilder sb) {

         if (fullName) {
            sb.Append(type.Namespace);
            sb.Append(".");
         }

         sb.Append(type.IsGenericType ? type.Name.Substring(0, type.Name.IndexOf('`')) : type.Name);

         if (type.IsGenericType) {

            sb.Append("<");

            foreach (var typeParam in type.GetGenericArguments()) {
               sb.Append(TypeToCSharp(typeParam, fullName, sb));
            }

            sb.Append(">");
         }

         return sb.ToString();
      }

      public void SetParameter(QualifiedName name, object value) {

         if (name == null) throw new ArgumentNullException(nameof(name));
         if (String.IsNullOrEmpty(name.Namespace)) throw new ArgumentException($"{nameof(name)} must be a qualified name.", nameof(name));

         this.parameters.Add(name, value);
      }

      public CompileResult Compile(Stream module, Uri baseUri = null) {
         return Compile(docb => docb.Build(module), baseUri);
      }

      public CompileResult Compile(TextReader module, Uri baseUri = null) {
         return Compile(docb => docb.Build(module), baseUri);
      }

      public CompileResult Compile(XmlReader module) {
         return Compile(docb => docb.Build(module));
      }

      CompileResult Compile(Func<DocumentBuilder, XdmNode> buildFn, Uri baseUri = null) {

         CheckRequiredParams();

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

            string moduleUri = errorObject.GetXdmEnumerator()
               .AsAtomicValues()
               .Select(x => x.ToString())
               .DefaultIfEmpty(ex.ModuleUri)
               .FirstOrDefault();

            int lineNumber = errorObject.GetXdmEnumerator()
               .AsAtomicValues()
               .Skip(1)
               .Select(x => (int)(long)x.Value)
               .DefaultIfEmpty(ex.LineNumber)
               .FirstOrDefault();

            throw new CompileException(ex.Message,
               errorCode: ex.ErrorCode?.ToQualifiedName(),
               moduleUri: moduleUri,
               lineNumber: lineNumber
            );
         }

         XdmNode docEl = destination.XdmNode.FirstElementOrSelf();

         QName languageName = new QName("language"),
            hrefName = new QName("href");

         var result = new CompileResult {
            Language = docEl.GetAttributeValue(languageName),
            CompilationUnits =
               ((IXdmEnumerator)docEl.EnumerateAxis(XdmAxis.Child, CompilerQName("compilation-unit")))
                  .AsNodes()
                  .Select(n => n.StringValue)
                  .ToArray(),
            References =
               new HashSet<Uri>(resolver.ResolvedUris
                  .Concat(((IXdmEnumerator)docEl.EnumerateAxis(XdmAxis.Child, CompilerQName("ref")))
                     .AsNodes()
                     .Select(n => new Uri(n.GetAttributeValue(hrefName), UriKind.Absolute))
                  )
               ).ToArray()
         };

         return result;
      }

      void CheckRequiredParams() {

         if (String.IsNullOrEmpty(this.TargetNamespace)) {
            throw new InvalidOperationException($"Must set {nameof(this.TargetNamespace)} first.");
         }

         if (String.IsNullOrEmpty(this.TargetClass)) {
            throw new InvalidOperationException($"Must set {nameof(this.TargetClass)} first.");
         }
      }

      XsltTransformer GetCompiler(XdmNode moduleDoc) {

         XsltTransformer compiler = this.compilerExec.Value.Load();
         compiler.InitialMode = CompilerQName("main");
         compiler.InitialContextNode = moduleDoc;

         foreach (var pair in this.parameters) {
            compiler.SetParameter(pair.Key.ToQName(), pair.Value.ToXdmValue());
         }

         compiler.SetParameter(CompilerQName("namespace"), this.TargetNamespace.ToXdmItem());
         compiler.SetParameter(CompilerQName("class"), this.TargetClass.ToXdmItem());
         compiler.SetParameter(CompilerQName("base-types"), this.TargetBaseTypes.ToXdmValue());
         compiler.SetParameter(CompilerQName("omit-assertions"), this.OmitAssertions.ToXdmValue());

         if (this.AlternateFirstBaseType != null) {
            compiler.SetParameter(CompilerQName("alternate-first-base-type"), this.AlternateFirstBaseType.ToXdmValue());
         }

         if (this.AlternateFirstBaseTypeIfExistsType != null) {
            compiler.SetParameter(CompilerQName("alternate-first-base-type-if-exists-type"), this.AlternateFirstBaseTypeIfExistsType.ToXdmValue());
         }

         compiler.SetParameter(CompilerQName("use-line-directive"), this.UseLineDirective.ToXdmValue());

         if (this.NewLineChars != null) {
            compiler.SetParameter(CompilerQName("new-line"), this.NewLineChars.ToXdmValue());
         }

         if (this.IndentChars != null) {
            compiler.SetParameter(CompilerQName("indent"), this.IndentChars.ToXdmValue());
         }

         compiler.SetParameter(CompilerQName("open-brace-on-new-line"), this.OpenBraceOnNewLine.ToXdmValue());

         return compiler;
      }

      internal static QName CompilerQName(string local) {
         return new QName(XmlNamespaces.XcstCompiled, local);
      }
   }

   /// <summary>
   /// The result of the <see cref="XcstCompiler.Compile"/> method.
   /// </summary>
   public class CompileResult {

      public string Language { get; internal set; }

      public IReadOnlyList<string> CompilationUnits { get; internal set; }

      public IReadOnlyList<Uri> References { get; internal set; }
   }
}
