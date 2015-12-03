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

namespace Xcst {

   public abstract class XcstWriter : IDisposable {

      bool disposed;

      public void WriteStartElement(string localName) {
         WriteStartElement(null, localName, default(string));
      }

      public void WriteStartElement(string localName, string ns) {
         WriteStartElement(null, localName, ns);
      }

      public abstract void WriteStartElement(string prefix, string localName, string ns);

      public abstract void WriteEndElement();

      public void WriteAttributeString(string localName, string ns, string value) {

         WriteStartAttribute(null, localName, ns);
         WriteString(value);
         WriteEndAttribute();
      }

      public void WriteAttributeString(string localName, string value) {

         WriteStartAttribute(null, localName, default(string));
         WriteString(value);
         WriteEndAttribute();
      }

      public void WriteAttributeString(string prefix, string localName, string ns, string value) {

         WriteStartAttribute(prefix, localName, ns);
         WriteString(value);
         WriteEndAttribute();
      }

      public void WriteStartAttribute(string localName) {
         WriteStartAttribute(null, localName, default(string));
      }

      public void WriteStartAttribute(string localName, string ns) {
         WriteStartAttribute(null, localName, ns);
      }

      public abstract void WriteStartAttribute(string prefix, string localName, string ns);

      public abstract void WriteEndAttribute();

      public abstract void WriteString(string text);

      public abstract void WriteRaw(string data);

      public abstract void WriteProcessingInstruction(string name, string text);

      public abstract void WriteComment(string text);

      public void Dispose() {

         Dispose(true);
         GC.SuppressFinalize(this);
      }

      protected virtual void Dispose(bool disposing) {

         if (this.disposed) {
            return;
         }

         this.disposed = true;
      }
   }
}
