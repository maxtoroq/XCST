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
using Xcst.Runtime;

namespace Xcst {

   public class XcstEvaluator {

      static readonly QualifiedName
      _initialTemplate = new QualifiedName("initial-template", XmlNamespaces.Xcst);

      readonly IXcstPackage
      _package;

      readonly Dictionary<string, object?>
      _parameters = new Dictionary<string, object?>();

      bool
      _paramsLocked = false;

      PrimingContext?
      _primingContext;

      public static XcstEvaluator
      Using(object package) {

         if (package is null) throw new ArgumentNullException(nameof(package));

         if (package is IXcstPackage pkg) {
            return new XcstEvaluator(pkg);
         }

         throw new ArgumentException("Provided instance is not a valid XCST package.", nameof(package));
      }

      public static XcstEvaluator<TPackage>
      Using<TPackage>(TPackage package) where TPackage : IXcstPackage {

         if (package is null) throw new ArgumentNullException(nameof(package));

         return new XcstEvaluator<TPackage>(package);
      }

      internal
      XcstEvaluator(IXcstPackage package) {

         if (package is null) throw new ArgumentNullException(nameof(package));

         _package = package;
      }

      public XcstEvaluator
      WithParam(string name, object? value) {

         if (name is null) throw new ArgumentNullException(nameof(name));

         // since there's no way to un-prime, a second prime would still use the values
         // of the first prime (for parameters not specified in the second prime)
         // the workaround is to simply create a new evaluator

         if (_paramsLocked) {
            throw new InvalidOperationException($"Cannot modify parameters, use a new {nameof(XcstEvaluator)} object.");
         }

         _parameters[name] = value;

         return this;
      }

      public XcstEvaluator
      WithParams(object? parameters) {

         if (parameters != null) {
            WithParams(ObjectToDictionary(parameters));
         }

         return this;
      }

      public XcstEvaluator
      WithParams(IDictionary<string, object?>? parameters) {

         if (parameters != null) {

            foreach (var pair in parameters) {
               WithParam(pair.Key, pair.Value);
            }
         }

         return this;
      }

      internal static IDictionary<string, object?>
      ObjectToDictionary(object? values) {

         IDictionary<string, object?>? dict = null;

         if (values != null) {

            dict = values as IDictionary<string, object?>;

            if (dict != null) {
               return dict;
            }
         }

         if (dict is null) {
            dict = new Dictionary<string, object?>();
         }

         if (values != null) {

            PropertyDescriptorCollection props = TypeDescriptor.GetProperties(values);

            foreach (PropertyDescriptor? prop in props) {
               object val = prop!.GetValue(values);
               dict.Add(prop.Name, val);
            }
         }

         return dict;
      }

      public XcstTemplateEvaluator
      CallInitialTemplate() => CallTemplate(_initialTemplate);

      public XcstTemplateEvaluator
      CallTemplate(string name) {

         if (name is null) throw new ArgumentNullException(nameof(name));

         return CallTemplate(new QualifiedName(name));
      }

      public XcstTemplateEvaluator
      CallTemplate(QualifiedName name) {

         if (name is null) throw new ArgumentNullException(nameof(name));

         _paramsLocked = true;

         return new XcstTemplateEvaluator(_package, Prime, name);
      }

      public XcstTemplateEvaluator
      ApplyTemplates(object? input, QualifiedName? mode = null) {

         _paramsLocked = true;

         return new XcstTemplateEvaluator(_package, Prime, input, mode);
      }

      internal PrimingContext
      Prime() {

         if (_primingContext is null) {

            _primingContext = PrimingContext.Create(_parameters.Count);

            foreach (var param in _parameters) {
               _primingContext.WithParam(param.Key, param.Value);
            }

            _parameters.Clear();
            _package.Prime(_primingContext, null);
         }

         return _primingContext;
      }
   }

   public class XcstEvaluator<TPackage> : XcstEvaluator
         where TPackage : IXcstPackage {

      readonly TPackage
      _package;

      internal
      XcstEvaluator(TPackage package)
         : base(package) {

         _package = package;
      }

      public new XcstEvaluator<TPackage>
      WithParam(string name, object? value) {

         base.WithParam(name, value);
         return this;
      }

      public new XcstEvaluator<TPackage>
      WithParams(object? parameters) {

         base.WithParams(parameters);
         return this;
      }

      public new XcstEvaluator<TPackage>
      WithParams(IDictionary<string, object?>? parameters) {

         base.WithParams(parameters);
         return this;
      }

      public XcstOutputter
      CallFunction(Action<TPackage> functionCaller) {

         if (functionCaller is null) throw new ArgumentNullException(nameof(functionCaller));

         void executionFn(OutputParameters? overrideParams, bool skipFlush) =>
            functionCaller(_package);

         return new XcstOutputter(_package, Prime, executionFn);
      }

      public XcstOutputter<TResult>
      CallFunction<TResult>(Func<TPackage, TResult> functionCaller) {

         if (functionCaller is null) throw new ArgumentNullException(nameof(functionCaller));

         TResult executionFn() => functionCaller(_package);

         return new XcstOutputter<TResult>(_package, Prime, executionFn);
      }
   }

   public class XcstTemplateEvaluator {

      readonly IXcstPackage
      _package;

      readonly Func<PrimingContext>
      _primeFn;

      readonly QualifiedName?
      _name;

      readonly object?
      _input;

      readonly QualifiedName?
      _mode;

      readonly Dictionary<string, object?>
      _templateParameters = new Dictionary<string, object?>();

      readonly Dictionary<string, object?>
      _tunnelParameters = new Dictionary<string, object?>();

      internal
      XcstTemplateEvaluator(IXcstPackage package, Func<PrimingContext> primeFn, QualifiedName name) {

         if (package is null) throw new ArgumentNullException(nameof(package));
         if (primeFn is null) throw new ArgumentNullException(nameof(primeFn));
         if (name is null) throw new ArgumentNullException(nameof(name));

         _package = package;
         _primeFn = primeFn;
         _name = name;
      }

      internal
      XcstTemplateEvaluator(IXcstPackage package, Func<PrimingContext> primeFn, object? input, QualifiedName? mode) {

         if (package is null) throw new ArgumentNullException(nameof(package));
         if (primeFn is null) throw new ArgumentNullException(nameof(primeFn));

         _package = package;
         _primeFn = primeFn;
         _input = input;
         _mode = mode;
      }

      public XcstTemplateEvaluator
      WithParam(string name, object? value, bool tunnel = false) {

         if (name is null) throw new ArgumentNullException(nameof(name));

         if (tunnel) {
            _tunnelParameters[name] = value;
         } else {
            _templateParameters[name] = value;
         }

         return this;
      }

      public XcstTemplateEvaluator
      WithParams(object? parameters) {

         if (parameters != null) {
            WithParams(XcstEvaluator.ObjectToDictionary(parameters));
         }

         return this;
      }

      public XcstTemplateEvaluator
      WithParams(IDictionary<string, object?>? parameters) {

         if (parameters != null) {

            foreach (var pair in parameters) {
               WithParam(pair.Key, pair.Value);
            }
         }

         return this;
      }

      public XcstTemplateEvaluator
      WithTunnelParams(object? parameters) {

         if (parameters != null) {
            WithTunnelParams(XcstEvaluator.ObjectToDictionary(parameters));
         }

         return this;
      }

      public XcstTemplateEvaluator
      WithTunnelParams(IDictionary<string, object?>? parameters) {

         if (parameters != null) {

            foreach (var pair in parameters) {
               WithParam(pair.Key, pair.Value, tunnel: true);
            }
         }

         return this;
      }

      public XcstTemplateEvaluator
      ClearParams() {

         _templateParameters.Clear();
         _tunnelParameters.Clear();
         return this;
      }

      public XcstOutputter
      OutputTo(Uri file) {

         if (file is null) throw new ArgumentNullException(nameof(file));
         if (!file.IsAbsoluteUri) throw new ArgumentException("file must be an absolute URI.", nameof(file));
         if (!file.IsFile) throw new ArgumentException("file must be a file URI", nameof(file));

         return CreateOutputter(WriterFactory.CreateWriter(file));
      }

      public XcstOutputter
      OutputTo(Stream output) {

         if (output is null) throw new ArgumentNullException(nameof(output));

         return CreateOutputter(WriterFactory.CreateWriter(output, null));
      }

      public XcstOutputter
      OutputTo(TextWriter output) {

         if (output is null) throw new ArgumentNullException(nameof(output));

         return CreateOutputter(WriterFactory.CreateWriter(output, null));
      }

      public XcstOutputter
      OutputTo(XmlWriter output) {

         if (output is null) throw new ArgumentNullException(nameof(output));

         return CreateOutputter(WriterFactory.CreateWriter(output, null));
      }

      public XcstOutputter
      OutputTo(XcstWriter output) {

         if (output is null) throw new ArgumentNullException(nameof(output));

         return CreateOutputter(WriterFactory.CreateWriter(output));
      }

      public XcstOutputter
      OutputTo<TItem>(ICollection<TItem> output) {

         if (output is null) throw new ArgumentNullException(nameof(output));

         var seqWriter = new SequenceWriter<TItem>(output);

         return OutputToRaw(seqWriter);
      }

      public XcstOutputter
      OutputTo<TItem>(Action<TItem> outputFn) {

         if (outputFn is null) throw new ArgumentNullException(nameof(outputFn));

         var seqWriter = new StreamedSequenceWriter<TItem>(outputFn);

         return OutputToRaw(seqWriter);
      }

      /// <exclude/>
      [EditorBrowsable(EditorBrowsableState.Never)]
      public XcstOutputter
      OutputToRaw<TBase>(ISequenceWriter<TBase> output) {

         if (output is null) throw new ArgumentNullException(nameof(output));

         Action<TemplateContext> tmplFn = TemplateFunction(output);

         void executionFn(OutputParameters? overrideParams, bool skipFlush, TemplateContext tmplContext) =>
            EvaluateTemplate(tmplFn, tmplContext);

         return CreateOutputter(executionFn);
      }

      XcstOutputter
      CreateOutputter(CreateWriterDelegate writerFn) {

         void executionFn(OutputParameters? overrideParams, bool skipFlush, TemplateContext tmplContext) =>
            EvaluateToWriter(writerFn, overrideParams, skipFlush, tmplContext);

         return CreateOutputter(executionFn);
      }

      XcstOutputter
      CreateOutputter(Action<OutputParameters?, bool, TemplateContext> executionFn) {

         // it's important to keep template parameters' variables outside the execution delegate
         // so subsequent modifications do not affect previously created outputters

         var templateParams = new Dictionary<string, object?>(_templateParameters);
         var tunnelParams = new Dictionary<string, object?>(_tunnelParameters);

         void executionFn2(OutputParameters? overrideParams, bool skipFlush) =>
            executionFn(overrideParams, skipFlush, CreateTemplateContext(templateParams, tunnelParams));

         return new XcstOutputter(_package, _primeFn, executionFn2);
      }

      TemplateContext
      CreateTemplateContext(Dictionary<string, object?> templateParams, Dictionary<string, object?> tunnelParams) {

         var context = (_name != null) ?
            TemplateContext.Create(templateParams.Count, tunnelParams.Count)
            : TemplateContext.ForApplyTemplates(templateParams.Count, tunnelParams.Count);

         foreach (var param in templateParams) {
            context.WithParam(param.Key, param.Value);
         }

         foreach (var param in tunnelParams) {
            context.WithParam(param.Key, param.Value, tunnel: true);
         }

         return context;
      }

      Action<TemplateContext>
      TemplateFunction<TBase>(ISequenceWriter<TBase> output) =>
         (_name != null) ?
            _package.GetTemplate(_name, output) ?? throw DynamicError.UnknownTemplate(_name)
            : _package.GetMode(_mode, output) ?? throw DynamicError.UnknownMode(_mode);

      void
      EvaluateTemplate(Action<TemplateContext> tmplFn, TemplateContext tmplContext) {

         if (_name != null) {
            tmplFn(tmplContext);
         } else {
            ApplyTemplates.Apply(tmplContext, _input, _mode, tmplFn);
         }
      }

      void
      EvaluateToWriter(CreateWriterDelegate writerFn, OutputParameters? overrideParams, bool skipFlush, TemplateContext tmplContext) {

         var defaultParams = new OutputParameters();
         _package.ReadOutputDefinition(null, defaultParams);

         RuntimeWriter writer = writerFn(defaultParams, overrideParams, _package.Context);

         try {

            Action<TemplateContext> tmplFn = TemplateFunction(writer);
            EvaluateTemplate(tmplFn, tmplContext);

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

   public class XcstOutputter {

      readonly IXcstPackage
      _package;

      readonly Func<PrimingContext>
      _primeFn;

      readonly Action<OutputParameters?, bool>
      _executionFn;

      OutputParameters?
      _parameters;

      Func<IFormatProvider>?
      _formatProviderFn;

      Uri?
      _baseUri;

      Uri?
      _baseOutputUri;

      internal
      XcstOutputter(IXcstPackage package, Func<PrimingContext> primeFn, Action<OutputParameters?, bool> executionFn) {

         if (package is null) throw new ArgumentNullException(nameof(package));
         if (primeFn is null) throw new ArgumentNullException(nameof(primeFn));
         if (executionFn is null) throw new ArgumentNullException(nameof(executionFn));

         _package = package;
         _primeFn = primeFn;
         _executionFn = executionFn;
      }

      public XcstOutputter
      WithParams(OutputParameters? parameters) {

         if (parameters != null) {
            parameters = new OutputParameters(parameters);
         }

         _parameters = parameters;
         return this;
      }

      public XcstOutputter
      WithFormatProvider(IFormatProvider? formatProvider) {

         if (formatProvider != null) {
            return WithFormatProvider(() => formatProvider);
         }

         _formatProviderFn = null;
         return this;
      }

      public XcstOutputter
      WithFormatProvider(Func<IFormatProvider>? formatProviderFn) {

         _formatProviderFn = formatProviderFn;
         return this;
      }

      public XcstOutputter
      WithBaseUri(Uri? baseUri) {

         if (baseUri != null
            && !baseUri.IsAbsoluteUri) {

            throw new ArgumentException("An absolute URI is expected.", nameof(baseUri));
         }

         _baseUri = baseUri;
         return this;
      }

      public XcstOutputter
      WithBaseOutputUri(Uri? baseOutputUri) {

         if (baseOutputUri != null
            && !baseOutputUri.IsAbsoluteUri) {

            throw new ArgumentException("An absolute URI is expected.", nameof(baseOutputUri));
         }

         _baseOutputUri = baseOutputUri;
         return this;
      }

      public void
      Run(bool skipFlush = false) {

         InitPackage();
         _executionFn(_parameters, skipFlush);
      }

      internal void
      InitPackage() {

         PrimingContext primingContext = _primeFn();

         var execContext = new ExecutionContext(_package, primingContext, _formatProviderFn, _baseUri, _baseOutputUri);

         _package.Context = execContext;
      }
   }

   public class XcstOutputter<TResult> : XcstOutputter {

      readonly Func<TResult>
      _executionFn;

      internal
      XcstOutputter(IXcstPackage package, Func<PrimingContext> primeFn, Func<TResult> executionFn)
         : base(package, primeFn, (p, sf) => executionFn()) {

         _executionFn = executionFn;
      }

      public new XcstOutputter<TResult>
      WithParams(OutputParameters? parameters) {

         base.WithParams(parameters);
         return this;
      }

      public new XcstOutputter<TResult>
      WithFormatProvider(IFormatProvider? formatProvider) {

         base.WithFormatProvider(formatProvider);
         return this;
      }

      public new XcstOutputter<TResult>
      WithFormatProvider(Func<IFormatProvider>? formatProviderFn) {

         base.WithFormatProvider(formatProviderFn);
         return this;
      }

      public new XcstOutputter<TResult>
      WithBaseUri(Uri? baseUri) {

         base.WithBaseUri(baseUri);
         return this;
      }

      public new XcstOutputter<TResult>
      WithBaseOutputUri(Uri? baseOutputUri) {

         base.WithBaseOutputUri(baseOutputUri);
         return this;
      }

      public TResult
      Evaluate() {

         InitPackage();
         return _executionFn();
      }
   }
}
