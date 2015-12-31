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

#region TemplateHelpers is based on code from ASP.NET Web Stack
// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.
#endregion

using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Web;
using System.Web.Mvc;
using System.Web.UI.WebControls;
using Xcst.Runtime;

namespace Xcst.Web.Mvc.Html {

   static class TemplateHelpers {

      static readonly Dictionary<DataBoundControlMode, string> _modeViewPaths =
         new Dictionary<DataBoundControlMode, string> {
            { DataBoundControlMode.ReadOnly, "DisplayTemplates" },
            { DataBoundControlMode.Edit, "EditorTemplates" }
         };

      static readonly Dictionary<string, Action<HtmlHelper, DynamicContext>> _defaultDisplayActions =
         new Dictionary<string, Action<HtmlHelper, DynamicContext>>(StringComparer.OrdinalIgnoreCase) {
            { "EmailAddress", DefaultDisplayTemplates.EmailAddressTemplate },
            { "HiddenInput", DefaultDisplayTemplates.HiddenInputTemplate },
            { "Html", DefaultDisplayTemplates.HtmlTemplate },
            { "Text", DefaultDisplayTemplates.StringTemplate },
            { "Url", DefaultDisplayTemplates.UrlTemplate },
            { "ImageUrl", DefaultDisplayTemplates.ImageUrlTemplate },
            { "Collection", DefaultDisplayTemplates.CollectionTemplate },
            { typeof(bool).Name, DefaultDisplayTemplates.BooleanTemplate },
            { typeof(decimal).Name, DefaultDisplayTemplates.DecimalTemplate },
            { typeof(string).Name, DefaultDisplayTemplates.StringTemplate },
            { typeof(object).Name, DefaultDisplayTemplates.ObjectTemplate },
         };

      static readonly Dictionary<string, Action<HtmlHelper, DynamicContext>> _defaultEditorActions =
         new Dictionary<string, Action<HtmlHelper, DynamicContext>>(StringComparer.OrdinalIgnoreCase) {
            { "HiddenInput", DefaultEditorTemplates.HiddenInputTemplate },
            { "MultilineText", DefaultEditorTemplates.MultilineTextTemplate },
            { "Password", DefaultEditorTemplates.PasswordTemplate },
            { "Text", DefaultEditorTemplates.TextTemplate },
            { "Collection", DefaultEditorTemplates.CollectionTemplate },
            { "PhoneNumber", DefaultEditorTemplates.PhoneNumberInputTemplate },
            { "Url", DefaultEditorTemplates.UrlInputTemplate },
            { "EmailAddress", DefaultEditorTemplates.EmailAddressInputTemplate },
            { "DateTime", DefaultEditorTemplates.DateTimeInputTemplate },
            { "DateTime-local", DefaultEditorTemplates.DateTimeLocalInputTemplate },
            { "Date", DefaultEditorTemplates.DateInputTemplate },
            { "Time", DefaultEditorTemplates.TimeInputTemplate },
            { "Upload", DefaultEditorTemplates.UploadTemplate },
            { typeof(Color).Name, DefaultEditorTemplates.ColorInputTemplate },
            { typeof(byte).Name, DefaultEditorTemplates.ByteInputTemplate },
            { typeof(sbyte).Name, DefaultEditorTemplates.SByteInputTemplate },
            { typeof(int).Name, DefaultEditorTemplates.Int32InputTemplate },
            { typeof(uint).Name, DefaultEditorTemplates.UInt32InputTemplate },
            { typeof(long).Name, DefaultEditorTemplates.Int64InputTemplate },
            { typeof(ulong).Name, DefaultEditorTemplates.UInt64InputTemplate },
            { typeof(bool).Name, DefaultEditorTemplates.BooleanTemplate },
            { typeof(decimal).Name, DefaultEditorTemplates.DecimalTemplate },
            { typeof(string).Name, DefaultEditorTemplates.StringTemplate },
            { typeof(object).Name, DefaultEditorTemplates.ObjectTemplate },
         };

      static string CacheItemId = Guid.NewGuid().ToString();

      internal delegate void ExecuteTemplateDelegate(HtmlHelper html, DynamicContext context, ViewDataDictionary viewData, string templateName,
                                                     DataBoundControlMode mode, GetViewNamesDelegate getViewNames,
                                                     GetDefaultActionsDelegate getDefaultActions);

      internal delegate Dictionary<string, Action<HtmlHelper, DynamicContext>> GetDefaultActionsDelegate(DataBoundControlMode mode);

      internal delegate IEnumerable<string> GetViewNamesDelegate(ModelMetadata metadata, params string[] templateHints);

      internal delegate void TemplateHelperDelegate(HtmlHelper html, DynamicContext context, ModelMetadata metadata, string htmlFieldName,
                                                    string templateName, DataBoundControlMode mode, object additionalViewData);

      static void ExecuteTemplate(HtmlHelper html, DynamicContext context, ViewDataDictionary viewData, string templateName, DataBoundControlMode mode, GetViewNamesDelegate getViewNames, GetDefaultActionsDelegate getDefaultActions) {

         Dictionary<string, ActionCacheItem> actionCache = GetActionCache(html);
         Dictionary<string, Action<HtmlHelper, DynamicContext>> defaultActions = getDefaultActions(mode);
         string modeViewPath = _modeViewPaths[mode];

         foreach (string viewName in getViewNames(viewData.ModelMetadata, templateName, viewData.ModelMetadata.TemplateHint, viewData.ModelMetadata.DataTypeName)) {

            string fullViewName = modeViewPath + "/" + viewName;
            ActionCacheItem cacheItem;

            if (actionCache.TryGetValue(fullViewName, out cacheItem)) {

               if (cacheItem != null) {
                  cacheItem.Execute(html, context, viewData);
                  return;
               }

            } else {

               ViewEngineResult viewEngineResult = ViewEngines.Engines.FindPartialView(html.ViewContext, fullViewName);

               if (viewEngineResult.View != null) {

                  actionCache[fullViewName] = new ActionCacheViewItem { ViewName = fullViewName };

                  RenderView(html, context.Output, viewData, viewEngineResult);
                  return;
               }

               Action<HtmlHelper, DynamicContext> defaultAction;

               if (defaultActions.TryGetValue(viewName, out defaultAction)) {

                  actionCache[fullViewName] = new ActionCacheCodeItem { Action = defaultAction };

                  defaultAction(MakeHtmlHelper(html, viewData), context);
                  return;
               }

               actionCache[fullViewName] = null;
            }
         }

         throw new InvalidOperationException($"Unable to locate an appropriate template for type {viewData.ModelMetadata.RealModelType().FullName}.");
      }

      static Dictionary<string, ActionCacheItem> GetActionCache(HtmlHelper html) {

         HttpContextBase context = html.ViewContext.HttpContext;
         Dictionary<string, ActionCacheItem> result;

         if (!context.Items.Contains(CacheItemId)) {
            result = new Dictionary<string, ActionCacheItem>();
            context.Items[CacheItemId] = result;
         } else {
            result = (Dictionary<string, ActionCacheItem>)context.Items[CacheItemId];
         }

         return result;
      }

      static Dictionary<string, Action<HtmlHelper, DynamicContext>> GetDefaultActions(DataBoundControlMode mode) {
         return mode == DataBoundControlMode.ReadOnly ? _defaultDisplayActions : _defaultEditorActions;
      }

      static IEnumerable<string> GetViewNames(ModelMetadata metadata, params string[] templateHints) {

         foreach (string templateHint in templateHints.Where(s => !String.IsNullOrEmpty(s))) {
            yield return templateHint;
         }

         // We don't want to search for Nullable<T>, we want to search for T (which should handle both T and Nullable<T>)
         Type fieldType = Nullable.GetUnderlyingType(metadata.RealModelType()) ?? metadata.RealModelType();

         // TODO: Make better string names for generic types
         yield return fieldType.Name;

         if (fieldType == typeof(string)) {

            // Nothing more to provide
            yield break;

         } else if (!metadata.IsComplexType) {

            // IsEnum is false for the Enum class itself
            if (fieldType.IsEnum) {
               // Same as fieldType.BaseType.Name in this case
               yield return "Enum";
            } else if (fieldType == typeof(DateTimeOffset)) {
               yield return "DateTime";
            }

            yield return "String";

         } else if (fieldType.IsInterface) {

            if (typeof(IEnumerable).IsAssignableFrom(fieldType)) {
               yield return "Collection";
            }

            yield return "Object";

         } else {

            bool isEnumerable = typeof(IEnumerable).IsAssignableFrom(fieldType);

            while (true) {

               fieldType = fieldType.BaseType;

               if (fieldType == null) {
                  break;
               }

               if (isEnumerable && fieldType == typeof(Object)) {
                  yield return "Collection";
               }

               yield return fieldType.Name;
            }
         }
      }

      public static void Template(HtmlHelper html, DynamicContext context, string expression, string templateName, string htmlFieldName, DataBoundControlMode mode, object additionalViewData) {
         Template(html, context, expression, templateName, htmlFieldName, mode, additionalViewData, TemplateHelper);
      }

      internal static void Template(HtmlHelper html, DynamicContext context, string expression, string templateName, string htmlFieldName,
                                    DataBoundControlMode mode, object additionalViewData, TemplateHelperDelegate templateHelper) {

         ModelMetadata metadata = ModelMetadata.FromStringExpression(expression, html.ViewData);

         if (htmlFieldName == null) {
            htmlFieldName = ExpressionHelper.GetExpressionText(expression);
         }

         templateHelper(html, context, metadata, htmlFieldName, templateName, mode, additionalViewData);
      }

      public static void TemplateFor<TContainer, TValue>(this HtmlHelper<TContainer> html, DynamicContext context, Expression<Func<TContainer, TValue>> expression,
                                                         string templateName, string htmlFieldName, DataBoundControlMode mode,
                                                         object additionalViewData) {

         TemplateFor(html, context, expression, templateName, htmlFieldName, mode, additionalViewData, TemplateHelper);
      }

      internal static void TemplateFor<TContainer, TValue>(this HtmlHelper<TContainer> html, DynamicContext context, Expression<Func<TContainer, TValue>> expression,
                                                           string templateName, string htmlFieldName, DataBoundControlMode mode,
                                                           object additionalViewData, TemplateHelperDelegate templateHelper) {

         ModelMetadata metadata = ModelMetadata.FromLambdaExpression(expression, html.ViewData);

         if (htmlFieldName == null) {
            htmlFieldName = ExpressionHelper.GetExpressionText(expression);
         }

         templateHelper(html, context, metadata, htmlFieldName, templateName, mode, additionalViewData);
      }

      public static void TemplateHelper(HtmlHelper html, DynamicContext context, ModelMetadata metadata, string htmlFieldName, string templateName, DataBoundControlMode mode, object additionalViewData) {
         TemplateHelper(html, context, metadata, htmlFieldName, templateName, mode, additionalViewData, ExecuteTemplate);
      }

      internal static void TemplateHelper(HtmlHelper html, DynamicContext context, ModelMetadata metadata, string htmlFieldName, string templateName, DataBoundControlMode mode, object additionalViewData, ExecuteTemplateDelegate executeTemplate) {

         // TODO: Convert Editor into Display if model.IsReadOnly is true? Need to be careful about this because
         // the Model property on the ViewPage/ViewUserControl is get-only, so the type descriptor automatically
         // decorates it with a [ReadOnly] attribute...

         bool displayMode = mode == DataBoundControlMode.ReadOnly;

         if (metadata.ConvertEmptyStringToNull
            && String.Empty.Equals(metadata.Model)) {

            metadata.Model = null;
         }

         object formattedModelValue = metadata.Model;

         if (metadata.Model == null
            && displayMode) {

            formattedModelValue = metadata.NullDisplayText;
         }

         string formatString = (displayMode) ?
            metadata.DisplayFormatString
            : metadata.EditFormatString;

         if (metadata.Model != null
            && !String.IsNullOrEmpty(formatString)) {

            formattedModelValue = (displayMode) ?
               context.Output.SimpleContent.Format(formatString, metadata.Model)
               : String.Format(CultureInfo.CurrentCulture, formatString, metadata.Model);
         }

         // Normally this shouldn't happen, unless someone writes their own custom Object templates which
         // don't check to make sure that the object hasn't already been displayed
         object visitedObjectsKey = metadata.Model
            ?? metadata.RealModelType();

         if (html.ViewDataContainer.ViewData.TemplateInfo.VisitedObjects().Contains(visitedObjectsKey)) {
            // DDB #224750
            return;
         }

         ViewDataDictionary viewData = new ViewDataDictionary(html.ViewDataContainer.ViewData) {
            Model = metadata.Model,
            ModelMetadata = metadata,
            TemplateInfo = new TemplateInfo {
               FormattedModelValue = formattedModelValue,
               HtmlFieldPrefix = html.ViewContext.ViewData.TemplateInfo.GetFullHtmlFieldName(htmlFieldName)
            }
         };

         viewData.TemplateInfo.VisitedObjects(new HashSet<object>(html.ViewContext.ViewData.TemplateInfo.VisitedObjects())); // DDB #224750

         if (additionalViewData != null) {

            IDictionary<string, object> additionalParams = additionalViewData as IDictionary<string, object>
               ?? TypeHelpers.ObjectToDictionary(additionalViewData);

            foreach (KeyValuePair<string, object> kvp in additionalParams) {
               viewData[kvp.Key] = kvp.Value;
            }
         }

         viewData.TemplateInfo.VisitedObjects().Add(visitedObjectsKey); // DDB #224750

         executeTemplate(html, context, viewData, templateName, mode, GetViewNames, GetDefaultActions);
      }

      // Helpers

      static HtmlHelper MakeHtmlHelper(HtmlHelper html, ViewDataDictionary viewData) {

         var newHelper = new HtmlHelper(
            new ViewContext(html.ViewContext, html.ViewContext.View, viewData, html.ViewContext.TempData, html.ViewContext.Writer),
            new ViewDataContainer(viewData));

         newHelper.Html5DateRenderingMode = html.Html5DateRenderingMode;

         return newHelper;
      }

      static void RenderView(HtmlHelper html, XcstWriter output, ViewDataDictionary viewData, ViewEngineResult viewEngineResult) {

         IView view = viewEngineResult.View;
         XcstView xcstView = view as XcstView;

         if (xcstView != null) {
            xcstView.RenderXcstView(new ViewContext(html.ViewContext, view, viewData, html.ViewContext.TempData, html.ViewContext.Writer), output);

         } else {

            using (StringWriter writer = new StringWriter(CultureInfo.InvariantCulture)) {

               view.Render(new ViewContext(html.ViewContext, view, viewData, html.ViewContext.TempData, writer), writer);

               output.WriteRaw(writer.ToString());
            }
         }
      }

      abstract class ActionCacheItem {
         public abstract void Execute(HtmlHelper html, DynamicContext context, ViewDataDictionary viewData);
      }

      class ActionCacheCodeItem : ActionCacheItem {

         public Action<HtmlHelper, DynamicContext> Action { get; set; }

         public override void Execute(HtmlHelper html, DynamicContext context, ViewDataDictionary viewData) {
            Action(MakeHtmlHelper(html, viewData), context);
         }
      }

      class ActionCacheViewItem : ActionCacheItem {

         public string ViewName { get; set; }

         public override void Execute(HtmlHelper html, DynamicContext context, ViewDataDictionary viewData) {

            ViewEngineResult viewEngineResult = ViewEngines.Engines.FindPartialView(html.ViewContext, this.ViewName);

            RenderView(html, context.Output, viewData, viewEngineResult);
         }
      }

      class ViewDataContainer : IViewDataContainer {

         public ViewDataContainer(ViewDataDictionary viewData) {
            ViewData = viewData;
         }

         public ViewDataDictionary ViewData { get; set; }
      }
   }
}
