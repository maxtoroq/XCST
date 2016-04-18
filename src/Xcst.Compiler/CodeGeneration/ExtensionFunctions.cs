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
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
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

   class EscapeValueTemplateFunction : ExtensionFunctionDefinition {

      public override QName FunctionName { get; } = CompilerQName("escape-value-template");

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
         return new XdmSequenceType(XdmNodeKind.Document, ' ');
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

               throw new CompileException("Package type resolver returned null.",
                  errorCode: packageResolveError,
                  moduleUri: moduleUri,
                  lineNumber: lineNumber
               );
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

                  const string ns = XmlNamespaces.XcstSyntax;
                  const string prefix = "xcst";

                  Func<MethodBase, string> methodVisibility = m =>
                     (m.IsAbstract) ? "abstract"
                     : (m.IsVirtual) ? "public"
                     : "final";

                  Func<MemberInfo, string> memberVisibility = m =>
                     methodVisibility(m as MethodBase ?? ((PropertyInfo)m).GetGetMethod());

                  writer.WriteStartElement(prefix, "package-manifest", ns);
                  writer.WriteAttributeString("package-type", packageType.FullName);

                  foreach (MemberInfo member in packageType.GetMembers(BindingFlags.Instance | BindingFlags.Public)) {

                     XcstComponentAttribute attr = member.GetCustomAttribute<XcstComponentAttribute>(inherit: true);

                     if (attr == null) {
                        continue;
                     }

                     switch (attr.ComponentKind) {
                        case XcstComponentKind.AttributeSet:

                           writer.WriteStartElement(prefix, "attribute-set", ns);

                           if (String.IsNullOrEmpty(attr.Namespace)) {
                              writer.WriteAttributeString("name", attr.Name);
                           } else {
                              writer.WriteAttributeString("name", "ns1:" + attr.Name);
                              writer.WriteAttributeString("xmlns", "ns1", null, attr.Namespace);
                           }

                           writer.WriteAttributeString("visibility", memberVisibility(member));
                           writer.WriteAttributeString("member-name", member.Name);

                           break;

                        case XcstComponentKind.Function:

                           writer.WriteStartElement(prefix, "function", ns);
                           writer.WriteAttributeString("name", attr.Name ?? member.Name);
                           writer.WriteAttributeString("visibility", memberVisibility(member));
                           writer.WriteAttributeString("member-name", member.Name);

                           MethodInfo method = ((MethodInfo)member);

                           if (method.ReturnType != typeof(void)) {
                              writer.WriteAttributeString("as", TypeReferenceExpression(method.ReturnType));
                           }

                           foreach (ParameterInfo param in method.GetParameters()) {

                              writer.WriteStartElement(prefix, "param", ns);
                              writer.WriteAttributeString("name", param.Name);
                              writer.WriteAttributeString("as", TypeReferenceExpression(param.ParameterType));

                              if (param.IsOptional) {
                                 writer.WriteAttributeString("value", Constant(param.RawDefaultValue));
                              }

                              writer.WriteEndElement();
                           }

                           break;

                        case XcstComponentKind.Parameter:
                           writer.WriteStartElement(prefix, "param", ns);
                           writer.WriteAttributeString("name", attr.Name ?? member.Name);
                           writer.WriteAttributeString("as", TypeReferenceExpression(((PropertyInfo)member).PropertyType));
                           writer.WriteAttributeString("visibility", memberVisibility(member));
                           writer.WriteAttributeString("member-name", member.Name);
                           break;

                        case XcstComponentKind.Template:

                           writer.WriteStartElement(prefix, "template", ns);

                           if (String.IsNullOrEmpty(attr.Namespace)) {
                              writer.WriteAttributeString("name", attr.Name);
                           } else {
                              writer.WriteAttributeString("name", "ns1:" + attr.Name);
                              writer.WriteAttributeString("xmlns", "ns1", null, attr.Namespace);
                           }

                           writer.WriteAttributeString("visibility", memberVisibility(member));
                           writer.WriteAttributeString("member-name", member.Name);

                           foreach (var param in member.GetCustomAttributes<XcstTemplateParameterAttribute>(inherit: true)) {

                              writer.WriteStartElement(prefix, "param", ns);
                              writer.WriteAttributeString("name", param.Name);
                              writer.WriteAttributeString("required", param.Required.ToString().ToLowerInvariant());
                              writer.WriteAttributeString("tunnel", param.Tunnel.ToString().ToLowerInvariant());
                              writer.WriteEndElement();
                           }

                           break;

                        case XcstComponentKind.Type:

                           Type type = (Type)member;

                           writer.WriteStartElement(prefix, "type", ns);
                           writer.WriteAttributeString("name", attr.Name ?? member.Name);

                           writer.WriteAttributeString("visibility",
                              type.IsAbstract ? "abstract"
                              : type.IsSealed ? "final"
                              : "public");

                           break;

                        case XcstComponentKind.Variable:
                           writer.WriteStartElement(prefix, "variable", ns);
                           writer.WriteAttributeString("name", attr.Name ?? member.Name);
                           writer.WriteAttributeString("as", TypeReferenceExpression(((PropertyInfo)member).PropertyType));
                           writer.WriteAttributeString("visibility", memberVisibility(member));
                           writer.WriteAttributeString("member-name", member.Name);
                           break;
                     }

                     writer.WriteEndElement();
                  }

                  writer.WriteEndElement();
               }

               output.Position = 0;

               DocumentBuilder builder = this.processor.NewDocumentBuilder();
               builder.BaseUri = new Uri("", UriKind.Relative);

               XdmNode result = builder.Build(output);

               return result.GetXdmEnumerator();
            }
         }

         static string Constant(object value) {

            if (value == null) {
               return "null";
            }

            string str = Convert.ToString(value, CultureInfo.InvariantCulture);

            if (value is string) {
               return $"@\"{str.Replace("\"", "\"\"")}\"";
            }

            if (value is decimal) {
               return str + "m";
            }

            if (value is long) {
               return str + "L";
            }

            if (value is double) {
               return str + "d";
            }

            if (value is float) {
               return str + "f";
            }

            if (value is uint) {
               return str + "u";
            }

            if (value is ulong) {
               return str + "ul";
            }

            return str;
         }
      }
   }
}
