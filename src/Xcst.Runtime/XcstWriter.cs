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
using Xcst.Runtime;
using Xcst.Xml;
using JObject = Newtonsoft.Json.Linq.JObject;
using JArray = Newtonsoft.Json.Linq.JArray;

namespace Xcst {

   public abstract class XcstWriter : ISequenceWriter<object?>, IDisposable {

      Stack<SequenceConstructor.State>?
      _trackStack;

      bool
      _disposed;

      public virtual Uri
      OutputUri { get; }

      /// <exclude/>
      [EditorBrowsable(EditorBrowsableState.Never)]
      public virtual SimpleContent
      SimpleContent { get; internal set; } = null!;

      protected internal abstract int
      Depth { get; }

      protected
      XcstWriter(Uri outputUri) {

         if (outputUri is null) throw new ArgumentNullException(nameof(outputUri));

         this.OutputUri = outputUri;
      }


      // ## Nodes

      public void
      WriteStartElement(string localName) =>
         WriteStartElement(null, localName, default(string));

      public void
      WriteStartElement(string localName, string? ns) =>
         WriteStartElement(null, localName, ns);

      public abstract void
      WriteStartElement(string? prefix, string localName, string? ns);

      public abstract void
      WriteEndElement();

      /// <exclude/>
      [EditorBrowsable(EditorBrowsableState.Never)]
      public void
      WriteStartElementLexical(string lexical, string? ns, string defaultNs) {

         int prefixIndex = lexical.IndexOf(':');
         bool hasPrefix = prefixIndex > 0;

         string? prefix = (hasPrefix) ? lexical.Substring(0, prefixIndex) : null;
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
      WriteAttributeString(string localName, string? ns, string value) {

         WriteStartAttribute(null, localName, ns);
         WriteString(value);
         WriteEndAttribute();
      }

      public void
      WriteAttributeString(string localName, string? value) {

         WriteStartAttribute(null, localName, default(string));
         WriteString(value);
         WriteEndAttribute();
      }

      public void
      WriteAttributeString(string? prefix, string localName, string? ns, string value) {

         WriteStartAttribute(prefix, localName, ns);
         WriteString(value);
         WriteEndAttribute();
      }

      public void
      WriteStartAttribute(string localName) =>
         WriteStartAttribute(null, localName, default(string), default(string));

      public void
      WriteStartAttribute(string localName, string? ns) =>
         WriteStartAttribute(null, localName, ns, default(string));

      public void
      WriteStartAttribute(string? prefix, string localName, string? ns) =>
         WriteStartAttribute(prefix, localName, ns, default(string));

      public abstract void
      WriteStartAttribute(string? prefix, string localName, string? ns, string? separator);

      public abstract void
      WriteEndAttribute();

      /// <exclude/>
      [EditorBrowsable(EditorBrowsableState.Never)]
      public void
      WriteAttributeStringLexical(string lexical, string? ns, string value) {

         WriteStartAttributeLexical(lexical, ns, null);
         WriteString(value);
         WriteEndAttribute();
      }

      /// <exclude/>
      [EditorBrowsable(EditorBrowsableState.Never)]
      public void
      WriteStartAttributeLexical(string lexical, string? ns) =>
         WriteStartAttributeLexical(lexical, ns, null);

      /// <exclude/>
      [EditorBrowsable(EditorBrowsableState.Never)]
      public void
      WriteStartAttributeLexical(string lexical, string? ns, string? separator) {

         int prefixIndex = lexical.IndexOf(':');
         bool hasPrefix = prefixIndex > 0;

         string? prefix = (hasPrefix) ? lexical.Substring(0, prefixIndex) : null;
         string localName = (hasPrefix) ? lexical.Substring(prefixIndex + 1) : lexical;

         if (hasPrefix
            && String.IsNullOrEmpty(ns)) {

            throw new NotSupportedException();
         }

         WriteStartAttribute(prefix, localName, ns, separator);
      }

      public abstract void
      WriteProcessingInstruction(string name, string? text);

      public abstract void
      WriteComment(string? text);

      public abstract void
      WriteString(string? text);

      public abstract void
      WriteChars(char[] buffer, int index, int count);

      public abstract void
      WriteRaw(string? data);

      void
      ISequenceWriter<object?>.WriteString(object? text) =>
         WriteString((string?)text);

      void
      ISequenceWriter<object?>.WriteRaw(object? data) =>
         WriteRaw((string?)data);


      // ## WriteObject

      public void
      WriteObject(object? value) {

         if (SimpleContent.ValueAsEnumerable(value) is IEnumerable seq) {
            WriteSequence(seq);
         } else {
            WriteItem(value);
         }
      }

      // Because in XcstWriter sequence flattening is done dynamically
      // and does not depend on overload resolution, these two can be private.
      // Same with CopyOf below.

      void
      ISequenceWriter<object?>.WriteObject(IEnumerable<object?>? value) =>
         WriteObject((object?)value);

      void
      ISequenceWriter<object?>.WriteObject<TDerived>(IEnumerable<TDerived>? value) =>
         WriteObject((object?)value);

      // Shortcut overloads

      public void
      WriteObject(string? value) =>
         WriteItem(value);

      public void
      WriteObject(IFormattable? value) =>
         WriteItem(value);

      public void
      WriteObject(Array? value) =>
         WriteSequence(value);

      protected internal virtual void
      WriteItem(object? value) {

         if (value != null) {
            WriteString(this.SimpleContent.Convert(value));
         }
      }

      protected void
      WriteSequence(IEnumerable? value) {

         if (value != null) {

            foreach (var item in value) {
               WriteItem(item);
            }
         }
      }


      // ## CopyOf

      public void
      CopyOf(object? value) =>
         CopyOfImpl(value, recurse: false);

      void
      ISequenceWriter<object?>.CopyOf(IEnumerable<object?>? value) =>
         CopyOf((object?)value);

      void
      ISequenceWriter<object?>.CopyOf<TDerived>(IEnumerable<TDerived>? value) =>
         CopyOf((object?)value);

      void
      CopyOfImpl(object? value, bool recurse) {

         if (value is null
            || TryCopyOf(value)) {

            return;
         }

         if (!recurse) {

            if (SimpleContent.ValueAsEnumerable(value, checkToString: false) is IEnumerable seq) {
               CopyOfSequence(seq);
               return;
            }
         }

         WriteObject(value);
      }

      public bool
      TryCopyOf(object? value) {

         if (value is XNode xNode) {
            CopyOf(xNode);
            return true;
         }

         if (value is XAttribute xAttr) {
            CopyOf(xAttr);
            return true;
         }

         if (value is XmlNode xmlNode) {
            CopyOf(xmlNode);
            return true;
         }

         if (value is IXPathNavigable xpathNav) {
            CopyOf(xpathNav);
            return true;
         }

         if (value is IXmlSerializable xmlSer) {
            CopyOf(xmlSer);
            return true;
         }

         if (value is XmlReader reader) {
            CopyOf(reader);
            return true;
         }

         if (value is JObject jObj) {
            CopyOf(jObj);
            return true;
         }

         if (value is JArray jArr) {
            CopyOf(jArr);
            return true;
         }

         return false;
      }

      void
      CopyOfSequence(IEnumerable value) {

         foreach (var item in value) {
            CopyOfImpl(item, recurse: true);
         }
      }

      // Shortcut overloads
      // Keeping interfaces private to avoid ambiguous calls

      public void
      CopyOf(XNode? value) =>
         value?.WriteTo(CreateXmlWriter());

      public void
      CopyOf(XAttribute? value) {

         if (value != null) {
            WriteAttributeString(value.Name.LocalName, value.Name.NamespaceName, value.Value);
         }
      }

      public void
      CopyOf(XmlNode? value) =>
         value?.WriteTo(CreateXmlWriter());

      void
      CopyOf(IXPathNavigable? value) {

         if (value != null) {

            XPathNavigator nav = value as XPathNavigator
               ?? value.CreateNavigator();

            nav.WriteSubtree(CreateXmlWriter());
         }
      }

      void
      CopyOf(IXmlSerializable? value) =>
         // Don't output root element, gives caller more flexibility and choice
         value?.WriteXml(CreateXmlWriter());

      public void
      CopyOf(XmlReader? value) {

         if (value != null) {
            CreateXmlWriter()
               .WriteNode(value, defattr: true);
         }
      }

      XmlWriter
      CreateXmlWriter() =>
         new XcstXmlWriter(this);

      public void
      CopyOf(Array? value) {

         if (value != null) {
            CopyOfSequence(value);
         }
      }

      // These are private to not force code that depends on XcstWriter
      // having to reference Newtonsoft.Json

      void
      CopyOf(JObject value) =>
         MapWriter.Create(this).CopyOf(value);

      void
      CopyOf(JArray value) =>
         MapWriter.Create(this).CopyOf(value);


      // ## Other

      public XcstWriter?
      TryCastToDocumentWriter() => this;

      public MapWriter?
      TryCastToMapWriter() => null;

      public virtual void
      BeginTrack(char cardinality) =>
         SequenceConstructor.BeginTrack(cardinality, this.Depth, ref _trackStack);

      internal virtual void
      OnItemWritten() => SequenceConstructor.OnItemWritten(_trackStack, this.Depth);

      internal virtual void
      OnItemWritting() => SequenceConstructor.OnItemWritting(_trackStack, this.Depth);

      public virtual bool
      OnEmpty() => SequenceConstructor.OnEmpty(_trackStack);

      public virtual void
      EndOfConstructor() => SequenceConstructor.EndOfConstructor(_trackStack);

      public virtual void
      EndTrack() => SequenceConstructor.EndTrack(_trackStack);

      public abstract void
      Flush();

      public void
      Dispose() {

         Dispose(true);
         GC.SuppressFinalize(this);
      }

      protected virtual void
      Dispose(bool disposing) {

         if (_disposed) {
            return;
         }

         _disposed = true;
      }
   }
}
