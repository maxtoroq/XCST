// Copyright 2025 Max Toro Q.
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
using System.Text;

namespace Xcst.Runtime;

sealed class SimpleContentWriter : XcstWriter {

   readonly StringBuilder
   _sb;

   bool
   _inAttr;

   int
   _depth;

   protected internal override int
   Depth => _depth;

   private bool
   InElementAttribute =>
      _inAttr && Depth > 0;

   public
   SimpleContentWriter(Uri outputUri, StringBuilder sb)
      : base(outputUri) {

      _sb = sb;
   }

   public override void
   WriteStartElement(string? prefix, string localName, string? ns) {
      _depth++;
   }

   public override void
   WriteEndElement() {
      _depth--;
   }

   public override void
   WriteStartAttribute(string? prefix, string localName, string? ns, string? separator) {
      _inAttr = true;
   }

   public override void
   WriteEndAttribute() {
      _inAttr = false;
   }

   public override void
   WriteComment(string? text) {

      if (_depth == 0) {
         WriteStringImpl(text);
      }
   }

   public override void
   WriteProcessingInstruction(string name, string? text) {

      if (_depth == 0) {
         WriteStringImpl(text);
      }
   }

   public override void
   WriteString(string? text) {

      if (!this.InElementAttribute) {
         WriteStringImpl(text);
      }
   }

   void
   WriteStringImpl(string? text) {

      if (!String.IsNullOrEmpty(text)) {
         OnItemWritting();
         _sb.Append(text);
         OnItemWritten();
      }
   }

   public override void
   WriteChars(char[] buffer, int index, int count) {

      if (!this.InElementAttribute
         && buffer is { Length: > 0 }) {

         OnItemWritting();
         _sb.Append(buffer, index, count);
         OnItemWritten();
      }
   }

   public override void
   WriteRaw(string? data) =>
      WriteString(data);

   public override void
   Flush() { }
}
