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
using System.ComponentModel;
using System.IO;
using System.Xml;
using Xcst.PackageModel;

namespace Xcst {

   public class XcstEvaluator {

      static readonly QualifiedName InitialTemplate = new QualifiedName("initial-template", XmlNamespaces.Xcst);

      readonly IXcstPackage package;
      readonly IDictionary<string, object> parameters = new Dictionary<string, object>();
      bool paramsLocked = false;
      PrimingContext primingContext;

      public static XcstEvaluator Using(object package) {

         if (package == null) throw new ArgumentNullException(nameof(package));

         var pkg = package as IXcstPackage;

         if (pkg == null) {
            throw new ArgumentException("Provided instance is not a valid XCST package.", nameof(package));
         }

         return new XcstEvaluator(pkg);
      }

      private XcstEvaluator(IXcstPackage package) {

         if (package == null) throw new ArgumentNullException(nameof(package));

         this.package = package;
      }

      public XcstEvaluator WithParam(string name, object value) {

         if (name == null) throw new ArgumentNullException(nameof(name));

         // since there's no way to un-prime, a second prime would still use the values
         // of the first prime (for parameters not specified in the second prime)
         // the workaround is to simply create a new evaluator

         if (this.paramsLocked) {
            throw new InvalidOperationException($"Cannot modify parameters, use a new {nameof(XcstEvaluator)} object.");
         }

         this.parameters[name] = value;

         return this;
      }

      public XcstEvaluator WithParams(object parameters) {

         if (parameters != null) {
            WithParams(ObjectToDictionary(parameters));
         }

         return this;
      }

      public XcstEvaluator WithParams(IDictionary<string, object> parameters) {

         if (parameters != null) {

            foreach (var pair in parameters) {
               WithParam(pair.Key, pair.Value);
            }
         }

         return this;
      }

      internal static IDictionary<string, object> ObjectToDictionary(object values) {

         IDictionary<string, object> dict = null;

         if (values != null) {

            dict = values as IDictionary<string, object>;

            if (dict != null) {
               return dict;
            }
         }

         if (dict == null) {
            dict = new Dictionary<string, object>();
         }

         if (values != null) {

            PropertyDescriptorCollection props = TypeDescriptor.GetProperties(values);

            foreach (PropertyDescriptor prop in props) {
               object val = prop.GetValue(values);
               dict.Add(prop.Name, val);
            }
         }

         return dict;
      }

      public XcstTemplateEvaluator CallInitialTemplate() {
         return CallTemplate(InitialTemplate);
      }

      public XcstTemplateEvaluator CallTemplate(string name) {

         if (name == null) throw new ArgumentNullException(nameof(name));

         return CallTemplate(new QualifiedName(name));
      }

      public XcstTemplateEvaluator CallTemplate(QualifiedName name) {

         if (name == null) throw new ArgumentNullException(nameof(name));

         this.paramsLocked = true;

         return new XcstTemplateEvaluator(this.package, Prime, name);
      }

      PrimingContext Prime() {

         if (this.primingContext == null) {

            this.primingContext = new PrimingContext();

            foreach (var param in this.parameters) {
               this.primingContext.WithParam(param.Key, param.Value);
            }

            this.parameters.Clear();
            this.package.Prime(this.primingContext, null);
         }

         return this.primingContext;
      }
   }

   public class XcstTemplateEvaluator {

      readonly IXcstPackage package;
      readonly Func<PrimingContext> primeFn;
      readonly QualifiedName name;
      readonly IDictionary<string, object> templateParameters = new Dictionary<string, object>();
      readonly IDictionary<string, object> tunnelParameters = new Dictionary<string, object>();

      internal XcstTemplateEvaluator(IXcstPackage package, Func<PrimingContext> primeFn, QualifiedName name) {

         if (package == null) throw new ArgumentNullException(nameof(package));
         if (primeFn == null) throw new ArgumentNullException(nameof(primeFn));
         if (name == null) throw new ArgumentNullException(nameof(name));

         this.package = package;
         this.primeFn = primeFn;
         this.name = name;
      }

      public XcstTemplateEvaluator WithParam(string name, object value, bool tunnel = false) {

         if (name == null) throw new ArgumentNullException(nameof(name));

         if (tunnel) {
            this.tunnelParameters[name] = value;
         } else {
            this.templateParameters[name] = value;
         }

         return this;
      }

      public XcstTemplateEvaluator WithParams(object parameters) {

         if (parameters != null) {
            WithParams(XcstEvaluator.ObjectToDictionary(parameters));
         }

         return this;
      }

      public XcstTemplateEvaluator WithParams(IDictionary<string, object> parameters) {

         if (parameters != null) {

            foreach (var pair in parameters) {
               WithParam(pair.Key, pair.Value);
            }
         }

         return this;
      }

      public XcstTemplateEvaluator WithTunnelParams(object parameters) {

         if (parameters != null) {
            WithTunnelParams(XcstEvaluator.ObjectToDictionary(parameters));
         }

         return this;
      }

      public XcstTemplateEvaluator WithTunnelParams(IDictionary<string, object> parameters) {

         if (parameters != null) {

            foreach (var pair in parameters) {
               WithParam(pair.Key, pair.Value, tunnel: true);
            }
         }

         return this;
      }

      public XcstTemplateEvaluator ClearParams() {

         this.templateParameters.Clear();
         this.tunnelParameters.Clear();
         return this;
      }

      public XcstOutputter OutputTo(Uri file) {

         if (file == null) throw new ArgumentNullException(nameof(file));
         if (!file.IsAbsoluteUri) throw new ArgumentException("file must be an absolute URI.", nameof(file));
         if (!file.IsFile) throw new ArgumentException("file must be a file URI", nameof(file));

         return CreateOutputter(WriterFactory.CreateWriter(file));
      }

      public XcstOutputter OutputTo(Stream output, Uri outputUri = null) {

         if (output == null) throw new ArgumentNullException(nameof(output));

         return CreateOutputter(WriterFactory.CreateWriter(output, outputUri));
      }

      public XcstOutputter OutputTo(TextWriter output, Uri outputUri = null) {

         if (output == null) throw new ArgumentNullException(nameof(output));

         return CreateOutputter(WriterFactory.CreateWriter(output, outputUri));
      }

      public XcstOutputter OutputTo(XmlWriter output, Uri outputUri = null) {

         if (output == null) throw new ArgumentNullException(nameof(output));

         return CreateOutputter(WriterFactory.CreateWriter(output, outputUri));
      }

      public XcstOutputter OutputTo(XcstWriter output) {

         if (output == null) throw new ArgumentNullException(nameof(output));

         return CreateOutputter(WriterFactory.CreateWriter(output));
      }

      /// <exclude/>

      [EditorBrowsable(EditorBrowsableState.Never)]
      public XcstOutputter OutputTo<TBase>(ISequenceWriter<TBase> output) {

         if (output == null) throw new ArgumentNullException(nameof(output));

         Action<TemplateContext> tmplFn = this.package.GetTypedTemplate<TBase>(this.name, output);

         // it's important to keep template parameters' variables outside the execution delegate
         // so subsequent modifications do not affect previously created outputters

         var templateParams = new Dictionary<string, object>(this.templateParameters);
         var tunnelParams = new Dictionary<string, object>(this.tunnelParameters);

         Action executionFn = () => tmplFn(CreateTemplateContext(templateParams, tunnelParams));

         return new XcstOutputter(this.package, this.primeFn, executionFn);
      }

      XcstOutputter CreateOutputter(CreateWriterDelegate writerFn) {

         // it's important to keep template parameters' variables outside the execution delegate
         // so subsequent modifications do not affect previously created outputters

         var templateParams = new Dictionary<string, object>(this.templateParameters);
         var tunnelParams = new Dictionary<string, object>(this.tunnelParameters);

         Action<IXcstPackage, XcstWriter> executionFn =
            (p, o) => p.GetTypedTemplate(this.name, o)(CreateTemplateContext(templateParams, tunnelParams));

         return new XcstOutputter(this.package, this.primeFn, writerFn, executionFn);
      }

      static TemplateContext CreateTemplateContext(IDictionary<string, object> templateParams, IDictionary<string, object> tunnelParams) {

         var context = new TemplateContext();

         foreach (var param in templateParams) {
            context.WithParam(param.Key, param.Value);
         }

         foreach (var param in tunnelParams) {
            context.WithParam(param.Key, param.Value, tunnel: true);
         }

         return context;
      }
   }

   public class XcstOutputter {

      readonly IXcstPackage package;
      readonly Func<PrimingContext> primeFn;
      readonly CreateWriterDelegate writerFn;
      readonly Action<IXcstPackage, XcstWriter> executionFn;
      readonly Action typedExecutionFn;

      OutputParameters parameters;
      IFormatProvider formatProvider;

      internal XcstOutputter(IXcstPackage package, Func<PrimingContext> primeFn, CreateWriterDelegate writerFn, Action<IXcstPackage, XcstWriter> executionFn) {

         if (package == null) throw new ArgumentNullException(nameof(package));
         if (primeFn == null) throw new ArgumentNullException(nameof(primeFn));
         if (writerFn == null) throw new ArgumentNullException(nameof(writerFn));
         if (executionFn == null) throw new ArgumentNullException(nameof(executionFn));

         this.package = package;
         this.primeFn = primeFn;
         this.writerFn = writerFn;
         this.executionFn = executionFn;
      }

      internal XcstOutputter(IXcstPackage package, Func<PrimingContext> primeFn, Action typedExecutionFn) {

         if (package == null) throw new ArgumentNullException(nameof(package));
         if (primeFn == null) throw new ArgumentNullException(nameof(primeFn));
         if (typedExecutionFn == null) throw new ArgumentNullException(nameof(typedExecutionFn));

         this.package = package;
         this.primeFn = primeFn;
         this.typedExecutionFn = typedExecutionFn;
      }

      public XcstOutputter WithParams(OutputParameters parameters) {

         this.parameters = parameters;
         return this;
      }

      public XcstOutputter WithFormatProvider(IFormatProvider formatProvider) {

         this.formatProvider = formatProvider;
         return this;
      }

      public void Run(bool skipFlush = false) {

         PrimingContext primingContext = this.primeFn();

         var execContext = new ExecutionContext(this.package, primingContext, this.formatProvider);

         this.package.Context = execContext;

         if (this.typedExecutionFn != null) {

            this.typedExecutionFn();

         } else {

            var defaultParams = new OutputParameters();
            this.package.ReadOutputDefinition(null, defaultParams);

            RuntimeWriter writer = this.writerFn(defaultParams, this.parameters, execContext);

            try {
               this.executionFn(this.package, writer);

               if (!writer.DisposeWriter
                  && !skipFlush) {

                  writer.Flush();
               }

            } finally {

               if (writer.DisposeWriter) {
                  writer.Dispose();
               }
            }
         }
      }
   }
}