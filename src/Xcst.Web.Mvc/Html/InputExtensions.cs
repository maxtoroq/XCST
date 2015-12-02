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

#region InputExtensions is based on code from ASP.NET Web Stack
// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.
#endregion

using System;
using System.Collections.Generic;
using System.Data.Linq;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq.Expressions;
using System.Web.Mvc;
using System.Web.Routing;
using System.Xml;

namespace Xcst.Web.Mvc.Html {

   /// <exclude/>
   public static class InputExtensions {

      // CheckBox

      public static void CheckBox(this HtmlHelper htmlHelper,
                                  XmlWriter output,
                                  string name,
                                  IDictionary<string, object> htmlAttributes = null) {

         CheckBoxHelper(htmlHelper, output, default(ModelMetadata), name, isChecked: null, htmlAttributes: htmlAttributes);
      }

      public static void CheckBox(this HtmlHelper htmlHelper,
                                  XmlWriter output,
                                  string name,
                                  bool isChecked,
                                  IDictionary<string, object> htmlAttributes = null) {

         CheckBoxHelper(htmlHelper, output, default(ModelMetadata), name, isChecked, htmlAttributes);
      }

      [SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures", Justification = "This is an appropriate nesting of generic types")]
      public static void CheckBoxFor<TModel>(this HtmlHelper<TModel> htmlHelper,
                                             XmlWriter output,
                                             Expression<Func<TModel, bool>> expression,
                                             IDictionary<string, object> htmlAttributes = null) {

         if (expression == null) throw new ArgumentNullException(nameof(expression));

         ModelMetadata metadata = ModelMetadata.FromLambdaExpression(expression, htmlHelper.ViewData);
         bool? isChecked = null;

         if (metadata.Model != null) {

            bool modelChecked;

            if (Boolean.TryParse(metadata.Model.ToString(), out modelChecked)) {
               isChecked = modelChecked;
            }
         }

         string expressionString = ExpressionHelper.GetExpressionText(expression);

         CheckBoxHelper(htmlHelper, output, metadata, expressionString, isChecked, htmlAttributes);
      }

      static void CheckBoxHelper(HtmlHelper htmlHelper,
                                 XmlWriter output,
                                 ModelMetadata metadata,
                                 string name,
                                 bool? isChecked,
                                 IDictionary<string, object> htmlAttributes) {

         RouteValueDictionary attributes = ToRouteValueDictionary(htmlAttributes);

         bool explicitValue = isChecked.HasValue;

         if (explicitValue) {
            attributes.Remove("checked"); // Explicit value must override dictionary
         }

         InputHelper(htmlHelper,
                     output,
                     InputType.CheckBox,
                     metadata,
                     name,
                     value: "true",
                     useViewData: !explicitValue,
                     isChecked: isChecked ?? false,
                     setId: true,
                     isExplicitValue: false,
                     format: null,
                     htmlAttributes: attributes);
      }

      // Hidden

      public static void Hidden(this HtmlHelper htmlHelper,
                                XmlWriter output,
                                string name,
                                object value = null,
                                IDictionary<string, object> htmlAttributes = null) {

         HiddenHelper(htmlHelper, output, default(ModelMetadata), value, value == null, name, htmlAttributes);
      }

      [SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures", Justification = "This is an appropriate nesting of generic types")]
      public static void HiddenFor<TModel, TProperty>(this HtmlHelper<TModel> htmlHelper,
                                                      XmlWriter output, Expression<Func<TModel, TProperty>> expression,
                                                      IDictionary<string, object> htmlAttributes = null) {

         ModelMetadata metadata = ModelMetadata.FromLambdaExpression(expression, htmlHelper.ViewData);
         string expressionString = ExpressionHelper.GetExpressionText(expression);

         HiddenHelper(htmlHelper, output, metadata, metadata.Model, false, expressionString, htmlAttributes);
      }

      static void HiddenHelper(HtmlHelper htmlHelper,
                               XmlWriter output,
                               ModelMetadata metadata,
                               object value,
                               bool useViewData,
                               string expression,
                               IDictionary<string, object> htmlAttributes) {

         Binary binaryValue = value as Binary;

         if (binaryValue != null) {
            value = binaryValue.ToArray();
         }

         byte[] byteArrayValue = value as byte[];

         if (byteArrayValue != null) {
            value = Convert.ToBase64String(byteArrayValue);
         }

         InputHelper(htmlHelper,
                     output,
                     InputType.Hidden,
                     metadata,
                     expression,
                     value,
                     useViewData,
                     isChecked: false,
                     setId: true,
                     isExplicitValue: true,
                     format: null,
                     htmlAttributes: htmlAttributes);
      }

      public static void HttpMethodOverride(HtmlHelper htmlHelper, XmlWriter output, string httpMethod) {

         if (String.IsNullOrEmpty(httpMethod)) throw new ArgumentNullException(nameof(httpMethod));

         if (String.Equals(httpMethod, "GET", StringComparison.OrdinalIgnoreCase)
            || String.Equals(httpMethod, "POST", StringComparison.OrdinalIgnoreCase)) {

            throw new ArgumentException("The GET and POST HTTP methods are not supported.", nameof(httpMethod));
         }

         output.WriteStartElement("input");
         output.WriteAttributeString("type", "hidden");
         output.WriteAttributeString("name", "X-HTTP-Method-Override");
         output.WriteAttributeString("value", httpMethod);
         output.WriteEndElement();
      }

      // Password

      public static void Password(this HtmlHelper htmlHelper, XmlWriter output, string name, object value = null, IDictionary<string, object> htmlAttributes = null) {
         PasswordHelper(htmlHelper, output, default(ModelMetadata), name, value, htmlAttributes);
      }

      [SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters", Justification = "Users cannot use anonymous methods with the LambdaExpression type")]
      [SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures", Justification = "This is an appropriate nesting of generic types")]
      public static void PasswordFor<TModel, TProperty>(this HtmlHelper<TModel> htmlHelper, XmlWriter output, Expression<Func<TModel, TProperty>> expression, IDictionary<string, object> htmlAttributes = null) {

         if (expression == null) throw new ArgumentNullException(nameof(expression));

         ModelMetadata metadata = ModelMetadata.FromLambdaExpression(expression, htmlHelper.ViewData);
         string expressionString = ExpressionHelper.GetExpressionText(expression);

         PasswordHelper(htmlHelper, output, metadata, expressionString, null /* value */, htmlAttributes);
      }

      static void PasswordHelper(HtmlHelper htmlHelper,
                                 XmlWriter output,
                                 ModelMetadata metadata,
                                 string name,
                                 object value,
                                 IDictionary<string, object> htmlAttributes) {

         InputHelper(htmlHelper,
                     output,
                     InputType.Password,
                     metadata,
                     name,
                     value,
                     useViewData: false,
                     isChecked: false,
                     setId: true,
                     isExplicitValue: true,
                     format: null,
                     htmlAttributes: htmlAttributes);
      }

      // RadioButton

      public static void RadioButton(this HtmlHelper htmlHelper,
                                     XmlWriter output,
                                     string name,
                                     object value,
                                     IDictionary<string, object> htmlAttributes = null) {

         // Determine whether or not to render the checked attribute based on the contents of ViewData.
         string valueString = Convert.ToString(value, CultureInfo.CurrentCulture);
         bool isChecked = (!String.IsNullOrEmpty(name)) && (String.Equals(htmlHelper.EvalString(name), valueString, StringComparison.OrdinalIgnoreCase));

         // checked attributes is implicit, so we need to ensure that the dictionary takes precedence.
         RouteValueDictionary attributes = ToRouteValueDictionary(htmlAttributes);

         if (attributes.ContainsKey("checked")) {
            InputHelper(htmlHelper,
                        output,
                        InputType.Radio,
                        metadata: null,
                        name: name,
                        value: value,
                        useViewData: false,
                        isChecked: false,
                        setId: true,
                        isExplicitValue: true,
                        format: null,
                        htmlAttributes: attributes);
            return;
         }

         RadioButton(htmlHelper, output, name, value, isChecked, htmlAttributes);
      }

      public static void RadioButton(this HtmlHelper htmlHelper,
                                     XmlWriter output,
                                     string name,
                                     object value,
                                     bool isChecked,
                                     IDictionary<string, object> htmlAttributes = null) {

         if (value == null) throw new ArgumentNullException(nameof(value));

         // checked attribute is an explicit parameter so it takes precedence.
         RouteValueDictionary attributes = ToRouteValueDictionary(htmlAttributes);
         attributes.Remove("checked");

         InputHelper(htmlHelper,
                     output,
                     InputType.Radio,
                     metadata: null,
                     name: name,
                     value: value,
                     useViewData: false,
                     isChecked: isChecked,
                     setId: true,
                     isExplicitValue: true,
                     format: null,
                     htmlAttributes: attributes);
      }

      [SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures", Justification = "This is an appropriate nesting of generic types")]
      public static void RadioButtonFor<TModel, TProperty>(this HtmlHelper<TModel> htmlHelper,
                                                           XmlWriter output,
                                                           Expression<Func<TModel, TProperty>> expression,
                                                           object value,
                                                           IDictionary<string, object> htmlAttributes = null) {

         ModelMetadata metadata = ModelMetadata.FromLambdaExpression(expression, htmlHelper.ViewData);
         string expressionString = ExpressionHelper.GetExpressionText(expression);

         RadioButtonHelper(htmlHelper, output, metadata, metadata.Model, expressionString, value, null /* isChecked */, htmlAttributes);
      }

      static void RadioButtonHelper(HtmlHelper htmlHelper,
                                    XmlWriter output,
                                    ModelMetadata metadata,
                                    object model,
                                    string name,
                                    object value,
                                    bool? isChecked,
                                    IDictionary<string, object> htmlAttributes) {

         if (value == null) throw new ArgumentNullException(nameof(value));

         RouteValueDictionary attributes = ToRouteValueDictionary(htmlAttributes);

         bool explicitValue = isChecked.HasValue;

         if (explicitValue) {
            attributes.Remove("checked"); // Explicit value must override dictionary
         } else {

            string valueString = Convert.ToString(value, CultureInfo.CurrentCulture);

            isChecked = model != null
               && !String.IsNullOrEmpty(name)
               && String.Equals(model.ToString(), valueString, StringComparison.OrdinalIgnoreCase);
         }

         InputHelper(htmlHelper,
                     output,
                     InputType.Radio,
                     metadata,
                     name,
                     value,
                     useViewData: false,
                     isChecked: isChecked ?? false,
                     setId: true,
                     isExplicitValue: true,
                     format: null,
                     htmlAttributes: attributes);
      }

      // TextBox

      public static void TextBox(this HtmlHelper htmlHelper,
                                 XmlWriter output,
                                 string name,
                                 object value = null,
                                 string format = null,
                                 IDictionary<string, object> htmlAttributes = null) {

         InputHelper(htmlHelper,
                     output,
                     InputType.Text,
                     metadata: null,
                     name: name,
                     value: value,
                     useViewData: (value == null),
                     isChecked: false,
                     setId: true,
                     isExplicitValue: true,
                     format: format,
                     htmlAttributes: htmlAttributes);
      }

      [SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures", Justification = "This is an appropriate nesting of generic types")]
      public static void TextBoxFor<TModel, TProperty>(this HtmlHelper<TModel> htmlHelper,
                                                       XmlWriter output,
                                                       Expression<Func<TModel, TProperty>> expression,
                                                       string format = null,
                                                       IDictionary<string, object> htmlAttributes = null) {

         ModelMetadata metadata = ModelMetadata.FromLambdaExpression(expression, htmlHelper.ViewData);
         string expressionString = ExpressionHelper.GetExpressionText(expression);

         TextBoxHelper(htmlHelper, output, metadata, metadata.Model, expressionString, format, htmlAttributes);
      }

      static void TextBoxHelper(this HtmlHelper htmlHelper,
                                XmlWriter output,
                                ModelMetadata metadata,
                                object model,
                                string expression,
                                string format,
                                IDictionary<string, object> htmlAttributes) {

         InputHelper(htmlHelper,
                     output,
                     InputType.Text,
                     metadata,
                     expression,
                     model,
                     useViewData: false,
                     isChecked: false,
                     setId: true,
                     isExplicitValue: true,
                     format: format,
                     htmlAttributes: htmlAttributes);
      }

      // Helper methods

      static void InputHelper(HtmlHelper htmlHelper,
                              XmlWriter output,
                              InputType inputType,
                              ModelMetadata metadata,
                              string name,
                              object value,
                              bool useViewData,
                              bool isChecked,
                              bool setId,
                              bool isExplicitValue,
                              string format,
                              IDictionary<string, object> htmlAttributes) {

         string fullName = htmlHelper.ViewContext.ViewData.TemplateInfo.GetFullHtmlFieldName(name);

         if (String.IsNullOrEmpty(fullName)) {
            throw new ArgumentNullException(nameof(name));
         }

         output.WriteStartElement("input");

         var attribs = HtmlAttributesMerger.Create(htmlAttributes)
            .AddDontReplace("type", HtmlHelper.GetInputTypeString(inputType))
            .AddReplace("name", fullName);

         string valueParameter = htmlHelper.FormatValue(value, format);
         bool usedModelState = false;

         switch (inputType) {
            case InputType.CheckBox:

               bool? modelStateWasChecked = htmlHelper.GetModelStateValue(fullName, typeof(bool)) as bool?;

               if (modelStateWasChecked.HasValue) {
                  isChecked = modelStateWasChecked.Value;
                  usedModelState = true;
               }

               goto case InputType.Radio;

            case InputType.Radio:

               if (!usedModelState) {

                  string modelStateValue = htmlHelper.GetModelStateValue(fullName, typeof(string)) as string;

                  if (modelStateValue != null) {
                     isChecked = String.Equals(modelStateValue, valueParameter, StringComparison.Ordinal);
                     usedModelState = true;
                  }
               }

               if (!usedModelState && useViewData) {
                  isChecked = htmlHelper.EvalBoolean(fullName);
               }

               if (isChecked) {
                  attribs.AddDontReplace("checked", "checked");
               }

               attribs.MergeAttribute("value", valueParameter, replaceExisting: isExplicitValue);

               break;

            case InputType.Password:

               if (value != null) {
                  attribs.MergeAttribute("value", valueParameter, replaceExisting: isExplicitValue);
               }

               break;

            default:

               string attemptedValue = (string)htmlHelper.GetModelStateValue(fullName, typeof(string));

               string val = attemptedValue
                  ?? ((useViewData) ? htmlHelper.EvalString(fullName, format) : valueParameter);

               attribs.MergeAttribute("value", val, replaceExisting: isExplicitValue);

               break;
         }

         if (setId) {
            attribs.GenerateId(fullName);
         }

         // If there are any errors for a named field, we add the css attribute.
         ModelState modelState;

         if (htmlHelper.ViewData.ModelState.TryGetValue(fullName, out modelState)) {
            if (modelState.Errors.Count > 0) {
               attribs.AddCssClass(HtmlHelper.ValidationInputCssClassName);
            }
         }

         attribs.MergeAttributes(htmlHelper.GetUnobtrusiveValidationAttributes(name, metadata), replaceExisting: false)
            .WriteTo(output);

         output.WriteEndElement();

         if (inputType == InputType.CheckBox) {

            // Render an additional <input type="hidden".../> for checkboxes. This
            // addresses scenarios where unchecked checkboxes are not sent in the request.
            // Sending a hidden input makes it possible to know that the checkbox was present
            // on the page when the request was submitted.

            output.WriteStartElement("input");
            output.WriteAttributeString("type", HtmlHelper.GetInputTypeString(InputType.Hidden));
            output.WriteAttributeString("name", fullName);
            output.WriteAttributeString("value", "false");
            output.WriteEndElement();
         }
      }

      static RouteValueDictionary ToRouteValueDictionary(IDictionary<string, object> dictionary) {
         return dictionary == null ? new RouteValueDictionary() : new RouteValueDictionary(dictionary);
      }
   }
}
