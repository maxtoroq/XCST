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

#region DisplayNameExtensions is based on code from ASP.NET Web Stack
// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.
#endregion

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Web.Mvc;

namespace Xcst.Web.Mvc.Html {

   /// <exclude/>
   public static class DisplayNameExtensions {

      public static string DisplayName(this HtmlHelper html, string expression) {

         ModelMetadata metadata = ModelMetadata.FromStringExpression(expression, html.ViewData);

         return DisplayNameHelper(metadata, expression);
      }

      [SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures", Justification = "This is an appropriate nesting of generic types")]
      public static string DisplayNameFor<TModel, TValue>(this HtmlHelper<IEnumerable<TModel>> html,
                                                          Expression<Func<TModel, TValue>> expression) {

         ModelMetadata metadata = ModelMetadata.FromLambdaExpression(expression, new ViewDataDictionary<TModel>());
         string expressionString = ExpressionHelper.GetExpressionText(expression);

         return DisplayNameHelper(metadata, expressionString);
      }

      [SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures", Justification = "This is an appropriate nesting of generic types")]
      public static string DisplayNameFor<TModel, TValue>(this HtmlHelper<TModel> html,
                                                          Expression<Func<TModel, TValue>> expression) {

         ModelMetadata metadata = ModelMetadata.FromLambdaExpression(expression, html.ViewData);
         string expressionString = ExpressionHelper.GetExpressionText(expression);

         return DisplayNameHelper(metadata, expressionString);
      }

      public static string DisplayNameForModel(this HtmlHelper html) {
         return DisplayNameHelper(html.ViewData.ModelMetadata, String.Empty);
      }

      internal static string DisplayNameHelper(ModelMetadata metadata, string htmlFieldName) {

         // We don't call ModelMetadata.GetDisplayName here because we want to fall back to the field name rather than the ModelType.
         // This is similar to how the LabelHelpers get the text of a label.

         string resolvedDisplayName = metadata.DisplayName
            ?? metadata.PropertyName
            ?? htmlFieldName.Split('.').Last();

         return resolvedDisplayName;
      }
   }
}
