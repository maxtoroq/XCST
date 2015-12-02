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

#region TextAreaExtensions is based on code from ASP.NET Web Stack
// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.
#endregion

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq.Expressions;
using System.Web.Mvc;
using System.Xml;

namespace Xcst.Web.Mvc.Html {

   /// <exclude/>
   public static class TextAreaExtensions {

      // These values are similar to the defaults used by WebForms
      // when using <asp:TextBox TextMode="MultiLine"> without specifying
      // the Rows and Columns attributes.
      const int TextAreaRows = 2;
      const int TextAreaColumns = 20;

      static Dictionary<string, object> implicitRowsAndColumns = new Dictionary<string, object> {
         { "rows", TextAreaRows.ToString(CultureInfo.InvariantCulture) },
         { "cols", TextAreaColumns.ToString(CultureInfo.InvariantCulture) },
      };

      static Dictionary<string, object> GetRowsAndColumnsDictionary(int rows, int columns) {

         if (rows < 0) throw new ArgumentOutOfRangeException(nameof(rows), "The value must be greater than or equal to zero.");
         if (columns < 0) throw new ArgumentOutOfRangeException(nameof(columns), "The value must be greater than or equal to zero.");

         Dictionary<string, object> result = new Dictionary<string, object>();

         if (rows > 0) {
            result.Add("rows", rows.ToString(CultureInfo.InvariantCulture));
         }

         if (columns > 0) {
            result.Add("cols", columns.ToString(CultureInfo.InvariantCulture));
         }

         return result;
      }

      public static void TextArea(this HtmlHelper htmlHelper,
                                  XmlWriter output,
                                  string name,
                                  string value = null,
                                  IDictionary<string, object> htmlAttributes = null) {

         ModelMetadata metadata = ModelMetadata.FromStringExpression(name, htmlHelper.ViewContext.ViewData);

         if (value != null) {
            metadata.Model = value;
         }

         TextAreaHelper(htmlHelper, output, metadata, name, implicitRowsAndColumns, htmlAttributes);
      }

      public static void TextArea(this HtmlHelper htmlHelper,
                                  XmlWriter output,
                                  string name,
                                  string value,
                                  int rows, int columns,
                                  IDictionary<string, object> htmlAttributes = null) {

         ModelMetadata metadata = ModelMetadata.FromStringExpression(name, htmlHelper.ViewContext.ViewData);

         if (value != null) {
            metadata.Model = value;
         }

         IDictionary<string, object> rowsAndColumns = GetRowsAndColumnsDictionary(rows, columns);

         TextAreaHelper(htmlHelper, output, metadata, name, rowsAndColumns, htmlAttributes);
      }

      [SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures", Justification = "This is an appropriate nesting of generic types")]
      public static void TextAreaFor<TModel, TProperty>(this HtmlHelper<TModel> htmlHelper,
                                                        XmlWriter output,
                                                        Expression<Func<TModel, TProperty>> expression,
                                                        IDictionary<string, object> htmlAttributes = null) {

         if (expression == null) throw new ArgumentNullException(nameof(expression));

         ModelMetadata metadata = ModelMetadata.FromLambdaExpression(expression, htmlHelper.ViewData);
         string expressionString = ExpressionHelper.GetExpressionText(expression);

         TextAreaHelper(htmlHelper, output, metadata, expressionString, implicitRowsAndColumns, htmlAttributes);
      }

      [SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures", Justification = "This is an appropriate nesting of generic types")]
      public static void TextAreaFor<TModel, TProperty>(this HtmlHelper<TModel> htmlHelper,
                                                        XmlWriter output,
                                                        Expression<Func<TModel, TProperty>> expression,
                                                        int rows, int columns,
                                                        IDictionary<string, object> htmlAttributes = null) {

         if (expression == null) throw new ArgumentNullException(nameof(expression));

         ModelMetadata metadata = ModelMetadata.FromLambdaExpression(expression, htmlHelper.ViewData);
         string expressionString = ExpressionHelper.GetExpressionText(expression);
         IDictionary<string, object> rowsAndColumns = GetRowsAndColumnsDictionary(rows, columns);

         TextAreaHelper(htmlHelper, output, metadata, expressionString, rowsAndColumns, htmlAttributes);
      }

      [SuppressMessage("Microsoft.Usage", "CA2208:InstantiateArgumentExceptionsCorrectly", Justification = "If this fails, it is because the string-based version had an empty 'name' parameter")]
      internal static void TextAreaHelper(HtmlHelper htmlHelper,
                                          XmlWriter output,
                                          ModelMetadata modelMetadata,
                                          string name,
                                          IDictionary<string, object> rowsAndColumns,
                                          IDictionary<string, object> htmlAttributes,
                                          string innerHtmlPrefix = null) {

         string fullName = htmlHelper.ViewContext.ViewData.TemplateInfo.GetFullHtmlFieldName(name);

         if (String.IsNullOrEmpty(fullName)) {
            throw new ArgumentNullException(nameof(name));
         }

         output.WriteStartElement("textarea");

         var attribs = HtmlAttributesMerger.Create(htmlAttributes)
            .GenerateId(fullName)
            .AddReplace("name", fullName)
            .MergeAttributes(rowsAndColumns, replaceExisting: rowsAndColumns != implicitRowsAndColumns); // Only force explicit rows/cols

         // If there are any errors for a named field, we add the CSS attribute.
         ModelState modelState;

         if (htmlHelper.ViewData.ModelState.TryGetValue(fullName, out modelState)
            && modelState.Errors.Count > 0) {

            attribs.AddCssClass(HtmlHelper.ValidationInputCssClassName);
         }

         attribs.MergeAttributes(htmlHelper.GetUnobtrusiveValidationAttributes(name, modelMetadata), replaceExisting: false)
            .WriteTo(output);

         string value;

         if (modelState != null && modelState.Value != null) {
            value = modelState.Value.AttemptedValue;

         } else if (modelMetadata.Model != null) {
            value = modelMetadata.Model.ToString();

         } else {
            value = String.Empty;
         }

         // The first newline is always trimmed when a TextArea is rendered, so we add an extra one
         // in case the value being rendered is something like "\r\nHello".
         output.WriteString((innerHtmlPrefix ?? Environment.NewLine) + value);

         output.WriteEndElement();
      }
   }
}
