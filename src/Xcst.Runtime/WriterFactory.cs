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
using System.Text;
using System.Xml;
using Xcst.Runtime;
using Xcst.Xml;

namespace Xcst {

   delegate RuntimeWriter CreateWriterDelegate(OutputParameters defaultParams, OutputParameters? overrideParams, ExecutionContext context);

   static class WriterFactory {

      internal static readonly Uri
      AbsentOutputUri = new Uri(String.Empty, UriKind.Relative);

      public static CreateWriterDelegate
      CreateWriter(Stream output, Uri? outputUri) =>
         CreateWriter(p => CreateXmlWriter(output, p), outputUri);

      public static CreateWriterDelegate
      CreateWriter(TextWriter output, Uri? outputUri) =>
         CreateWriter(p => CreateXmlWriter(output, p), outputUri);

      public static CreateWriterDelegate
      CreateWriter(StringBuilder output) =>
         CreateWriter(p => CreateXmlWriter(output, p), null);

      public static CreateWriterDelegate
      CreateWriter(Uri file) =>
         CreateWriter(p => CreateXmlWriter(file, p), file, dispose: true);

      public static CreateWriterDelegate
      CreateWriter(XmlWriter output, Uri? outputUri) =>
         CreateWriter(p => output, outputUri);

      public static CreateWriterDelegate
      CreateWriter(XcstWriter output) {

         return (defaultParams, overrideParams, context) => {

            OutputParameters parameters = MergedParameters(defaultParams, overrideParams);

            return CreateRuntimeWriter(output, parameters, context);
         };
      }

      static CreateWriterDelegate
      CreateWriter(Func<OutputParameters, XmlWriter> writerFn, Uri? outputUri, bool dispose = false) {

         return (defaultParams, overrideParams, context) => {

            OutputParameters parameters = MergedParameters(defaultParams, overrideParams);

            return CreateRuntimeWriter(CreateXmlXcstWriter(parameters, outputUri ?? context.BaseOutputUri ?? AbsentOutputUri,
               p => writerFn(p)), parameters, context, dispose);
         };
      }

      static RuntimeWriter
      CreateRuntimeWriter(XcstWriter writer, OutputParameters parameters, ExecutionContext context, bool dispose = false) {

         var runtimeWriter = writer as RuntimeWriter
            ?? new RuntimeWriter(writer, parameters) {
               SimpleContent = context.SimpleContent,
               DisposeWriter = dispose
            };

         return runtimeWriter;
      }

      static OutputParameters
      MergedParameters(OutputParameters defaultParams, OutputParameters? overrideParams) {

         if (defaultParams is null) throw new ArgumentNullException(nameof(defaultParams));

         if (overrideParams != null) {
            defaultParams.Merge(overrideParams);
         }

         return defaultParams;
      }

      static XcstWriter
      CreateXmlXcstWriter(OutputParameters parameters, Uri outputUri, Func<OutputParameters, XmlWriter> writerFn) {

         XmlWriter writer = writerFn(parameters);

         XmlWriter finalWriter = WrapHtmlWriter(writer, parameters)
            ?? writer;

         return new XmlXcstWriter(finalWriter, outputUri, parameters);
      }

      static XmlWriter?
      WrapHtmlWriter(XmlWriter writer, OutputParameters parameters) {

         if (parameters.Method == OutputParameters.Methods.Html
            && parameters.DoctypePublic is null
            && parameters.DoctypeSystem is null
            && parameters.RequestedHtmlVersion() >= 5m) {

            return new HtmlWriter(writer, outputHtml5Doctype: true);
         }

         return null;
      }

      static XmlWriter
      CreateXmlWriter(Stream output, OutputParameters parameters) {

         var writerSettings = XmlWriterSettingsFactory.Create(parameters);

         return XmlWriter.Create(output, writerSettings);
      }

      static XmlWriter
      CreateXmlWriter(TextWriter output, OutputParameters parameters) {

         var writerSettings = XmlWriterSettingsFactory.Create(parameters);

         return XmlWriter.Create(output, writerSettings);
      }

      static XmlWriter
      CreateXmlWriter(StringBuilder output, OutputParameters parameters) {

         var writerSettings = XmlWriterSettingsFactory.Create(parameters);

         return XmlWriter.Create(output, writerSettings);
      }

      static XmlWriter
      CreateXmlWriter(Uri outputUri, OutputParameters parameters) {

         var writerSettings = XmlWriterSettingsFactory.Create(parameters);
         writerSettings.CloseOutput = true;

         return XmlWriter.Create(outputUri.LocalPath, writerSettings);
      }
   }
}
