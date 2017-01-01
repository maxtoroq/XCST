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
using System.Xml;

namespace Xcst.Xml {

   class HtmlWriter : WrappingXmlWriter {

      bool outputHtml5Doctype;

      public HtmlWriter(XmlWriter baseWriter, bool outputHtml5Doctype)
         : base(baseWriter) {

         this.outputHtml5Doctype = outputHtml5Doctype;
      }

      public override void WriteStartElement(string prefix, string localName, string ns) {

         if (this.outputHtml5Doctype) {

            string name = !String.IsNullOrEmpty(prefix) ?
               (prefix + ":" + localName)
               : localName;

            switch (this.WriteState) {
               case WriteState.Start:
               case WriteState.Prolog:
                  WriteDocType(name, null, null, null);
                  break;
            }

            this.outputHtml5Doctype = false;
         }

         base.WriteStartElement(prefix, localName, ns);
      }
   }
}
