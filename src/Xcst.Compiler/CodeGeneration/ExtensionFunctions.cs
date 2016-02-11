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
using Saxon.Api;
using static Xcst.Compiler.XcstCompiler;

namespace Xcst.Compiler.CodeGeneration {

   class LineNumberFunction : ExtensionFunctionDefinition {

      public override QName FunctionName { get; } = CompilerQName("line-number");

      public override XdmSequenceType[] ArgumentTypes { get; } = {
         new XdmSequenceType(XdmAnyNodeType.Instance, ' ')
      };

      public override int MinimumNumberOfArguments => 1;

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

      public override int MinimumNumberOfArguments => 1;

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

   class EscapeValueTemplateFunction : ExtensionFunctionDefinition {

      public override QName FunctionName { get; } = CompilerQName("escape-value-template");

      public override XdmSequenceType[] ArgumentTypes { get; } = {
         new XdmSequenceType(XdmAtomicType.BuiltInAtomicType(QName.XS_STRING), ' ')
      };

      public override int MinimumNumberOfArguments => 1;

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

            return ValueTemplateEscaper.EscapeValueTemplate(value.ToString())
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

      public override int MinimumNumberOfArguments => 2;

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
         new XdmSequenceType(XdmAtomicType.BuiltInAtomicType(QName.XS_ANYURI), ' ')
      };

      public override int MinimumNumberOfArguments => 1;

      public override int MaximumNumberOfArguments => MinimumNumberOfArguments;

      public DocWithUrisFunction(Processor processor) {
         this.processor = processor;
      }

      public override ExtensionFunctionCall MakeFunctionCall() {
         return new FunctionCall(this.processor);
      }

      public override XdmSequenceType ResultType(XdmSequenceType[] ArgumentTypes) {
         return new XdmSequenceType(XdmAnyItemType.Instance, '*');
      }

      class FunctionCall : ExtensionFunctionCall {

         readonly Processor processor;

         public FunctionCall(Processor processor) {
            this.processor = processor;
         }

         public override IXdmEnumerator Call(IXdmEnumerator[] arguments, DynamicContext context) {

            XdmAtomicValue value = arguments[0].AsAtomicValues().Single();

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

               return XdmEmptySequence.INSTANCE
                  .GetXdmEnumerator();

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
}
