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

#region DefaultDisplayTemplates is based on code from ASP.NET Web Stack
// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.
#endregion

using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Web.Mvc;
using System.Web.UI.WebControls;
using Xcst.Runtime;

namespace Xcst.Web.Mvc.Html {

   static class DefaultDisplayTemplates {

      public static void BooleanTemplate(HtmlHelper html, DynamicContext context) {

         XcstWriter output = context.Output;

         bool? value = null;

         if (html.ViewContext.ViewData.Model != null) {
            value = Convert.ToBoolean(html.ViewContext.ViewData.Model, CultureInfo.InvariantCulture);
         }

         if (html.ViewContext.ViewData.ModelMetadata.IsNullableValueType) {

            output.WriteStartElement("select");

            string className = DefaultEditorTemplates.GetEditorCssClass(new EditorInfo("Boolean", "select"), "list-box tri-state");

            HtmlAttributesMerger.Create()
               .AddCssClass(className)
               .WriteTo(output);

            output.WriteAttributeString("disabled", "disabled");

            foreach (SelectListItem item in DefaultEditorTemplates.TriStateValues(value)) {
               SelectExtensions.ListItemToOption(output, item);
            }

            output.WriteEndElement();

         } else {

            output.WriteStartElement("input");
            output.WriteAttributeString("type", "checkbox");

            string className = DefaultEditorTemplates.GetEditorCssClass(new EditorInfo("Boolean", "input", InputType.CheckBox), "check-box");

            HtmlAttributesMerger.Create()
               .AddCssClass(className)
               .WriteTo(output);

            output.WriteAttributeString("disabled", "disabled");

            if (value.GetValueOrDefault()) {
               output.WriteAttributeString("checked", "checked");
            }

            output.WriteEndElement();
         }
      }

      public static void CollectionTemplate(HtmlHelper html, DynamicContext context) {
         CollectionTemplate(html, context, TemplateHelpers.TemplateHelper);
      }

      internal static void CollectionTemplate(HtmlHelper html, DynamicContext context, TemplateHelpers.TemplateHelperDelegate templateHelper) {

         object model = html.ViewContext.ViewData.ModelMetadata.Model;

         if (model == null) {
            return;
         }

         IEnumerable collection = model as IEnumerable;

         if (collection == null) {
            throw new InvalidOperationException($"The Collection template was used with an object of type '{model.GetType().FullName}', which does not implement System.IEnumerable.");
         }

         Type typeInCollection = typeof(string);
         Type genericEnumerableType = TypeHelpers.ExtractGenericInterface(collection.GetType(), typeof(IEnumerable<>));

         if (genericEnumerableType != null) {
            typeInCollection = genericEnumerableType.GetGenericArguments()[0];
         }

         bool typeInCollectionIsNullableValueType = TypeHelpers.IsNullableValueType(typeInCollection);

         string oldPrefix = html.ViewContext.ViewData.TemplateInfo.HtmlFieldPrefix;

         try {

            html.ViewContext.ViewData.TemplateInfo.HtmlFieldPrefix = String.Empty;

            string fieldNameBase = oldPrefix;
            int index = 0;

            foreach (object item in collection) {

               Type itemType = typeInCollection;

               if (item != null && !typeInCollectionIsNullableValueType) {
                  itemType = item.GetType();
               }

               ModelMetadata metadata = ModelMetadataProviders.Current.GetMetadataForType(() => item, itemType);
               string fieldName = String.Format(CultureInfo.InvariantCulture, "{0}[{1}]", fieldNameBase, index++);
               templateHelper(html, context, metadata, fieldName, null /* templateName */, DataBoundControlMode.ReadOnly, null /* additionalViewData */);
            }

         } finally {
            html.ViewContext.ViewData.TemplateInfo.HtmlFieldPrefix = oldPrefix;
         }
      }

      public static void DecimalTemplate(HtmlHelper html, DynamicContext context) {

         if (html.ViewContext.ViewData.TemplateInfo.FormattedModelValue == html.ViewContext.ViewData.ModelMetadata.Model) {
            html.ViewContext.ViewData.TemplateInfo.FormattedModelValue = context.Output.SimpleContent.Format("{0:0.00}", html.ViewContext.ViewData.ModelMetadata.Model);
         }

         StringTemplate(html, context);
      }

      public static void EmailAddressTemplate(HtmlHelper html, DynamicContext context) {

         XcstWriter output = context.Output;

         output.WriteStartElement("a");
         output.WriteAttributeString("href", "mailto:" + Convert.ToString(html.ViewContext.ViewData.Model, CultureInfo.InvariantCulture));
         output.WriteString(html.ViewContext.ViewData.TemplateInfo.FormattedModelValue);
         output.WriteEndElement();
      }

      public static void HiddenInputTemplate(HtmlHelper html, DynamicContext context) {

         if (html.ViewContext.ViewData.ModelMetadata.HideSurroundingHtml) {
            return;
         }

         StringTemplate(html, context);
      }

      public static void HtmlTemplate(HtmlHelper html, DynamicContext context) {

         XcstWriter output = context.Output;

         output.WriteRaw(output.SimpleContent.Convert(html.ViewContext.ViewData.TemplateInfo.FormattedModelValue));
      }

      public static void ObjectTemplate(HtmlHelper html, DynamicContext context) {
         ObjectTemplate(html, context, TemplateHelpers.TemplateHelper);
      }

      internal static void ObjectTemplate(HtmlHelper html, DynamicContext context, TemplateHelpers.TemplateHelperDelegate templateHelper) {

         XcstWriter output = context.Output;
         ViewDataDictionary viewData = html.ViewContext.ViewData;
         TemplateInfo templateInfo = viewData.TemplateInfo;
         ModelMetadata modelMetadata = viewData.ModelMetadata;

         if (modelMetadata.Model == null) {
            // DDB #225237
            output.WriteString(modelMetadata.NullDisplayText);
            return;
         }

         if (templateInfo.TemplateDepth > 1) {

            // DDB #224751
            string text = modelMetadata.SimpleDisplayText;

            if (modelMetadata.HtmlEncode) {
               output.WriteString(text);
            } else {
               output.WriteRaw(text);
            }

            return;
         }

         Action<DynamicContext> memberTemplate = viewData[DefaultEditorTemplates.MemberTemplateKey] as Action<DynamicContext>;

         foreach (ModelMetadata propertyMetadata in modelMetadata.Properties.Where(pm => ShouldShow(pm, templateInfo))) {

            if (!propertyMetadata.HideSurroundingHtml) {

               if (memberTemplate != null) {
                  memberTemplate(new DynamicContext(context).WithParam("member", propertyMetadata));
                  continue;
               }

               output.WriteStartElement("div");
               output.WriteAttributeString("class", "display-label");
               output.WriteString(propertyMetadata.GetDisplayName() ?? "");
               output.WriteEndElement();

               output.WriteStartElement("div");
               output.WriteAttributeString("class", "display-field");
            }

            templateHelper(html, context, propertyMetadata, propertyMetadata.PropertyName, null /* templateName */, DataBoundControlMode.ReadOnly, null /* additionalViewData */);

            if (!propertyMetadata.HideSurroundingHtml) {
               output.WriteEndElement(); // </div>
            }
         }
      }

      static bool ShouldShow(ModelMetadata metadata, TemplateInfo templateInfo) {

         return metadata.ShowForDisplay
            && metadata.ModelType != typeof(EntityState)
            && !metadata.IsComplexType
            && !templateInfo.Visited(metadata);
      }

      public static void StringTemplate(HtmlHelper html, DynamicContext context) {
         context.Output.WriteString(html.ViewContext.ViewData.TemplateInfo.FormattedModelValue);
      }

      public static void UrlTemplate(HtmlHelper html, DynamicContext context) {

         XcstWriter output = context.Output;

         output.WriteStartElement("a");
         output.WriteAttributeString("href", Convert.ToString(html.ViewContext.ViewData.Model, CultureInfo.InvariantCulture));
         output.WriteString(html.ViewContext.ViewData.TemplateInfo.FormattedModelValue);
         output.WriteEndElement();
      }

      public static void ImageUrlTemplate(HtmlHelper html, DynamicContext context) {

         XcstWriter output = context.Output;

         if (html.ViewContext.ViewData.Model != null) {
            output.WriteStartElement("img");
            output.WriteAttributeString("src", Convert.ToString(html.ViewContext.ViewData.Model, CultureInfo.InvariantCulture));
            output.WriteEndElement();
         }
      }
   }
}
