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

#region LabelExtensions is based on code from ASP.NET Web Stack
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
   public static class LabelExtensions {

      public static void Label(this HtmlHelper html,
                               XcstWriter output,
                               string expression,
                               string labelText = null,
                               IDictionary<string, object> htmlAttributes = null) {

         ModelMetadata metadata = ModelMetadata.FromStringExpression(expression, html.ViewData);

         LabelHelper(html, output, metadata, expression, labelText, htmlAttributes);
      }

      [SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures", Justification = "This is an appropriate nesting of generic types")]
      public static void LabelFor<TModel, TValue>(this HtmlHelper<TModel> html,
                                                  XcstWriter output,
                                                  Expression<Func<TModel, TValue>> expression,
                                                  string labelText = null,
                                                  IDictionary<string, object> htmlAttributes = null) {

         ModelMetadata metadata = ModelMetadata.FromLambdaExpression(expression, html.ViewData);
         string expressionString = ExpressionHelper.GetExpressionText(expression);

         LabelHelper(html, output, metadata, expressionString, labelText, htmlAttributes);
      }

      public static void LabelForModel(this HtmlHelper html,
                                       XcstWriter output,
                                       string labelText = null,
                                       IDictionary<string, object> htmlAttributes = null) {

         LabelHelper(html, output, html.ViewData.ModelMetadata, String.Empty, labelText, htmlAttributes);
      }

      public static void LabelHelper(HtmlHelper html,
                                     XcstWriter output,
                                     ModelMetadata metadata,
                                     string expression,
                                     string labelText = null,
                                     IDictionary<string, object> htmlAttributes = null) {

         string htmlFieldName = expression;
         string resolvedLabelText = labelText ?? metadata.DisplayName ?? metadata.PropertyName ?? htmlFieldName.Split('.').Last();

         if (String.IsNullOrEmpty(resolvedLabelText)) {
            return;
         }

         output.WriteStartElement("label");

         HtmlAttributesMerger.Create(htmlAttributes)
            .AddDontReplace("for", TagBuilder.CreateSanitizedId(html.ViewContext.ViewData.TemplateInfo.GetFullHtmlFieldName(htmlFieldName)))
            .WriteTo(output);

         output.WriteString(resolvedLabelText);
         output.WriteEndElement();
      }
   }
}
