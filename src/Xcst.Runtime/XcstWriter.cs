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
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;
using System.Xml.XPath;
using Xcst.PackageModel;
using Xcst.Runtime;
using Xcst.Xml;
using JObject = Newtonsoft.Json.Linq.JObject;
using JArray = Newtonsoft.Json.Linq.JArray;

namespace Xcst {

   public abstract class XcstWriter : ISequenceWriter<object>, IDisposable {

      bool
      disposed;

      public virtual Uri
      OutputUri { get; }

      /// <exclude/>
      [EditorBrowsable(EditorBrowsableState.Never)]
      public virtual SimpleContent
      SimpleContent { get; internal set; }

      protected
      XcstWriter(Uri outputUri) {

         if (outputUri == null) throw new ArgumentNullException(nameof(outputUri));

         this.OutputUri = outputUri;
      }

      public void
      WriteStartElement(string localName) {
         WriteStartElement(null, localName, default(string));
      }

      public void
      WriteStartElement(string localName, string ns) {
         WriteStartElement(null, localName, ns);
      }

      public abstract void
      WriteStartElement(string prefix, string localName, string ns);

      public abstract void
      WriteEndElement();

      /// <exclude/>
      [EditorBrowsable(EditorBrowsableState.Never)]
      public void
      WriteStartElementLexical(string lexical, string ns, string defaultNs) {

         int prefixIndex = lexical.IndexOf(':');
         bool hasPrefix = prefixIndex > 0;

         string prefix = (hasPrefix) ? lexical.Substring(0, prefixIndex) : null;
         string localName = (hasPrefix) ? lexical.Substring(prefixIndex + 1) : lexical;

         if (hasPrefix) {

            if (String.IsNullOrEmpty(ns)) {
               throw new NotSupportedException();
            }

            WriteStartElement(prefix, localName, ns);

         } else {
            WriteStartElement(null, localName, ns ?? defaultNs);
         }
      }

      public void
      WriteAttributeString(string localName, string ns, string value) {

         WriteStartAttribute(null, localName, ns);
         WriteString(value);
         WriteEndAttribute();
      }

      public void
      WriteAttributeString(string localName, string value) {

         WriteStartAttribute(null, localName, default(string));
         WriteString(value);
         WriteEndAttribute();
      }

      public void
      WriteAttributeString(string prefix, string localName, string ns, string value) {

         WriteStartAttribute(prefix, localName, ns);
         WriteString(value);
         WriteEndAttribute();
      }

      public void
      WriteStartAttribute(string localName) {
         WriteStartAttribute(null, localName, default(string), default(string));
      }

      public void
      WriteStartAttribute(string localName, string ns) {
         WriteStartAttribute(null, localName, ns, default(string));
      }

      public void
      WriteStartAttribute(string prefix, string localName, string ns) {
         WriteStartAttribute(prefix, localName, ns, default(string));
      }

      public abstract void
      WriteStartAttribute(string prefix, string localName, string ns, string separator);

      public abstract void
      WriteEndAttribute();

      /// <exclude/>
      [EditorBrowsable(EditorBrowsableState.Never)]
      public void
      WriteAttributeStringLexical(string lexical, string ns, string value) {

         WriteStartAttributeLexical(lexical, ns, null);
         WriteString(value);
         WriteEndAttribute();
      }

      /// <exclude/>
      [EditorBrowsable(EditorBrowsableState.Never)]
      public void
      WriteStartAttributeLexical(string lexical, string ns) {
         WriteStartAttributeLexical(lexical, ns, null);
      }

      /// <exclude/>
      [EditorBrowsable(EditorBrowsableState.Never)]
      public void
      WriteStartAttributeLexical(string lexical, string ns, string separator) {

         int prefixIndex = lexical.IndexOf(':');
         bool hasPrefix = prefixIndex > 0;

         string prefix = (hasPrefix) ? lexical.Substring(0, prefixIndex) : null;
         string localName = (hasPrefix) ? lexical.Substring(prefixIndex + 1) : lexical;

         if (hasPrefix
            && String.IsNullOrEmpty(ns)) {

            throw new NotSupportedException();
         }

         WriteStartAttribute(prefix, localName, ns, separator);
      }

      public abstract void
      WriteProcessingInstruction(string name, string text);

      public abstract void
      WriteComment(string text);

      public abstract void
      WriteString(string text);

      public abstract void
      WriteChars(char[] buffer, int index, int count);

      public abstract void
      WriteRaw(string data);

      public abstract void
      Flush();

      public void
      Dispose() {

         Dispose(true);
         GC.SuppressFinalize(this);
      }

      protected virtual void
      Dispose(bool disposing) {

         if (this.disposed) {
            return;
         }

         this.disposed = true;
      }

      #region ISequenceWriter<object> Members

      void ISequenceWriter<object>.
      WriteString(object text) {
         WriteString((string)text);
      }

      void ISequenceWriter<object>.
      WriteRaw(object data) {
         WriteRaw((string)data);
      }

      public void
      WriteObject(object value) {

         IEnumerable seq = SimpleContent.ValueAsEnumerable(value);

         if (seq != null) {
            WriteSequence(seq);
         } else {
            WriteItem(value);
         }
      }

      void ISequenceWriter<object>.
      WriteObject(IEnumerable<object> value) {
         WriteObject((object)value);
      }

      void ISequenceWriter<object>.
      WriteObject<TDerived>(IEnumerable<TDerived> value) {
         WriteObject((object)value);
      }

      public void
      CopyOf(object value) {
         CopyOfImpl(value, recurse: false);
      }

      void ISequenceWriter<object>.
      CopyOf(IEnumerable<object> value) {
         CopyOf((object)value);
      }

      void ISequenceWriter<object>.
      CopyOf<TDerived>(IEnumerable<TDerived> value) {
         CopyOf((object)value);
      }

      public XcstWriter
      TryCastToDocumentWriter() => this;

      public MapWriter
      TryCastToMapWriter() => null;

      #endregion

      public void
      WriteObject(string value) {
         WriteItem(value);
      }

      public void
      WriteObject(IFormattable value) {
         WriteItem(value);
      }

      public void
      WriteObject(Array value) {
         WriteSequence(value);
      }

      protected internal virtual void
      WriteItem(object value) {

         if (value != null) {
            WriteString(this.SimpleContent.Convert(value));
         }
      }

      protected void
      WriteSequence(IEnumerable value) {

         if (value != null) {

            foreach (var item in value) {
               WriteItem(item);
            }
         }
      }

      void
      CopyOfImpl(object value, bool recurse) {

         if (value == null
            || TryCopyOf(value)) {

            return;
         }

         if (!recurse) {

            IEnumerable seq = SimpleContent.ValueAsEnumerable(value, checkToString: false);

            if (seq != null) {
               CopyOfSequence(seq);
               return;
            }
         }

         WriteObject(value);
      }

      public virtual bool
      TryCopyOf(object value) {

         XNode xNode = value as XNode;

         if (xNode != null) {
            CopyOf(xNode);
            return true;
         }

         XmlNode xmlNode = value as XmlNode;

         if (xmlNode != null) {
            CopyOf(xmlNode);
            return true;
         }

         IXPathNavigable xpathNav = value as IXPathNavigable;

         if (xpathNav != null) {
            CopyOf(xpathNav);
            return true;
         }

         IXmlSerializable xmlSer = value as IXmlSerializable;

         if (xmlSer != null) {
            CopyOf(xmlSer);
            return true;
         }

         XmlReader reader = value as XmlReader;

         if (reader != null) {
            CopyOf(reader);
            return true;
         }

         JObject jObj = value as JObject;

         if (jObj != null) {
            CopyOf(jObj);
            return true;
         }

         JArray jArr = value as JArray;

         if (jArr != null) {
            CopyOf(jArr);
            return true;
         }

         return false;
      }

      void
      CopyOfSequence(IEnumerable value) {

         if (value != null) {

            foreach (var item in value) {
               CopyOfImpl(item, recurse: true);
            }
         }
      }

      // These don't need to be public, but it's a small optimization

      public void
      CopyOf(XNode value) {
         value?.WriteTo(new XcstXmlWriter(this));
      }

      public void
      CopyOf(XmlNode value) {
         value?.WriteTo(new XcstXmlWriter(this));
      }

      public void
      CopyOf(IXPathNavigable value) {

         if (value != null) {

            XPathNavigator nav = value as XPathNavigator
               ?? value.CreateNavigator();

            nav.WriteSubtree(new XcstXmlWriter(this));
         }
      }

      public void
      CopyOf(IXmlSerializable value) {

         // Don't output root element, gives caller more flexibility and choice

         value?.WriteXml(new XcstXmlWriter(this));
      }

      public void
      CopyOf(XmlReader value) {

         if (value != null) {
            new XcstXmlWriter(this).WriteNode(value, true);
         }
      }

      public void
      CopyOf(Array value) {
         CopyOfSequence(value);
      }

      // These are private to not force code that depends on XcstWriter
      // having to reference Newtonsoft.Json

      void
      CopyOf(JObject value) {

         if (value != null) {
            MapWriter.Create(this).CopyOf(value);
         }
      }

      void
      CopyOf(JArray value) {

         if (value != null) {
            MapWriter.Create(this).CopyOf(value);
         }
      }
   }
}
