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

      internal bool DisposeWriter { get; set; }

      public RuntimeWriter(XcstWriter baseWriter)
         : base(baseWriter) { }

      public override void WriteStartAttribute(string prefix, string localName, string ns) {

         if (localName == null) throw new ArgumentNullException(nameof(localName));

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
         this.arrAttrs[this.idxLastName].Init(prefix, localName, ns, hashCode);
      }

      public override void WriteEndAttribute() {
         this.inAttr = false;
      }

      public override void WriteString(string text) {

         if (this.inAttr) {

            EnsureAttributeCache();
            this.arrAttrs[this.numEntries++].Init(text);

         } else {

            FlushAttributes();
            base.WriteString(text);
         }
      }

      public override void WriteComment(string text) {

         if (!this.inAttr) {
            FlushAttributes();
         }

         base.WriteComment(text);
      }

      public override void WriteEndElement() {

         if (!this.inAttr) {
            FlushAttributes();
         }

         base.WriteEndElement();
      }

      public override void WriteProcessingInstruction(string name, string text) {

         if (!this.inAttr) {
            FlushAttributes();
         }

         base.WriteProcessingInstruction(name, text);
      }

      public override void WriteRaw(string data) {

         if (this.inAttr) {
            throw new InvalidOperationException($"Calling {nameof(WriteRaw)} for attributes is not supported.");
         }

         FlushAttributes();
         base.WriteRaw(data);
      }

      public override void WriteStartElement(string prefix, string localName, string ns) {

         if (!this.inAttr) {
            FlushAttributes();
         }

         base.WriteStartElement(prefix, localName, ns);
      }

      /// <summary>
      /// Ensure that attribute array has been created and is large enough for at least one
      /// additional entry.
      /// </summary>

      void EnsureAttributeCache() {

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

               base.WriteStartAttribute(prefix, localName, ns);

               // Output all of this attribute's text
               while (++idx != idxNext) {
                  string text = this.arrAttrs[idx].Text;
                  base.WriteString(text);
               }

               base.WriteEndAttribute();

            } else {
               // Skip over duplicate attributes
               idx = idxNext;
            }
         }

         if (this.numEntries > 0) {

            for (int i = 0; i < this.arrAttrs.Length; i++) {
               this.arrAttrs[i].Init(default(string), default(string), default(string), default(int));
               this.arrAttrs[i].Init(default(string));
            }

            this.numEntries = default(int);
            this.idxLastName = default(int);
            this.hashCodeUnion = default(int);
         }
      }

      struct AttrNameVal {

         string localName;
         string prefix;
         string namespaceName;
         string text;
         int hashCode;
         int nextNameIndex;

         public string LocalName => this.localName;

         public string Prefix => this.prefix;

         public string Namespace => this.namespaceName;

         public string Text => this.text;

         public int NextNameIndex {
            get { return this.nextNameIndex; }
            set { this.nextNameIndex = value; }
         }

         /// <summary>
         /// Cache an attribute's name and type.
         /// </summary>

         public void Init(string prefix, string localName, string ns, int hashCode) {
            this.localName = localName;
            this.prefix = prefix;
            this.namespaceName = ns;
            this.hashCode = hashCode;
            this.nextNameIndex = 0;
         }

         /// <summary>
         /// Cache all or part of the attribute's string value.
         /// </summary>

         public void Init(string text) {
            this.text = text;
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
