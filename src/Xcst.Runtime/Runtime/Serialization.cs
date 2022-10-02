﻿// Copyright 2016 Max Toro Q.
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
using System.Xml.Linq;

namespace Xcst.Runtime;

/// <exclude/>
public static class Serialization {

   public static string
   Serialize(IXcstPackage package, XName? outputName, OutputParameters parameters, Action<XcstWriter> action) {

      Argument.NotNull(package);
      Argument.NotNull(parameters);
      Argument.NotNull(action);

      var sb = new StringBuilder();
      var defaultParams = new OutputParameters();

      if (outputName != null) {
         package.ReadOutputDefinition(outputName, defaultParams);
      }

      using (XcstWriter writer = WriterFactory.CreateWriter(sb)(defaultParams, parameters, package.Context)) {
         action(writer);
      }

      return sb.ToString();
   }

   public static string
   SimpleContent(IXcstPackage package, string separator, Action<XcstWriter> action) {

      return Serialize(
         package,
         null,
         new OutputParameters {
            Method = OutputParameters.Methods.Text,
            ItemSeparator = separator
         },
         action
      );
   }

   public static XcstWriter
   ResultDocument(
         IXcstPackage package,
         OutputParameters parameters,
         XName? outputName,
         Uri outputUri) {

      Argument.NotNull(outputUri);

      return ResultDocumentImpl(u => WriterFactory.CreateWriter(u!), false, package, parameters, outputName, outputUri);
   }

   public static XcstWriter
   ResultDocument(
         IXcstPackage package,
         OutputParameters parameters,
         XName? outputName,
         Uri? outputUri,
         Stream output) {

      Argument.NotNull(output);

      return ResultDocumentImpl(u => WriterFactory.CreateWriter(output, u), true, package, parameters, outputName, outputUri);
   }

   public static XcstWriter
   ResultDocument(
         IXcstPackage package,
         OutputParameters parameters,
         XName? outputName,
         Uri? outputUri,
         TextWriter output) {

      Argument.NotNull(output);

      return ResultDocumentImpl(u => WriterFactory.CreateWriter(output, u), true, package, parameters, outputName, outputUri);
   }

   public static XcstWriter
   ResultDocument(
         IXcstPackage package,
         OutputParameters parameters,
         XName? outputName,
         Uri? outputUri,
         XmlWriter output) {

      Argument.NotNull(output);

      return ResultDocumentImpl(u => WriterFactory.CreateWriter(output, u), true, package, parameters, outputName, outputUri);
   }

   public static XcstWriter
   ResultDocument(
         IXcstPackage package,
         OutputParameters parameters,
         XName? outputName,
         Uri? outputUri,
         XcstWriter output) {

      Argument.NotNull(output);

      return ResultDocumentImpl(u => WriterFactory.CreateWriter(output), true, package, parameters, outputName, outputUri);
   }

   static XcstWriter
   ResultDocumentImpl(
         Func<Uri?, CreateWriterDelegate> writerFn,
         bool customOutput,
         IXcstPackage package,
         OutputParameters parameters,
         XName? outputName,
         Uri? outputUri) {

      Argument.NotNull(package);
      Argument.NotNull(parameters);

      if (outputUri != null
         && !customOutput
         && !outputUri.IsFile) {

         throw new RuntimeException($"Can write to file URIs only ({outputUri.OriginalString}).");
      }

      var defaultParams = new OutputParameters();
      package.ReadOutputDefinition(outputName, defaultParams);

      return writerFn(outputUri)
         (defaultParams, parameters, package.Context);
   }

   public static XcstWriter
   Void(IXcstPackage package) =>
      WriterFactory.CreateWriter(new NullWriter(WriterFactory.AbsentOutputUri))
         (new OutputParameters(), null, package.Context);
}

class NullWriter : XcstWriter {

   int
   _depth;

   protected internal override int
   Depth => _depth;

   public
   NullWriter(Uri outputUri)
      : base(outputUri) { }

   public override void
   Flush() { }

   public override void
   WriteChars(char[] buffer, int index, int count) { }

   public override void
   WriteComment(string? text) { }

   public override void
   WriteEndAttribute() {
      _depth--;
   }

   public override void
   WriteEndElement() {
      _depth--;
   }

   public override void
   WriteProcessingInstruction(string name, string? text) { }

   public override void
   WriteRaw(string? data) { }

   public override void
   WriteStartAttribute(string? prefix, string localName, string? ns, string? separator) {
      _depth++;
   }

   public override void
   WriteStartElement(string? prefix, string localName, string? ns) {
      _depth++;
   }

   public override void
   WriteString(string? text) { }
}
