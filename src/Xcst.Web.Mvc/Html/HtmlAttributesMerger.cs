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
using System.Collections.Generic;
using System.Web.Mvc;

namespace Xcst.Web.Mvc.Html {

   /// <exclude/>
   public class HtmlAttributesMerger {

      public IDictionary<string, object> Attributes { get; }

      public static HtmlAttributesMerger Create() {
         return new HtmlAttributesMerger(new Dictionary<string, object>());
      }

      public static HtmlAttributesMerger Create(object htmlAttributes) {
         return new HtmlAttributesMerger(HtmlHelper.AnonymousObjectToHtmlAttributes(htmlAttributes));
      }

      public static HtmlAttributesMerger Create(IDictionary<string, object> htmlAttributes) {

         if (htmlAttributes == null) {
            return Create();
         }

         return new HtmlAttributesMerger(new Dictionary<string, object>(htmlAttributes));
      }

      private HtmlAttributesMerger(IDictionary<string, object> htmlAttributes) {

         if (htmlAttributes == null) throw new ArgumentNullException(nameof(htmlAttributes));

         this.Attributes = htmlAttributes;
      }

      public HtmlAttributesMerger AddCssClass(string cssClass) {

         this.Attributes.AddCssClass(cssClass);
         return this;
      }

      public HtmlAttributesMerger AddDontReplace(string key, string value, bool omitIfNull = false) {

         bool exists = this.Attributes.ContainsKey(key);

         if (!exists) {
            if (value != null
               || !omitIfNull) {

               this.Attributes[key] = value;
            }
         }

         return this;
      }

      public HtmlAttributesMerger AddReplace(string key, string value, bool removeIfNull = false) {

         bool exists = this.Attributes.ContainsKey(key);

         if (value == null
            && removeIfNull) {

            this.Attributes.Remove(key);
         } else {

            this.Attributes[key] = value;
         }

         return this;
      }

      public HtmlAttributesMerger MergeAttribute(string key, object value, bool replaceExisting) {

         if (String.IsNullOrEmpty(key)) throw new ArgumentNullException(nameof(key));

         if (replaceExisting || !this.Attributes.ContainsKey(key)) {
            this.Attributes[key] = value;
         }

         return this;
      }

      public HtmlAttributesMerger MergeAttributes<TValue>(IDictionary<string, TValue> attributes, bool replaceExisting) {

         if (attributes != null) {
            foreach (var entry in attributes) {
               MergeAttribute(entry.Key, entry.Value, replaceExisting);
            }
         }

         return this;
      }

      internal HtmlAttributesMerger GenerateId(string name) {

         if (!this.Attributes.ContainsKey("id")) {

            string sanitizedId = TagBuilder.CreateSanitizedId(name);

            if (!String.IsNullOrEmpty(sanitizedId)) {
               Attributes["id"] = sanitizedId;
            }
         }

         return this;
      }

      internal void WriteTo(XcstWriter output) {

         foreach (var item in this.Attributes) {
            output.WriteAttributeString(item.Key, output.SimpleContent.Convert(item.Value));
         }
      }
   }
}
