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

#region XHtmlWriter is based on code from XMLMVP
// BSD License
// 
// Copyright (c) 2005, XMLMVP Project
// All rights reserved.
//
// Redistribution and use in source and binary forms, with or without 
// modification, are permitted provided that the following conditions 
// are met:
// 
// * Redistributions of source code must retain the above copyright 
// notice, this list of conditions and the following disclaimer. 
// * Redistributions in binary form must reproduce the above copyright 
// notice, this list of conditions and the following disclaimer in 
// the documentation and/or other materials provided with the 
// distribution. 
// * Neither the name of the XMLMVP Project nor the names of its 
// contributors may be used to endorse or promote products derived
// from this software without specific prior written permission. 
// 
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS 
// "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT 
// LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS 
// FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE 
// COPYRIGHT OWNER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, 
// INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, 
// BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; 
// LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER 
// CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT 
// LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN 
// ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE 
// POSSIBILITY OF SUCH DAMAGE.
#endregion

using System;
using System.Collections.Generic;
using System.Xml;

namespace Xcst.Xml {

   class XHtmlWriter : WrappingXmlWriter {

      readonly Stack<XmlQualifiedName>
      _elementStack = new Stack<XmlQualifiedName>();

      public
      XHtmlWriter(XmlWriter baseWriter)
         : base(baseWriter) { }

      public override void
      WriteStartElement(string prefix, string localName, string ns) {

         _elementStack.Push(new XmlQualifiedName(localName, ns));
         base.WriteStartElement(prefix, localName, ns);
      }

      public override void
      WriteEndElement() => WriteXHMLEndElement(fullEndTag: false);

      public override void
      WriteFullEndElement() => WriteXHMLEndElement(fullEndTag: true);

      void
      WriteXHMLEndElement(bool fullEndTag) {

         bool writeFullEndTag = fullEndTag;
         XmlQualifiedName elementName = _elementStack.Pop();

         if (String.IsNullOrEmpty(elementName.Namespace)
            || elementName.Namespace == "http://www.w3.org/1999/xhtml") {

            switch (elementName.Name.ToLowerInvariant()) {
               case "area":
               case "base":
               case "basefont":
               case "br":
               case "col":
               case "embed":
               case "frame":
               case "hr":
               case "img":
               case "input":
               case "isindex":
               case "keygen":
               case "link":
               case "meta":
               case "param":
               case "source":
               case "track":
               case "wbr":
                  writeFullEndTag = false;
                  break;

               default:
                  writeFullEndTag = true;
                  break;
            }
         }

         if (writeFullEndTag) {
            base.WriteFullEndElement();
         } else {
            base.WriteEndElement();
         }
      }
   }
}
