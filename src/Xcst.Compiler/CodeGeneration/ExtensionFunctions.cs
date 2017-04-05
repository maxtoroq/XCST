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
using System.IO;
using System.Linq;
using System.Xml;
using Saxon.Api;
using static Xcst.Compiler.XcstCompiler;

namespace Xcst.Compiler.CodeGeneration {

   class LineNumberFunction : ExtensionFunctionDefinition {

      public override QName FunctionName { get; } = CompilerQName("line-number");

      public override XdmSequenceType[] ArgumentTypes { get; } = {
         new XdmSequenceType(XdmAnyNodeType.Instance, ' ')
      };

      public override int MinimumNumberOfArguments => ArgumentTypes.Length;

      public override int MaximumNumberOfArguments => MinimumNumberOfArguments;

      public override ExtensionFunctionCall MakeFunctionCall() {
         return new FunctionCall();
      }

      public override XdmSequenceType ResultType(XdmSequenceType[] ArgumentTypes) {
         return new XdmSequenceType(XdmAtomicType.BuiltInAtomicType(QName.XS_INTEGER), ' ');
      }

      class FunctionCall : ExtensionFunctionCall {

         public override IXdmEnumerator Call(IXdmEnumerator[] arguments, DynamicContext context) {

            XdmNode node = arguments[0].AsNodes().Single();

            return node.GetLineNumber()
               .ToXdmAtomicValue()
               .GetXdmEnumerator();
         }
      }
   }

   class LocalPathFunction : ExtensionFunctionDefinition {

      public override QName FunctionName { get; } = CompilerQName("local-path");

      public override XdmSequenceType[] ArgumentTypes { get; } = {
         new XdmSequenceType(XdmAtomicType.BuiltInAtomicType(QName.XS_ANYURI), ' ')
      };

      public override int MinimumNumberOfArguments => ArgumentTypes.Length;

      public override int MaximumNumberOfArguments => MinimumNumberOfArguments;

      public override ExtensionFunctionCall MakeFunctionCall() {
         return new FunctionCall();
      }

      public override XdmSequenceType ResultType(XdmSequenceType[] ArgumentTypes) {
         return new XdmSequenceType(XdmAtomicType.BuiltInAtomicType(QName.XS_STRING), ' ');
      }

      class FunctionCall : ExtensionFunctionCall {

         public override IXdmEnumerator Call(IXdmEnumerator[] arguments, DynamicContext context) {

            XdmAtomicValue value = arguments[0].AsAtomicValues().Single();

            Uri uri = value.Value as Uri
               ?? new Uri(value.ToString(), UriKind.RelativeOrAbsolute);

            return Helpers.LocalPath(uri)
               .ToXdmAtomicValue()
               .GetXdmEnumerator();
         }
      }
   }

   class InterpolatedStringFunction : ExtensionFunctionDefinition {

      public override QName FunctionName { get; } = CompilerQName("interpolated-string");

      public override XdmSequenceType[] ArgumentTypes { get; } = {
         new XdmSequenceType(XdmAtomicType.BuiltInAtomicType(QName.XS_STRING), ' ')
      };

      public override int MinimumNumberOfArguments => ArgumentTypes.Length;

      public override int MaximumNumberOfArguments => MinimumNumberOfArguments;

      public override ExtensionFunctionCall MakeFunctionCall() {
         return new FunctionCall();
      }

      public override XdmSequenceType ResultType(XdmSequenceType[] ArgumentTypes) {
         return new XdmSequenceType(XdmAtomicType.BuiltInAtomicType(QName.XS_STRING), ' ');
      }

      class FunctionCall : ExtensionFunctionCall {

         public override IXdmEnumerator Call(IXdmEnumerator[] arguments, DynamicContext context) {

            XdmAtomicValue value = arguments[0].AsAtomicValues().Single();

            return CSharpExpression.InterpolatedString(value.ToString())
               .ToXdmAtomicValue()
               .GetXdmEnumerator();
         }
      }
   }

   class MakeRelativeUriFunction : ExtensionFunctionDefinition {

      public override QName FunctionName { get; } = CompilerQName("make-relative-uri");

      public override XdmSequenceType[] ArgumentTypes { get; } = {
         new XdmSequenceType(XdmAtomicType.BuiltInAtomicType(QName.XS_ANYURI), ' '),
         new XdmSequenceType(XdmAtomicType.BuiltInAtomicType(QName.XS_ANYURI), ' ')
      };

      public override int MinimumNumberOfArguments => ArgumentTypes.Length;

      public override int MaximumNumberOfArguments => MinimumNumberOfArguments;

      public override ExtensionFunctionCall MakeFunctionCall() {
         return new FunctionCall();
      }

      public override XdmSequenceType ResultType(XdmSequenceType[] ArgumentTypes) {
         return new XdmSequenceType(XdmAtomicType.BuiltInAtomicType(QName.XS_ANYURI), ' ');
      }

      class FunctionCall : ExtensionFunctionCall {

         public override IXdmEnumerator Call(IXdmEnumerator[] arguments, DynamicContext context) {

            Uri[] uris = arguments.SelectMany(a => a.AsAtomicValues())
               .Select(a => (Uri)a.Value)
               .ToArray();

            return (IXdmEnumerator)new XdmAtomicValue(
               Helpers.MakeRelativeUri(uris[0], uris[1])
            ).GetEnumerator();
         }
      }
   }

   class DocWithUrisFunction : ExtensionFunctionDefinition {

      readonly Processor processor;

      public override QName FunctionName { get; } = CompilerQName("doc-with-uris");

      public override XdmSequenceType[] ArgumentTypes { get; } = {
         new XdmSequenceType(XdmAtomicType.BuiltInAtomicType(QName.XS_ANYURI), ' '),
         new XdmSequenceType(XdmAnyItemType.Instance, '+')
      };

      public override int MinimumNumberOfArguments => ArgumentTypes.Length;

      public override int MaximumNumberOfArguments => MinimumNumberOfArguments;

      public DocWithUrisFunction(Processor processor) {
         this.processor = processor;
      }

      public override ExtensionFunctionCall MakeFunctionCall() {
         return new FunctionCall(this.processor);
      }

      public override XdmSequenceType ResultType(XdmSequenceType[] ArgumentTypes) {
         return new XdmSequenceType(XdmAnyItemType.Instance, '+');
      }

      class FunctionCall : ExtensionFunctionCall {

         readonly Processor processor;

         public FunctionCall(Processor processor) {
            this.processor = processor;
         }

         public override IXdmEnumerator Call(IXdmEnumerator[] arguments, DynamicContext context) {

            XdmAtomicValue value = arguments[0].AsAtomicValues().Single();

            XdmValue errorObject = new XdmValue(arguments[1].AsItems());
            var errorData = ModuleUriAndLineNumberFromErrorObject(errorObject);
            string moduleUri = errorData.Item1;
            int lineNumber = errorData.Item2.GetValueOrDefault();

            Uri uri = (Uri)value.Value;

            if (!uri.IsAbsoluteUri) {
               throw new InvalidOperationException("Supplied URI must be absolute.");
            }

            var resolver = new LoggingResolver();

            DocumentBuilder docb = this.processor.NewDocumentBuilder();
            docb.XmlResolver = resolver;

            XdmNode doc;

            try {
               doc = docb.Build(uri);

            } catch (FileNotFoundException) {

               throw new CompileException("Could not retrieve imported module.",
                  errorCode: new QualifiedName("XTSE0165", XmlNamespaces.XcstErrors),
                  moduleUri: moduleUri,
                  lineNumber: lineNumber
               );

            } catch (net.sf.saxon.trans.XPathException ex) {

               var locator = ex.getLocator();

               QualifiedName errorCode = null;
               string errorLocal = ex.getErrorCodeLocalPart();

               if (!String.IsNullOrEmpty(errorLocal)) {
                  errorCode = new QualifiedName(errorLocal, ex.getErrorCodeNamespace());
               }

               throw new CompileException(ex.Message,
                  errorCode: errorCode,
                  moduleUri: locator?.getSystemId() ?? uri?.AbsoluteUri,
                  lineNumber: locator?.getLineNumber() ?? -1
               );
            }

            return doc.Append(new XdmValue(resolver.ResolvedUris.Select(u => u.ToXdmAtomicValue())))
               .GetXdmEnumerator();
         }
      }
   }

   class PackageManifestFunction : ExtensionFunctionDefinition {

      readonly XcstCompilerFactory compilerFactory;
      readonly Processor processor;

      public override QName FunctionName { get; } = CompilerQName("package-manifest");

      public override XdmSequenceType[] ArgumentTypes { get; } = {
         new XdmSequenceType(XdmAtomicType.BuiltInAtomicType(QName.XS_STRING), ' '),
         new XdmSequenceType(XdmAnyItemType.Instance, '+')
      };

      public override int MinimumNumberOfArguments => ArgumentTypes.Length;

      public override int MaximumNumberOfArguments => MinimumNumberOfArguments;

      public PackageManifestFunction(XcstCompilerFactory compilerFactory, Processor processor) {
         this.compilerFactory = compilerFactory;
         this.processor = processor;
      }

      public override ExtensionFunctionCall MakeFunctionCall() {
         return new FunctionCall(this.compilerFactory, this.processor);
      }

      public override XdmSequenceType ResultType(XdmSequenceType[] ArgumentTypes) {
         return new XdmSequenceType(XdmNodeKind.Document, '?');
      }

      class FunctionCall : ExtensionFunctionCall {

         readonly XcstCompilerFactory compilerFactory;
         readonly Processor processor;

         public FunctionCall(XcstCompilerFactory compilerFactory, Processor processor) {
            this.compilerFactory = compilerFactory;
            this.processor = processor;
         }

         public override IXdmEnumerator Call(IXdmEnumerator[] arguments, DynamicContext context) {

            string typeName = arguments[0].AsAtomicValues().Single().ToString();

            XdmValue errorObject = new XdmValue(arguments[1].AsItems());
            var errorData = ModuleUriAndLineNumberFromErrorObject(errorObject);
            string moduleUri = errorData.Item1;
            int lineNumber = errorData.Item2.GetValueOrDefault();

            Type packageType;
            var packageResolveError = new QualifiedName("XTSE3000", XmlNamespaces.XcstErrors);

            try {
               packageType = this.compilerFactory.PackageTypeResolver(typeName);

            } catch (Exception ex) {

               throw new CompileException(ex.Message,
                  errorCode: packageResolveError,
                  moduleUri: moduleUri,
                  lineNumber: lineNumber
               );
            }

            if (packageType == null) {
               return EmptyEnumerator.INSTANCE;
            }

            if (!typeof(IXcstPackage).IsAssignableFrom(packageType)) {

               throw new CompileException($"{packageType.FullName} is not a valid XCST package.",
                  errorCode: packageResolveError,
                  moduleUri: moduleUri,
                  lineNumber: lineNumber
               );
            }

            using (var output = new MemoryStream()) {

               using (XmlWriter writer = XmlWriter.Create(output)) {
                  PackageManifest.WriteManifest(packageType, writer);
               }

               output.Position = 0;

               DocumentBuilder builder = this.processor.NewDocumentBuilder();
               builder.BaseUri = new Uri("", UriKind.Relative);

               XdmNode result = builder.Build(output);

               return result.GetXdmEnumerator();
            }
         }
      }
   }

   class PackageLocationFunction : ExtensionFunctionDefinition {

      readonly XcstCompilerFactory compilerFactory;

      public override QName FunctionName { get; } = CompilerQName("package-location");

      public override XdmSequenceType[] ArgumentTypes { get; } = {
         new XdmSequenceType(XdmAtomicType.BuiltInAtomicType(QName.XS_STRING), ' ')
      };

      public override int MinimumNumberOfArguments => ArgumentTypes.Length;

      public override int MaximumNumberOfArguments => MinimumNumberOfArguments;

      public PackageLocationFunction(XcstCompilerFactory compilerFactory) {
         this.compilerFactory = compilerFactory;
      }

      public override ExtensionFunctionCall MakeFunctionCall() {
         return new FunctionCall(this.compilerFactory);
      }

      public override XdmSequenceType ResultType(XdmSequenceType[] ArgumentTypes) {
         return new XdmSequenceType(XdmAtomicType.BuiltInAtomicType(QName.XS_ANYURI), '?');
      }

      class FunctionCall : ExtensionFunctionCall {

         readonly XcstCompilerFactory compilerFactory;

         public FunctionCall(XcstCompilerFactory compilerFactory) {
            this.compilerFactory = compilerFactory;
         }

         public override IXdmEnumerator Call(IXdmEnumerator[] arguments, DynamicContext context) {

            string packageName = arguments[0].AsAtomicValues().Single().ToString();

            Uri packageLocation = this.compilerFactory.PackageLocationResolver?.Invoke(packageName);

            return packageLocation?.ToXdmAtomicValue()
               .GetXdmEnumerator()
               ?? EmptyEnumerator.INSTANCE;
         }
      }
   }

   class QNameIdFunction : ExtensionFunctionDefinition {

      public override QName FunctionName { get; } = CompilerQName("qname-id");

      public override XdmSequenceType[] ArgumentTypes { get; } = {
         new XdmSequenceType(XdmAtomicType.BuiltInAtomicType(QName.XS_QNAME), ' ')
      };

      public override int MinimumNumberOfArguments => ArgumentTypes.Length;

      public override int MaximumNumberOfArguments => MinimumNumberOfArguments;

      public override ExtensionFunctionCall MakeFunctionCall() {
         return new FunctionCall();
      }

      public override XdmSequenceType ResultType(XdmSequenceType[] ArgumentTypes) {
         return new XdmSequenceType(XdmAtomicType.BuiltInAtomicType(QName.XS_INTEGER), ' ');
      }

      class FunctionCall : ExtensionFunctionCall {

         public override IXdmEnumerator Call(IXdmEnumerator[] arguments, DynamicContext context) {

            QualifiedName qname = ((QName)arguments[0].AsAtomicValues().Single().Value).ToQualifiedName();

            return Helpers.QNameId(qname)
               .ToXdmAtomicValue()
               .GetXdmEnumerator();
         }
      }
   }
}
