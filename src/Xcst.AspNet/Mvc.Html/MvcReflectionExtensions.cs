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
using System.Globalization;
using System.Reflection;
using System.Web.Mvc;

namespace Xcst.Web.Mvc.Html {

   static class MvcReflectionExtensions {

      static readonly Func<ModelMetadata, Type> getRealModelType =
         (Func<ModelMetadata, Type>)Delegate.CreateDelegate(typeof(Func<ModelMetadata, Type>), typeof(ModelMetadata).GetProperty("RealModelType", BindingFlags.Instance | BindingFlags.NonPublic).GetGetMethod(nonPublic: true));

      static readonly Func<TemplateInfo, HashSet<object>> getVisitedObjects =
         (Func<TemplateInfo, HashSet<object>>)Delegate.CreateDelegate(typeof(Func<TemplateInfo, HashSet<object>>), typeof(TemplateInfo).GetProperty("VisitedObjects", BindingFlags.Instance | BindingFlags.NonPublic).GetGetMethod(nonPublic: true));

      static readonly Action<TemplateInfo, HashSet<object>> setVisitedObjects =
         (Action<TemplateInfo, HashSet<object>>)Delegate.CreateDelegate(typeof(Action<TemplateInfo, HashSet<object>>), typeof(TemplateInfo).GetProperty("VisitedObjects", BindingFlags.Instance | BindingFlags.NonPublic).GetSetMethod(nonPublic: true));

      public static Type RealModelType(this ModelMetadata metadata) {
         return getRealModelType(metadata);
      }

      public static HashSet<object> VisitedObjects(this TemplateInfo templateInfo) {
         return getVisitedObjects(templateInfo);
      }

      public static void VisitedObjects(this TemplateInfo templateInfo, HashSet<object> value) {
         setVisitedObjects(templateInfo, value);
      }

      public static bool HasNonDefaultEditFormat(this ModelMetadata metadata) {

         return (bool)metadata.GetType()
            .GetProperty("HasNonDefaultEditFormat", BindingFlags.Instance | BindingFlags.NonPublic)
            .GetValue(metadata);
      }

      public static object GetModelStateValue(this HtmlHelper htmlHelper, string key, Type destinationType) {

         ModelState modelState;

         if (htmlHelper.ViewData.ModelState.TryGetValue(key, out modelState)) {
            if (modelState.Value != null) {
               return modelState.Value.ConvertTo(destinationType, null /* culture */);
            }
         }

         return null;
      }

      public static string EvalString(this HtmlHelper htmlHelper, string key) {
         return Convert.ToString(htmlHelper.ViewData.Eval(key), CultureInfo.CurrentCulture);
      }

      public static string EvalString(this HtmlHelper htmlHelper, string key, string format) {
         return Convert.ToString(htmlHelper.ViewData.Eval(key, format), CultureInfo.CurrentCulture);
      }

      public static bool EvalBoolean(this HtmlHelper htmlHelper, string key) {
         return Convert.ToBoolean(htmlHelper.ViewData.Eval(key), CultureInfo.InvariantCulture);
      }

      public static FormContext GetFormContextForClientValidation(this ViewContext viewContext) {
         return (viewContext.ClientValidationEnabled) ? viewContext.FormContext : null;
      }
   }
}
