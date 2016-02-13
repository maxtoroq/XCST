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

      readonly IXcstExecutable executable;
      readonly IDictionary<string, object> parameters = new Dictionary<string, object>();
      bool primed = false;

      public static XcstEvaluator Using<TCompiled>() where TCompiled : new() {
         return Using(new TCompiled());
      }

      public static XcstEvaluator Using(Type compiled) {
         return Using(Activator.CreateInstance(compiled));
      }

      public static XcstEvaluator Using(object instance) {

         if (instance == null) throw new ArgumentNullException(nameof(instance));

         var executable = instance as IXcstExecutable;

         if (executable == null) {
            throw new ArgumentException("Provided instance is not a valid XCST program.", nameof(instance));
         }

         return new XcstEvaluator(executable);
      }

      private XcstEvaluator(IXcstExecutable executable) {

         if (executable == null) throw new ArgumentNullException(nameof(executable));

         this.executable = executable;
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

         return new XcstTemplateEvaluator(this.executable, name);
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
         this.executable.Prime(context);
         this.primed = true;
      }
   }

   public class XcstTemplateEvaluator {

      static readonly Uri DefaultOuputUri = new Uri("", UriKind.Relative);

      readonly IXcstExecutable executable;
      readonly QualifiedName name;
      readonly IDictionary<string, ParameterArgument> parameters = new Dictionary<string, ParameterArgument>();

      internal XcstTemplateEvaluator(IXcstExecutable executable, QualifiedName name) {

         if (executable == null) throw new ArgumentNullException(nameof(executable));
         if (name == null) throw new ArgumentNullException(nameof(name));

         this.executable = executable;
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

         return new XcstOutputter(this.executable, writerFn, ExecuteTemplate);
      }

      public XcstOutputter OutputTo(Stream output, Uri outputUri = null, bool autoClose = false) {

         if (output == null) throw new ArgumentNullException(nameof(output));

         Func<OutputParameters, IWriterFactory> writerFn = @params =>
            WriterFactory.CreateFactory(output, outputUri ?? DefaultOuputUri, @params, autoClose);

         return new XcstOutputter(this.executable, writerFn, ExecuteTemplate);
      }

      public XcstOutputter OutputTo(TextWriter output, Uri outputUri = null, bool autoClose = false) {

         if (output == null) throw new ArgumentNullException(nameof(output));

         Func<OutputParameters, IWriterFactory> writerFn = @params =>
            WriterFactory.CreateFactory(output, outputUri ?? DefaultOuputUri, @params, autoClose);

         return new XcstOutputter(this.executable, writerFn, ExecuteTemplate);
      }

      public XcstOutputter OutputTo(XmlWriter output, Uri outputUri = null, bool autoClose = false) {

         if (output == null) throw new ArgumentNullException(nameof(output));

         Func<OutputParameters, IWriterFactory> writerFn = @params =>
            WriterFactory.CreateFactory(output, outputUri ?? DefaultOuputUri, autoClose);

         return new XcstOutputter(this.executable, writerFn, ExecuteTemplate);
      }

      public XcstOutputter OutputTo(XcstWriter output, Uri outputUri = null, bool autoClose = false) {

         if (output == null) throw new ArgumentNullException(nameof(output));

         Func<OutputParameters, IWriterFactory> writerFn = @params =>
            WriterFactory.CreateFactory(output, outputUri ?? DefaultOuputUri, autoClose);

         return new XcstOutputter(this.executable, writerFn, ExecuteTemplate);
      }

      void ExecuteTemplate(IXcstExecutable executable, DynamicContext context) {

         foreach (var param in this.parameters) {
            context.WithParam(param.Key, param.Value.Value, param.Value.Tunnel);
         }

         ClearParams();

         executable.CallTemplate(this.name, context);
      }
   }

   public class XcstOutputter {

      readonly IXcstExecutable executable;
      readonly Func<OutputParameters, IWriterFactory> writerFn;
      readonly Action<IXcstExecutable, DynamicContext> executionFn;

      OutputParameters parameters;
      IFormatProvider formatProvider;

      internal XcstOutputter(IXcstExecutable executable, Func<OutputParameters, IWriterFactory> writerFn, Action<IXcstExecutable, DynamicContext> executionFn) {

         if (executable == null) throw new ArgumentNullException(nameof(executable));
         if (writerFn == null) throw new ArgumentNullException(nameof(writerFn));
         if (executionFn == null) throw new ArgumentNullException(nameof(executionFn));

         this.executable = executable;
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

         var execContext = new ExecutionContext(this.executable, this.formatProvider);

         using (IWriterFactory writerFactory = writerFn(this.parameters)) {
            using (DynamicContext dynamicContext = execContext.CreateDynamicContext(writerFactory)) {

               this.executable.Context = execContext;
               this.executionFn(this.executable, dynamicContext);
            }
         }
      }
   }
}