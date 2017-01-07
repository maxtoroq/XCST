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
using System.IO;
using System.Text;
using System.Xml;
using Xcst.Runtime;
using Xcst.Xml;

namespace Xcst {

   delegate RuntimeWriter CreateWriterDelegate(OutputParameters defaultParams, OutputParameters overrideParams, SimpleContent simplContent);

   static class WriterFactory {

      static readonly Uri DefaultOuputUri = new Uri("", UriKind.Relative);

      public static CreateWriterDelegate CreateWriter(Stream output, Uri outputUri) {
         return CreateWriter(p => CreateXmlWriter(output, p), outputUri);
      }

      public static CreateWriterDelegate CreateWriter(TextWriter output, Uri outputUri) {
         return CreateWriter(p => CreateXmlWriter(output, p), outputUri);
      }

      public static CreateWriterDelegate CreateWriter(StringBuilder output) {
         return CreateWriter(p => CreateXmlWriter(output, p), null);
      }

      public static CreateWriterDelegate CreateWriter(Uri file) {
         return CreateWriter(p => CreateXmlWriter(file, p), file, dispose: true);
      }

      public static CreateWriterDelegate CreateWriter(XmlWriter output, Uri outputUri) {
         return CreateWriter(p => output, outputUri);
      }

      public static CreateWriterDelegate CreateWriter(XcstWriter output) {

         return (defaultParams, overrideParams, simplContent) =>
            CreateRuntimeWriter(output, simplContent);
      }

      static CreateWriterDelegate CreateWriter(Func<OutputParameters, XmlWriter> writerFn, Uri outputUri, bool dispose = false) {

         return (defaultParams, overrideParams, simplContent) =>
            CreateRuntimeWriter(CreateXmlXcstWriter(MergedParameters(defaultParams, overrideParams), outputUri ?? DefaultOuputUri,
               p => writerFn(p)), simplContent, dispose);
      }

      static RuntimeWriter CreateRuntimeWriter(XcstWriter writer, SimpleContent simpleContent, bool dispose = false) {

         var runtimeWriter = writer as RuntimeWriter
            ?? new RuntimeWriter(writer) {
                  SimpleContent = simpleContent,
                  DisposeWriter = dispose
               };

         return runtimeWriter;
      }

      static OutputParameters MergedParameters(OutputParameters defaultParams, OutputParameters overrideParams) {

         if (overrideParams != null) {
            defaultParams.Merge(overrideParams);
         }

         return defaultParams;
      }

      static XcstWriter CreateXmlXcstWriter(OutputParameters parameters, Uri outputUri, Func<OutputParameters, XmlWriter> writerFn) {

         XmlWriter writer = writerFn(parameters);

         XmlWriter finalWriter = WrapHtmlWriter(writer, parameters)
            ?? WrapXHtmlWriter(writer, parameters)
            ?? writer;

         return new XmlXcstWriter(finalWriter, outputUri);
      }

      static XmlWriter WrapHtmlWriter(XmlWriter writer, OutputParameters parameters) {

         if (parameters.Method == OutputParameters.StandardMethods.Html
            && parameters.DoctypePublic == null
            && parameters.DoctypeSystem == null
            && parameters.RequestedHtmlVersion() >= 5m) {

            return new HtmlWriter(writer, outputHtml5Doctype: true);
         }

         return null;
      }

      static XmlWriter WrapXHtmlWriter(XmlWriter writer, OutputParameters parameters) {

         if (parameters.Method == OutputParameters.StandardMethods.XHtml) {
            return new XHtmlWriter(writer);
         }

         return null;
      }

      static XmlWriter CreateXmlWriter(Stream output, OutputParameters parameters) {

         var writerSettings = XmlWriterSettingsFactory.Create(parameters);

         return XmlWriter.Create(output, writerSettings);
      }

      static XmlWriter CreateXmlWriter(TextWriter output, OutputParameters parameters) {

         var writerSettings = XmlWriterSettingsFactory.Create(parameters);

         return XmlWriter.Create(output, writerSettings);
      }

      static XmlWriter CreateXmlWriter(StringBuilder output, OutputParameters parameters) {

         var writerSettings = XmlWriterSettingsFactory.Create(parameters);

         return XmlWriter.Create(output, writerSettings);
      }

      static XmlWriter CreateXmlWriter(Uri outputUri, OutputParameters parameters) {

         var writerSettings = XmlWriterSettingsFactory.Create(parameters);
         writerSettings.CloseOutput = true;

         return XmlWriter.Create(outputUri.LocalPath, writerSettings);
      }
   }
}