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

namespace Xcst.Runtime;

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
   WriteChars(char[] buffer, int index, int count) {

      if (buffer != null
         && buffer.Length > 0) {

         OnItemWritting();
         OnItemWritten();
      }
   }

   public override void
   WriteComment(string? text) {
      OnItemWritting();
      OnItemWritten();
   }

   public override void
   WriteEndAttribute() {
      _depth--;
   }

   public override void
   WriteEndElement() {
      _depth--;
      OnItemWritten();
   }

   public override void
   WriteProcessingInstruction(string name, string? text) {
      OnItemWritting();
      OnItemWritten();
   }

   public override void
   WriteRaw(string? data) { }

   public override void
   WriteStartAttribute(string? prefix, string localName, string? ns, string? separator) {
      _depth++;
   }

   public override void
   WriteStartElement(string? prefix, string localName, string? ns) {
      OnItemWritting();
      _depth++;
   }

   public override void
   WriteString(string? text) {
      if (!String.IsNullOrEmpty(text)) {
         OnItemWritting();
         OnItemWritten();
      }
   }
}
