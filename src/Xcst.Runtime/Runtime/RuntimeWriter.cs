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

namespace Xcst.Runtime;

// RuntimeWriter is a wrapping writer that implements attribute buffering/overriding
// and item separators

sealed class RuntimeWriter : WrappingWriter {

   readonly bool
   _isSimplContent;

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

   readonly string?
   _itemSeparator;

   ItemType
   _lastItem;

   internal bool
   DisposeWriter { get; set; }

   public
   RuntimeWriter(XcstWriter baseWriter, string? itemSeparator)
      : base(baseWriter) {

      _isSimplContent = baseWriter is SimpleContentWriter;
      _itemSeparator = itemSeparator;
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

      Argument.NotNull(localName);

      if (_inAttr) {
         throw new RuntimeException("Cannot create an attribute within another attribute.");
      }

      _inAttr = true;

      if (_isSimplContent) {
         base.WriteStartAttribute(prefix, localName, ns, separator);
         return;
      }

      prefix ??= String.Empty;
      ns ??= String.Empty;

      Assert.That(localName != null);
      Debug.Assert(localName.Length != 0);
      Assert.That(prefix != null);
      Assert.That(ns != null);

      // Compute hashcode based on first letter of the localName
      var hashCode = (1 << ((int)localName[0] & 31));

      // If the hashcode is not in the union, then name will not be found by a scan
      if ((_hashCodeUnion & hashCode) != 0) {

         // The name may or may not be present, so scan for it
         Debug.Assert(_numEntries != 0);

         var idx = 0;

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

      if (_isSimplContent) {
         base.WriteEndAttribute();
      }
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

      if (_inAttr
         && !_isSimplContent) {

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

      if (_inAttr
         && !_isSimplContent) {

         WriteString(new String(buffer, index, count));

      } else {

         FlushAttributes();
         ItemWriting(ItemType.Text);

         base.WriteChars(buffer, index, count);

         ItemWritten(ItemType.Text);
      }
   }

   public override void
   WriteRaw(string? data) {

      if (_inAttr
         && !_isSimplContent) {

         WriteString(data);

      } else {

         FlushAttributes();
         ItemWriting(ItemType.Text);

         base.WriteRaw(data);

         ItemWritten(ItemType.Text);
      }
   }

   protected internal override void
   WriteItem(object? value) {

      if (_inAttr
         && !_isSimplContent) {

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
         var arrNew = new AttrNameVal[_numEntries * 2];
         Array.Copy(_arrAttrs, arrNew, _numEntries);
         _arrAttrs = arrNew;
      }
   }

   void
   FlushAttributes() {

      if (_inAttr
         || _isSimplContent) {

         return;
      }

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

         if (localName is null) {
            // Skip over duplicate attributes
            idx = idxNext;
            continue;
         }

         var prefix = _arrAttrs[idx].Prefix;
         var ns = _arrAttrs[idx].Namespace;
         var separator = _arrAttrs[idx].Separator ?? " ";

         base.WriteStartAttribute(prefix, localName, ns, null);

         var first = true;
         var lastWasText = false;

         // Output all of this attribute's text
         while (++idx != idxNext) {

            if (_arrAttrs[idx].Object is { } obj) {

               if (!first
                  && !String.IsNullOrEmpty(separator)) {

                  base.WriteString(separator);
               }

               base.WriteItem(obj);

               lastWasText = false;

            } else {

               if (!first
                  && !lastWasText
                  && !String.IsNullOrEmpty(separator)) {

                  base.WriteString(separator);
               }

               var text = _arrAttrs[idx].Text;
               base.WriteString(text);

               lastWasText = true;
            }

            first = false;
         }

         base.WriteEndAttribute();
      }

      if (_numEntries > 0) {

         for (int i = 0; i < _arrAttrs.Length; i++) {
            _arrAttrs[i].Init(default(string), default(string), default(string), default(string), default(int));
            _arrAttrs[i].Init(default(string));
            _arrAttrs[i].Init(default(object));
         }

         _numEntries = default;
         _idxLastName = default;
         _hashCodeUnion = default;
      }
   }

   void
   ItemWriting(ItemType type) {

      if (_lastItem != default
         && (_lastItem != ItemType.Text || type != ItemType.Text)) {

         var separator = (this.Depth == 0) ? _itemSeparator : null;

         if (separator is null
            && _lastItem == ItemType.Object
            && type == ItemType.Object) {

            separator = " ";
         }

         if (!String.IsNullOrEmpty(separator)) {
            base.WriteString(separator);
         }
      }

      if (type == ItemType.Element) {
         // Reset _lastItem for child nodes
         _lastItem = default;
      }
   }

   void
   ItemWritten(ItemType type) {
      _lastItem = type;
   }

   enum ItemType {
      None,
      Element,
      Text,
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
         if (_localName != null
            // And if hash codes match,
            && _hashCode == hashCode
            // And if local names match,
            && _localName.Equals(localName)
            // And if namespaces match,
            && String.Equals(_namespaceName, ns)) {

            // Then found duplicate attribute, so mark the attribute as deleted
            _localName = null;
            return true;
         }

         return false;
      }
   }
}
