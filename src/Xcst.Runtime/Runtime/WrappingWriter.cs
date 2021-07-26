// Copyright 2016 Max Toro Q.
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

namespace Xcst.Runtime {

   abstract class WrappingWriter : XcstWriter {

      readonly XcstWriter
      _output;

      public override Uri
      OutputUri => _output.OutputUri;

      public override SimpleContent
      SimpleContent {
         get => _output.SimpleContent;
         internal set => _output.SimpleContent = value;
      }

      protected internal override int
      Depth => _output.Depth;

      protected
      WrappingWriter(XcstWriter baseWriter)
         : base(baseWriter.OutputUri) {

         if (baseWriter is null) throw new ArgumentNullException(nameof(baseWriter));

         _output = baseWriter;
      }

      public override bool
      TryCopyOf(object? value) => _output.TryCopyOf(value);

      public override void
      WriteChars(char[] buffer, int index, int count) =>
         _output.WriteChars(buffer, index, count);

      public override void
      WriteComment(string? text) =>
         _output.WriteComment(text);

      public override void
      WriteEndAttribute() =>
         _output.WriteEndAttribute();

      public override void
      WriteEndElement() =>
         _output.WriteEndElement();

      public override void
      WriteProcessingInstruction(string name, string? text) =>
         _output.WriteProcessingInstruction(name, text);

      public override void
      WriteRaw(string? data) => _output.WriteRaw(data);

      public override void
      WriteStartAttribute(string? prefix, string localName, string? ns, string? separator) =>
         _output.WriteStartAttribute(prefix, localName, ns, separator);

      public override void
      WriteStartElement(string? prefix, string localName, string? ns) =>
         _output.WriteStartElement(prefix, localName, ns);

      public override void
      WriteString(string? text) => _output.WriteString(text);

      protected internal override void
      WriteItem(object? value) => _output.WriteItem(value);

      public override void
      BeginTrack(char cardinality) => _output.BeginTrack(cardinality);

      internal override void
      OnItemWritting() => _output.OnItemWritting();

      internal override void
      OnItemWritten() => _output.OnItemWritten();

      public override bool
      OnEmpty() => _output.OnEmpty();

      public override void
      EndOfConstructor() => _output.EndOfConstructor();

      public override void
      EndTrack() => _output.EndTrack();

      public override void
      Flush() => _output.Flush();

      protected override void
      Dispose(bool disposing) {

         if (disposing) {
            _output.Dispose();
         }

         base.Dispose(disposing);
      }
   }
}
