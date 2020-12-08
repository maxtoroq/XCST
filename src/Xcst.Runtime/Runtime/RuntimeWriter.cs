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

#region RuntimeWriter is based on code from .NET Framework
//------------------------------------------------------------------------------
// <copyright file="XmlAttributeCache.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>                                                                
// <owner current="true" primary="true">[....]</owner>
//------------------------------------------------------------------------------
#endregion

using System;
using System.Diagnostics;

namespace Xcst.Runtime {

   class RuntimeWriter : WrappingWriter {

      bool
      _inAttr;

      AttrNameVal[]
      _arrAttrs = null!; // List of cached attribute names and value parts

      int
      _numEntries;       // Number of attributes in the cache

      int
      _idxLastName;      // The entry containing the name of the last attribute to be cached

      int
      _hashCodeUnion;    // Set of hash bits that can quickly guarantee a name is not a duplicate

      string?
      _itemSeparator;

      int
      _depth;

      ItemType?
      _lastItem;

      internal bool
      DisposeWriter { get; set; }

      public
      RuntimeWriter(XcstWriter baseWriter, OutputParameters parameters)
         : base(baseWriter) {

         _itemSeparator = parameters.ItemSeparator;
      }

      public override void
      WriteStartElement(string? prefix, string localName, string? ns) {

         if (_inAttr) {
            throw new RuntimeException("Cannot create an element within an attribute.");
         }

         FlushAttributes();
         ItemWriting(ItemType.Element);

         base.WriteStartElement(prefix, localName, ns);
      }

      public override void
      WriteEndElement() {

         FlushAttributes();

         base.WriteEndElement();

         ItemWritten(ItemType.Element);
      }

      public override void
      WriteStartAttribute(string? prefix, string localName, string? ns, string? separator) {

         if (localName is null) throw new ArgumentNullException(nameof(localName));

         if (_inAttr) {
            throw new RuntimeException("Cannot create an attribute within another attribute.");
         }

         if (prefix is null) {
            prefix = String.Empty;
         }

         if (ns is null) {
            ns = String.Empty;
         }

         _inAttr = true;

         int hashCode;
         int idx = 0;

         Assert.IsNotNull(localName);
         Debug.Assert(localName.Length != 0);
         Assert.IsNotNull(prefix);
         Assert.IsNotNull(ns);

         // Compute hashcode based on first letter of the localName
         hashCode = (1 << ((int)localName[0] & 31));

         // If the hashcode is not in the union, then name will not be found by a scan
         if ((_hashCodeUnion & hashCode) != 0) {

            // The name may or may not be present, so scan for it
            Debug.Assert(_numEntries != 0);

            do {

               if (_arrAttrs[idx].IsDuplicate(localName, ns, hashCode)) {
                  break;
               }

               // Next attribute name
               idx = _arrAttrs[idx].NextNameIndex;

            } while (idx != 0);

         } else {

            // Insert hashcode into union
            _hashCodeUnion |= hashCode;
         }

         // Insert new attribute; link attribute names together in a list
         EnsureAttributeCache();

         if (_numEntries != 0) {
            _arrAttrs[_idxLastName].NextNameIndex = _numEntries;
         }

         _idxLastName = _numEntries++;
         _arrAttrs[_idxLastName].Init(prefix, localName, ns, separator, hashCode);
      }

      public override void
      WriteEndAttribute() {
         _inAttr = false;
      }

      public override void
      WriteComment(string? text) {

         if (_inAttr) {
            throw new RuntimeException("Cannot create a comment within an attribute.");
         }

         FlushAttributes();
         ItemWriting(ItemType.Comment);

         base.WriteComment(text);

         ItemWritten(ItemType.Comment);
      }

      public override void
      WriteProcessingInstruction(string name, string? text) {

         if (_inAttr) {
            throw new RuntimeException("Cannot create a processing instruction within an attribute.");
         }

         FlushAttributes();
         ItemWriting(ItemType.ProcessingInstruction);

         base.WriteProcessingInstruction(name, text);

         ItemWritten(ItemType.ProcessingInstruction);
      }

      public override void
      WriteString(string? text) {

         if (_inAttr) {

            EnsureAttributeCache();
            _arrAttrs[_numEntries++].Init(text);

         } else {

            FlushAttributes();
            ItemWriting(ItemType.Text);

            base.WriteString(text);

            ItemWritten(ItemType.Text);
         }
      }

      public override void
      WriteChars(char[] buffer, int index, int count) {

         if (_inAttr) {

            EnsureAttributeCache();
            _arrAttrs[_numEntries++].Init(new string(buffer, index, count));

         } else {

            FlushAttributes();
            ItemWriting(ItemType.Text);

            base.WriteChars(buffer, index, count);

            ItemWritten(ItemType.Text);
         }
      }

      public override void
      WriteRaw(string? data) {

         if (_inAttr) {
            throw new InvalidOperationException($"Calling {nameof(WriteRaw)} for attributes is not supported.");
         }

         FlushAttributes();
         ItemWriting(ItemType.Raw);

         base.WriteRaw(data);

         ItemWritten(ItemType.Raw);
      }

      protected internal override void
      WriteItem(object? value) {

         if (_inAttr) {

            if (value != null) {
               EnsureAttributeCache();
               _arrAttrs[_numEntries++].Init(value);
            }

         } else {

            FlushAttributes();

            if (value != null) {

               ItemWriting(ItemType.Object);

               base.WriteItem(value);

               ItemWritten(ItemType.Object);
            }
         }
      }

      void
      EnsureAttributeCache() {

         // Ensure that attribute array has been created and is large enough for at least one
         // additional entry.

         if (_arrAttrs is null) {

            // Create caching array
            _arrAttrs = new AttrNameVal[32];

         } else if (_numEntries >= _arrAttrs.Length) {

            // Resize caching array
            Debug.Assert(_numEntries == _arrAttrs.Length);
            AttrNameVal[] arrNew = new AttrNameVal[_numEntries * 2];
            Array.Copy(_arrAttrs, arrNew, _numEntries);
            _arrAttrs = arrNew;
         }
      }

      void
      FlushAttributes() {

         int idx = 0, idxNext;
         string? localName;

         while (idx != _numEntries) {

            // Get index of next attribute's name (0 if this is the last attribute)
            idxNext = _arrAttrs[idx].NextNameIndex;

            if (idxNext == 0) {
               idxNext = _numEntries;
            }

            // If localName is null, then this is a duplicate attribute that has been marked as "deleted"
            localName = _arrAttrs[idx].LocalName;

            if (localName != null) {

               string? prefix = _arrAttrs[idx].Prefix;
               string? ns = _arrAttrs[idx].Namespace;
               string? separator = _arrAttrs[idx].Separator;

               base.WriteStartAttribute(prefix, localName, ns, null);

               bool first = true;
               bool lastWasText = false;

               // Output all of this attribute's text
               while (++idx != idxNext) {

                  object? obj = _arrAttrs[idx].Object;
                  string? sep = separator;

                  if (obj != null) {

                     if (!first) {

                        if (!String.IsNullOrEmpty(sep)) {
                           base.WriteString(sep);
                        }
                     }

                     base.WriteItem(obj);

                     lastWasText = false;

                  } else {

                     if (!first
                        && !lastWasText
                        && !String.IsNullOrEmpty(sep)) {

                        base.WriteString(sep);
                     }

                     string? text = _arrAttrs[idx].Text;
                     base.WriteString(text);

                     lastWasText = true;
                  }

                  first = false;
               }

               base.WriteEndAttribute();

            } else {
               // Skip over duplicate attributes
               idx = idxNext;
            }
         }

         if (_numEntries > 0) {

            for (int i = 0; i < _arrAttrs.Length; i++) {
               _arrAttrs[i].Init(default(string), default(string), default(string), default(string), default(int));
               _arrAttrs[i].Init(default(string));
               _arrAttrs[i].Init(default(object));
            }

            _numEntries = default(int);
            _idxLastName = default(int);
            _hashCodeUnion = default(int);
         }
      }

      void
      ItemWriting(ItemType type) {

         if (_lastItem != null
            && (_lastItem.Value != ItemType.Text || type != ItemType.Text)) {

            string? separator = (_depth == 0 ? _itemSeparator : null);

            if (separator is null
               && _lastItem.Value == ItemType.Object
               && type == ItemType.Object) {

               separator = " ";
            }

            if (!String.IsNullOrEmpty(separator)) {
               base.WriteString(separator);
            }
         }

         if (type == ItemType.Element) {
            _depth++;
            _lastItem = null;
         }
      }

      void
      ItemWritten(ItemType type) {

         if (type == ItemType.Element) {
            _depth--;
         }

         _lastItem = type;
      }

      enum ItemType {
         Element,
         Text,
         Raw,
         Comment,
         ProcessingInstruction,
         Object
      }

      struct AttrNameVal {

         string?
         _localName;

         string?
         _prefix;

         string?
         _namespaceName;

         string?
         _separator;

         string?
         _text;

         object?
         _obj;

         int
         _hashCode;

         int
         _nextNameIndex;

         public string?
         LocalName => _localName;

         public string?
         Prefix => _prefix;

         public string?
         Namespace => _namespaceName;

         public string?
         Separator => _separator;

         public string?
         Text => _text;

         public object?
         Object => _obj;

         public int
         NextNameIndex {
            get => _nextNameIndex;
            set => _nextNameIndex = value;
         }

         /// <summary>
         /// Cache an attribute's name and type.
         /// </summary>
         public void
         Init(string? prefix, string? localName, string? ns, string? separator, int hashCode) {
            _localName = localName;
            _prefix = prefix;
            _namespaceName = ns;
            _separator = separator;
            _hashCode = hashCode;
            _nextNameIndex = 0;
         }

         /// <summary>
         /// Cache all or part of the attribute's string value.
         /// </summary>
         public void
         Init(string? text) {
            _text = text;
         }

         public void
         Init(object? obj) {
            _obj = obj;
         }

         /// <summary>
         /// Returns true if this attribute has the specified name (and thus is a duplicate).
         /// </summary>
         public bool
         IsDuplicate(string localName, string? ns, int hashCode) {

            // If attribute is not marked as deleted
            if (_localName != null) {

               // And if hash codes match,
               if (_hashCode == hashCode) {

                  // And if local names match,
                  if (_localName.Equals(localName)) {

                     // And if namespaces match,
                     if (String.Equals(_namespaceName, ns)) {

                        // Then found duplicate attribute, so mark the attribute as deleted
                        _localName = null;
                        return true;
                     }
                  }
               }
            }

            return false;
         }
      }
   }
}
