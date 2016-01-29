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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using Xcst.Web.Mvc.Html;

namespace Xcst.Web.Mvc {

   public class ModelHelper {

      public HtmlHelper Html { get; }

      public object Model => Html.ViewData.Model;

      public ModelMetadata Metadata => Html.ViewData.ModelMetadata;

      public static ModelHelper<TModel> ForModel<TModel>(
            ModelHelper currentHelper,
            TModel model,
            string htmlFieldPrefix = null,
            object additionalViewData = null) {

         if (currentHelper == null) throw new ArgumentNullException(nameof(currentHelper));

         HtmlHelper currentHtml = currentHelper.Html;
         ViewDataDictionary currentViewData = currentHtml.ViewData;

         // Cannot call new ViewDataDictionary<TModel>(currentViewData)
         // because currentViewData.Model might be incompatible with TModel

         var tempDictionary = new ViewDataDictionary(currentViewData) {
            Model = model
         };

         var container = new ViewDataContainer {
            ViewData = new ViewDataDictionary<TModel>(tempDictionary) {

               // setting new TemplateInfo clears VisitedObjects cache
               TemplateInfo = new TemplateInfo {
                  HtmlFieldPrefix = currentViewData.TemplateInfo.HtmlFieldPrefix
               }
            }
         };

         if (!String.IsNullOrEmpty(htmlFieldPrefix)) {

            TemplateInfo templateInfo = container.ViewData.TemplateInfo;
            templateInfo.HtmlFieldPrefix = templateInfo.GetFullHtmlFieldName(htmlFieldPrefix);
         }

         if (additionalViewData != null) {

            IDictionary<string, object> additionalParams = additionalViewData as IDictionary<string, object>
               ?? TypeHelpers.ObjectToDictionary(additionalViewData);

            foreach (var kvp in additionalParams) {
               container.ViewData[kvp.Key] = kvp.Value;
            }
         }

         ViewContext currentViewContext = currentHtml.ViewContext;

         // new ViewContext resets FormContext

         var newViewContext = new ViewContext(
            currentViewContext.Controller.ControllerContext,
            currentViewContext.View,
            container.ViewData,
            currentViewContext.TempData,
            currentViewContext.Writer
         );

         var html = new HtmlHelper<TModel>(newViewContext, container, currentHtml.RouteCollection);

         return new ModelHelper<TModel>(html);
      }

      public static ModelHelper ForProperty(ModelHelper currentHelper, ModelMetadata metadata) {

         if (currentHelper == null) throw new ArgumentNullException(nameof(currentHelper));
         if (metadata == null) throw new ArgumentNullException(nameof(metadata));

         HtmlHelper currentHtml = currentHelper.Html;
         ViewDataDictionary currentViewData = currentHtml.ViewData;

         var container = new ViewDataContainer {
            ViewData = new ViewDataDictionary(currentViewData) {
               Model = metadata.Model,
               ModelMetadata = metadata,
               TemplateInfo = new TemplateInfo {
                  HtmlFieldPrefix = currentViewData.TemplateInfo.GetFullHtmlFieldName(metadata.PropertyName)
               }
            }
         };

         // setting new TemplateInfo clears VisitedObjects cache, need to restore it
         currentViewData.TemplateInfo.VisitedObjects(new HashSet<object>(currentViewData.TemplateInfo.VisitedObjects()));

         var html = new HtmlHelper(currentHtml.ViewContext, container, currentHtml.RouteCollection);

         return new ModelHelper(html);
      }

      public ModelHelper(HtmlHelper htmlHelper) {

         if (htmlHelper == null) throw new ArgumentNullException(nameof(htmlHelper));

         this.Html = htmlHelper;
      }

      public string DisplayName() {
         return DisplayNameHelper(this.Html.ViewData.ModelMetadata, String.Empty);
      }

      public string DisplayName(string name) {

         ModelMetadata metadata = ModelMetadata.FromStringExpression(name, this.Html.ViewData);

         return DisplayNameHelper(metadata, name);
      }

      internal static string DisplayNameHelper(ModelMetadata metadata, string htmlFieldName) {

         // We don't call ModelMetadata.GetDisplayName here because we want to fall back to the field name rather than the ModelType.
         // This is similar to how the LabelHelpers get the text of a label.

         string resolvedDisplayName = metadata.DisplayName
            ?? metadata.PropertyName
            ?? htmlFieldName.Split('.').Last();

         return resolvedDisplayName;
      }

      public string FieldId() {
         return FieldId(String.Empty);
      }

      public string FieldId(string name) {
         return this.Html.ViewData.TemplateInfo.GetFullHtmlFieldId(name);
      }

      public string FieldName() {
         return FieldName(String.Empty);
      }

      public string FieldName(string name) {
         return this.Html.ViewData.TemplateInfo.GetFullHtmlFieldName(name);
      }

      public string FieldValue() {
         return FieldValueHelper(String.Empty, value: null, format: this.Metadata.EditFormatString, useViewData: true);
      }

      public string FieldValue(string name) {

         if (name == null) throw new ArgumentNullException(nameof(name));

         return FieldValueHelper(name, value: null, format: this.Metadata.EditFormatString, useViewData: true);
      }

      public string FieldValue(string name, string format) {

         if (name == null) throw new ArgumentNullException(nameof(name));

         return FieldValueHelper(name, value: null, format: format, useViewData: true);
      }

      internal string FieldValueHelper(string name, object value, string format, bool useViewData) {

         string fullName = this.Html.ViewData.TemplateInfo.GetFullHtmlFieldName(name);
         string attemptedValue = (string)this.Html.GetModelStateValue(fullName, typeof(string));
         string resolvedValue;

         if (attemptedValue != null) {

            // case 1: if ModelState has a value then it's already formatted so ignore format string

            resolvedValue = attemptedValue;

         } else if (useViewData) {

            if (name.Length == 0) {

               // case 2(a): format the value from ModelMetadata for the current model

               ModelMetadata metadata = ModelMetadata.FromStringExpression(String.Empty, this.Html.ViewData);
               resolvedValue = this.Html.FormatValue(metadata.Model, format);

            } else {

               // case 2(b): format the value from ViewData

               resolvedValue = this.Html.EvalString(name, format);
            }
         } else {

            // case 3: format the explicit value from ModelMetadata

            resolvedValue = this.Html.FormatValue(value, format);
         }

         return resolvedValue;
      }

      public virtual void SetModel(object value) {
         this.Html.ViewData.Model = value;
      }

      class ViewDataContainer : IViewDataContainer {

         public ViewDataDictionary ViewData { get; set; }
      }
   }
}
