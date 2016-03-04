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
using Xcst.Runtime;

namespace Xcst {

   public class XcstEvaluator {

      static readonly QualifiedName InitialTemplate = new QualifiedName("initial-template", XmlNamespaces.Xcst);

      readonly IXcstPackage package;
      readonly IDictionary<string, object> parameters = new Dictionary<string, object>();
      bool primed = false;

      public static XcstEvaluator Using<TPackage>() where TPackage : new() {
         return Using(new TPackage());
      }

      public static XcstEvaluator Using(Type packageType) {
         return Using(Activator.CreateInstance(packageType));
      }

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

         if (this.primed) {
            throw new InvalidOperationException("Already primed.");
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

         Prime();

         return new XcstTemplateEvaluator(this.package, name);
      }

      void Prime() {

         if (this.primed) {
            return;
         }

         var context = new PrimingContext();

         foreach (var param in this.parameters) {
            context.WithParam(param.Key, param.Value);
         }

         this.parameters.Clear();
         this.package.Prime(context);
         this.primed = true;
      }
   }

   public class XcstTemplateEvaluator {

      static readonly Uri DefaultOuputUri = new Uri("", UriKind.Relative);

      readonly IXcstPackage package;
      readonly QualifiedName name;
      readonly IDictionary<string, ParameterArgument> parameters = new Dictionary<string, ParameterArgument>();

      internal XcstTemplateEvaluator(IXcstPackage package, QualifiedName name) {

         if (package == null) throw new ArgumentNullException(nameof(package));
         if (name == null) throw new ArgumentNullException(nameof(name));

         this.package = package;
         this.name = name;
      }

      public XcstTemplateEvaluator WithParam(string name, object value, bool tunnel = false) {

         if (name == null) throw new ArgumentNullException(nameof(name));

         this.parameters[name] = new ParameterArgument(value, tunnel);
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

      public XcstTemplateEvaluator ClearParams() {

         this.parameters.Clear();
         return this;
      }

      public XcstOutputter OutputTo(Uri file) {

         if (file == null) throw new ArgumentNullException(nameof(file));
         if (!file.IsAbsoluteUri) throw new ArgumentException("file must be an absolute URI.", nameof(file));
         if (!file.IsFile) throw new ArgumentException("file must be a file URI", nameof(file));

         Func<OutputParameters, IWriterFactory> writerFn = @params =>
            WriterFactory.CreateFactory(file, @params);

         return new XcstOutputter(this.package, writerFn, ExecuteTemplate);
      }

      public XcstOutputter OutputTo(Stream output, Uri outputUri = null, bool autoClose = false) {

         if (output == null) throw new ArgumentNullException(nameof(output));

         Func<OutputParameters, IWriterFactory> writerFn = @params =>
            WriterFactory.CreateFactory(output, outputUri ?? DefaultOuputUri, @params, autoClose);

         return new XcstOutputter(this.package, writerFn, ExecuteTemplate);
      }

      public XcstOutputter OutputTo(TextWriter output, Uri outputUri = null, bool autoClose = false) {

         if (output == null) throw new ArgumentNullException(nameof(output));

         Func<OutputParameters, IWriterFactory> writerFn = @params =>
            WriterFactory.CreateFactory(output, outputUri ?? DefaultOuputUri, @params, autoClose);

         return new XcstOutputter(this.package, writerFn, ExecuteTemplate);
      }

      public XcstOutputter OutputTo(XmlWriter output, Uri outputUri = null, bool autoClose = false) {

         if (output == null) throw new ArgumentNullException(nameof(output));

         Func<OutputParameters, IWriterFactory> writerFn = @params =>
            WriterFactory.CreateFactory(output, outputUri ?? DefaultOuputUri, autoClose);

         return new XcstOutputter(this.package, writerFn, ExecuteTemplate);
      }

      public XcstOutputter OutputTo(XcstWriter output, Uri outputUri = null, bool autoClose = false) {

         if (output == null) throw new ArgumentNullException(nameof(output));

         Func<OutputParameters, IWriterFactory> writerFn = @params =>
            WriterFactory.CreateFactory(output, outputUri ?? DefaultOuputUri, autoClose);

         return new XcstOutputter(this.package, writerFn, ExecuteTemplate);
      }

      void ExecuteTemplate(IXcstPackage package, DynamicContext context) {

         foreach (var param in this.parameters) {
            context.WithParam(param.Key, param.Value.Value, param.Value.Tunnel);
         }

         ClearParams();

         package.CallTemplate(this.name, context);
      }
   }

   public class XcstOutputter {

      readonly IXcstPackage package;
      readonly Func<OutputParameters, IWriterFactory> writerFn;
      readonly Action<IXcstPackage, DynamicContext> executionFn;

      OutputParameters parameters;
      IFormatProvider formatProvider;

      internal XcstOutputter(IXcstPackage package, Func<OutputParameters, IWriterFactory> writerFn, Action<IXcstPackage, DynamicContext> executionFn) {

         if (package == null) throw new ArgumentNullException(nameof(package));
         if (writerFn == null) throw new ArgumentNullException(nameof(writerFn));
         if (executionFn == null) throw new ArgumentNullException(nameof(executionFn));

         this.package = package;
         this.writerFn = writerFn;
         this.executionFn = executionFn;
      }

      public XcstOutputter WithParams(OutputParameters parameters) {

         this.parameters = parameters;
         return this;
      }

      public XcstOutputter WithFormatProvider(IFormatProvider formatProvider) {

         this.formatProvider = formatProvider;
         return this;
      }

      public void Run() {

         var execContext = new ExecutionContext(this.package, this.formatProvider);

         using (IWriterFactory writerFactory = writerFn(this.parameters)) {
            using (DynamicContext dynamicContext = execContext.CreateDynamicContext(writerFactory)) {

               this.package.Context = execContext;
               this.executionFn(this.package, dynamicContext);
            }
         }
      }
   }
}