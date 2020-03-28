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
      output;

      public override Uri
      OutputUri => output.OutputUri;

      public override SimpleContent
      SimpleContent {
         get {
            return output.SimpleContent;
         }
         internal set {
            output.SimpleContent = value;
         }
      }

      protected
      WrappingWriter(XcstWriter baseWriter)
         : base(baseWriter.OutputUri) {

         if (baseWriter is null) throw new ArgumentNullException(nameof(baseWriter));

         this.output = baseWriter;
      }

      public override bool
      TryCopyOf(object? value) => this.output.TryCopyOf(value);

      public override void
      WriteChars(char[] buffer, int index, int count) =>
         this.output.WriteChars(buffer, index, count);

      public override void
      WriteComment(string? text) =>
         this.output.WriteComment(text);

      public override void
      WriteEndAttribute() =>
         this.output.WriteEndAttribute();

      public override void
      WriteEndElement() =>
         this.output.WriteEndElement();

      public override void
      WriteProcessingInstruction(string name, string? text) =>
         this.output.WriteProcessingInstruction(name, text);

      public override void
      WriteRaw(string? data) => this.output.WriteRaw(data);

      public override void
      WriteStartAttribute(string? prefix, string localName, string? ns, string? separator) =>
         this.output.WriteStartAttribute(prefix, localName, ns, separator);

      public override void
      WriteStartElement(string? prefix, string localName, string? ns) =>
         this.output.WriteStartElement(prefix, localName, ns);

      public override void
      WriteString(string? text) => this.output.WriteString(text);

      protected internal override void
      WriteItem(object? value) => this.output.WriteItem(value);

      public override void
      Flush() => this.output.Flush();

      protected override void
      Dispose(bool disposing) {

         if (disposing) {
            this.output.Dispose();
         }

         base.Dispose(disposing);
      }
   }
}
