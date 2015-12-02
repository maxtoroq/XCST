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

#region ValueExtensions is based on code from ASP.NET Web Stack
// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.
#endregion

using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Web.Mvc;

namespace Xcst.Web.Mvc.Html {

   /// <exclude/>
   public static class ValueExtensions {

      public static string Value(this HtmlHelper html, string name, string format = null) {

         if (name == null) throw new ArgumentNullException(nameof(name));

         return ValueForHelper(html, name, value: null, format: format, useViewData: true);
      }

      [SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures", Justification = "This is an appropriate nesting of generic types")]
      public static string ValueFor<TModel, TProperty>(this HtmlHelper<TModel> html,
                                                       Expression<Func<TModel, TProperty>> expression,
                                                       string format = null) {

         ModelMetadata metadata = ModelMetadata.FromLambdaExpression(expression, html.ViewData);
         string expressionString = ExpressionHelper.GetExpressionText(expression);

         return ValueForHelper(html, expressionString, metadata.Model, format, useViewData: false);
      }

      public static string ValueForModel(this HtmlHelper html, string format = null) {
         return Value(html, String.Empty, format);
      }

      internal static string ValueForHelper(HtmlHelper html, string name, object value, string format, bool useViewData) {

         string fullName = html.ViewContext.ViewData.TemplateInfo.GetFullHtmlFieldName(name);
         string attemptedValue = (string)html.GetModelStateValue(fullName, typeof(string));
         string resolvedValue;

         if (attemptedValue != null) {

            // case 1: if ModelState has a value then it's already formatted so ignore format string

            resolvedValue = attemptedValue;

         } else if (useViewData) {

            if (name.Length == 0) {

               // case 2(a): format the value from ModelMetadata for the current model

               ModelMetadata metadata = ModelMetadata.FromStringExpression(String.Empty, html.ViewContext.ViewData);
               resolvedValue = html.FormatValue(metadata.Model, format);

            } else {

               // case 2(b): format the value from ViewData

               resolvedValue = html.EvalString(name, format);
            }
         } else {

            // case 3: format the explicit value from ModelMetadata

            resolvedValue = html.FormatValue(value, format);
         }

         return resolvedValue;
      }
   }
}
