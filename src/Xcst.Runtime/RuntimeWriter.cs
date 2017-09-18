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

namespace Xcst {

   class RuntimeWriter : WrappingWriter {

      bool inAttr;
      AttrNameVal[] arrAttrs;         // List of cached attribute names and value parts
      int numEntries;                 // Number of attributes in the cache
      int idxLastName;                // The entry containing the name of the last attribute to be cached
      int hashCodeUnion;              // Set of hash bits that can quickly guarantee a name is not a duplicate

      string itemSeparator;
      int depth;
      ItemType? lastItem;

      internal bool DisposeWriter { get; set; }

      public RuntimeWriter(XcstWriter baseWriter, OutputParameters parameters)
         : base(baseWriter) {

         this.itemSeparator = parameters.ItemSeparator;
      }

      public override void WriteStartElement(string prefix, string localName, string ns) {

         if (this.inAttr) {
            throw new RuntimeException("Cannot create an element within an attribute.");
         }

         FlushAttributes();
         ItemWriting(ItemType.Element);

         base.WriteStartElement(prefix, localName, ns);
      }

      public override void WriteEndElement() {

         FlushAttributes();

         base.WriteEndElement();

         ItemWritten(ItemType.Element);
      }

      public override void WriteStartAttribute(string prefix, string localName, string ns, string separator) {

         if (localName == null) throw new ArgumentNullException(nameof(localName));

         if (this.inAttr) {
            throw new RuntimeException("Cannot create an attribute within another attribute.");
         }

         if (prefix == null) {
            prefix = String.Empty;
         }

         if (ns == null) {
            ns = String.Empty;
         }

         this.inAttr = true;

         int hashCode;
         int idx = 0;
         Debug.Assert(localName != null && localName.Length != 0 && prefix != null && ns != null);

         // Compute hashcode based on first letter of the localName
         hashCode = (1 << ((int)localName[0] & 31));

         // If the hashcode is not in the union, then name will not be found by a scan
         if ((this.hashCodeUnion & hashCode) != 0) {

            // The name may or may not be present, so scan for it
            Debug.Assert(this.numEntries != 0);

            do {

               if (this.arrAttrs[idx].IsDuplicate(localName, ns, hashCode)) {
                  break;
               }

               // Next attribute name
               idx = this.arrAttrs[idx].NextNameIndex;

            } while (idx != 0);

         } else {

            // Insert hashcode into union
            this.hashCodeUnion |= hashCode;
         }

         // Insert new attribute; link attribute names together in a list
         EnsureAttributeCache();

         if (this.numEntries != 0) {
            this.arrAttrs[this.idxLastName].NextNameIndex = this.numEntries;
         }

         this.idxLastName = this.numEntries++;
         this.arrAttrs[this.idxLastName].Init(prefix, localName, ns, separator, hashCode);
      }

      public override void WriteEndAttribute() {
         this.inAttr = false;
      }

      public override void WriteComment(string text) {

         if (this.inAttr) {
            throw new RuntimeException("Cannot create a comment within an attribute.");
         }

         FlushAttributes();
         ItemWriting(ItemType.Comment);

         base.WriteComment(text);

         ItemWritten(ItemType.Comment);
      }

      public override void WriteProcessingInstruction(string name, string text) {

         if (this.inAttr) {
            throw new RuntimeException("Cannot create a processing instruction within an attribute.");
         }

         FlushAttributes();
         ItemWriting(ItemType.ProcessingInstruction);

         base.WriteProcessingInstruction(name, text);

         ItemWritten(ItemType.ProcessingInstruction);
      }

      public override void WriteString(string text) {

         if (this.inAttr) {

            EnsureAttributeCache();
            this.arrAttrs[this.numEntries++].Init(text);

         } else {

            FlushAttributes();
            ItemWriting(ItemType.Text);

            base.WriteString(text);

            ItemWritten(ItemType.Text);
         }
      }

      public override void WriteRaw(string data) {

         if (this.inAttr) {
            throw new InvalidOperationException($"Calling {nameof(WriteRaw)} for attributes is not supported.");
         }

         FlushAttributes();
         ItemWriting(ItemType.Raw);

         base.WriteRaw(data);

         ItemWritten(ItemType.Raw);
      }

      public override void WriteObject(object value) {

         if (this.inAttr) {

            if (value != null) {
               EnsureAttributeCache();
               this.arrAttrs[this.numEntries++].Init(value);
            }

         } else {

            FlushAttributes();

            if (value != null) {

               ItemWriting(ItemType.Object);

               base.WriteObject(value);

               ItemWritten(ItemType.Object);
            }
         }
      }

      void EnsureAttributeCache() {

         // Ensure that attribute array has been created and is large enough for at least one
         // additional entry.

         if (this.arrAttrs == null) {

            // Create caching array
            this.arrAttrs = new AttrNameVal[32];

         } else if (this.numEntries >= this.arrAttrs.Length) {

            // Resize caching array
            Debug.Assert(this.numEntries == this.arrAttrs.Length);
            AttrNameVal[] arrNew = new AttrNameVal[this.numEntries * 2];
            Array.Copy(this.arrAttrs, arrNew, this.numEntries);
            this.arrAttrs = arrNew;
         }
      }

      void FlushAttributes() {

         int idx = 0, idxNext;
         string localName;

         while (idx != this.numEntries) {

            // Get index of next attribute's name (0 if this is the last attribute)
            idxNext = this.arrAttrs[idx].NextNameIndex;

            if (idxNext == 0) {
               idxNext = this.numEntries;
            }

            // If localName is null, then this is a duplicate attribute that has been marked as "deleted"
            localName = this.arrAttrs[idx].LocalName;

            if (localName != null) {

               string prefix = this.arrAttrs[idx].Prefix;
               string ns = this.arrAttrs[idx].Namespace;
               string separator = this.arrAttrs[idx].Separator;

               base.WriteStartAttribute(prefix, localName, ns, null);

               bool first = true;
               bool lastWasText = false;

               // Output all of this attribute's text
               while (++idx != idxNext) {

                  object obj = this.arrAttrs[idx].Object;
                  string sep = separator;

                  if (obj != null) {

                     if (!first) {

                        if (!lastWasText
                           && sep == null) {

                           sep = " ";
                        }

                        if (!String.IsNullOrEmpty(sep)) {
                           base.WriteString(sep);
                        }
                     }

                     base.WriteObject(obj);

                     lastWasText = false;

                  } else {

                     if (!first
                        && !lastWasText
                        && !String.IsNullOrEmpty(sep)) {

                        base.WriteString(sep);
                     }

                     string text = this.arrAttrs[idx].Text;
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

         if (this.numEntries > 0) {

            for (int i = 0; i < this.arrAttrs.Length; i++) {
               this.arrAttrs[i].Init(default(string), default(string), default(string), default(string), default(int));
               this.arrAttrs[i].Init(default(string));
               this.arrAttrs[i].Init(default(object));
            }

            this.numEntries = default(int);
            this.idxLastName = default(int);
            this.hashCodeUnion = default(int);
         }
      }

      void ItemWriting(ItemType type) {

         if (this.lastItem != null
            && (this.lastItem.Value != ItemType.Text || type != ItemType.Text)) {

            string separator = (this.depth == 0 ? this.itemSeparator : null);

            if (separator == null
               && this.lastItem.Value == ItemType.Object
               && type == ItemType.Object) {

               separator = " ";
            }

            if (!String.IsNullOrEmpty(separator)) {
               base.WriteString(separator);
            }
         }

         if (type == ItemType.Element) {
            this.depth++;
            this.lastItem = null;
         }
      }

      void ItemWritten(ItemType type) {

         if (type == ItemType.Element) {
            this.depth--;
         }

         this.lastItem = type;
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

         string localName;
         string prefix;
         string namespaceName;
         string separator;
         string text;
         object obj;
         int hashCode;
         int nextNameIndex;

         public string LocalName => this.localName;

         public string Prefix => this.prefix;

         public string Namespace => this.namespaceName;

         public string Separator => this.separator;

         public string Text => this.text;

         public object Object => this.obj;

         public int NextNameIndex {
            get { return this.nextNameIndex; }
            set { this.nextNameIndex = value; }
         }

         /// <summary>
         /// Cache an attribute's name and type.
         /// </summary>

         public void Init(string prefix, string localName, string ns, string separator, int hashCode) {
            this.localName = localName;
            this.prefix = prefix;
            this.namespaceName = ns;
            this.separator = separator;
            this.hashCode = hashCode;
            this.nextNameIndex = 0;
         }

         /// <summary>
         /// Cache all or part of the attribute's string value.
         /// </summary>

         public void Init(string text) {
            this.text = text;
         }

         public void Init(object obj) {
            this.obj = obj;
         }

         /// <summary>
         /// Returns true if this attribute has the specified name (and thus is a duplicate).
         /// </summary>

         public bool IsDuplicate(string localName, string ns, int hashCode) {

            // If attribute is not marked as deleted
            if (this.localName != null) {

               // And if hash codes match,
               if (this.hashCode == hashCode) {

                  // And if local names match,
                  if (this.localName.Equals(localName)) {

                     // And if namespaces match,
                     if (this.namespaceName.Equals(ns)) {

                        // Then found duplicate attribute, so mark the attribute as deleted
                        this.localName = null;
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
