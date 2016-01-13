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

#region ValidationExtensions is based on code from ASP.NET Web Stack
// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.
#endregion

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Web;
using System.Web.Mvc;
using Xcst.Runtime;

namespace Xcst.Web.Mvc.Html {

   /// <exclude/>
   public static class ValidationExtensions {

      static string _resourceClassKey;

      public static string ResourceClassKey
      {
         get { return _resourceClassKey ?? String.Empty; }
         set { _resourceClassKey = value; }
      }

      static FieldValidationMetadata ApplyFieldValidationMetadata(HtmlHelper htmlHelper, ModelMetadata modelMetadata, string modelName) {

         FormContext formContext = htmlHelper.ViewContext.FormContext;
         FieldValidationMetadata fieldMetadata = formContext.GetValidationMetadataForField(modelName, true /* createIfNotFound */);

         // write rules to context object
         IEnumerable<ModelValidator> validators = ModelValidatorProviders.Providers.GetValidators(modelMetadata, htmlHelper.ViewContext);

         foreach (ModelClientValidationRule rule in validators.SelectMany(v => v.GetClientValidationRules())) {
            fieldMetadata.ValidationRules.Add(rule);
         }

         return fieldMetadata;
      }

      static string GetInvalidPropertyValueResource(HttpContextBase httpContext) {

         string resourceValue = null;

         if (!String.IsNullOrEmpty(ResourceClassKey) && (httpContext != null)) {
            // If the user specified a ResourceClassKey try to load the resource they specified.
            // If the class key is invalid, an exception will be thrown.
            // If the class key is valid but the resource is not found, it returns null, in which
            // case it will fall back to the MVC default error message.
            resourceValue = httpContext.GetGlobalResourceObject(ResourceClassKey, "InvalidPropertyValue", CultureInfo.CurrentUICulture) as string;
         }
         return resourceValue ?? "The value '{0}' is invalid.";
      }

      static string GetUserErrorMessageOrDefault(HttpContextBase httpContext, ModelError error, ModelState modelState) {

         if (!String.IsNullOrEmpty(error.ErrorMessage)) {
            return error.ErrorMessage;
         }

         if (modelState == null) {
            return null;
         }

         string attemptedValue = (modelState.Value != null) ? modelState.Value.AttemptedValue : null;
         return String.Format(CultureInfo.CurrentCulture, GetInvalidPropertyValueResource(httpContext), attemptedValue);
      }

      public static void Validate(this HtmlHelper htmlHelper, string modelName) {

         if (modelName == null) throw new ArgumentNullException(nameof(modelName));

         ValidateHelper(htmlHelper,
                        ModelMetadata.FromStringExpression(modelName, htmlHelper.ViewData),
                        modelName);
      }

      [SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures", Justification = "This is an appropriate nesting of generic types")]
      public static void ValidateFor<TModel, TProperty>(this HtmlHelper<TModel> htmlHelper, Expression<Func<TModel, TProperty>> expression) {
         ValidateHelper(htmlHelper,
                        ModelMetadata.FromLambdaExpression(expression, htmlHelper.ViewData),
                        ExpressionHelper.GetExpressionText(expression));
      }

      static void ValidateHelper(HtmlHelper htmlHelper, ModelMetadata modelMetadata, string expression) {

         FormContext formContext = htmlHelper.ViewContext.GetFormContextForClientValidation();

         if (formContext == null
            || htmlHelper.ViewContext.UnobtrusiveJavaScriptEnabled) {

            return; // nothing to do
         }

         string modelName = htmlHelper.ViewData.TemplateInfo.GetFullHtmlFieldName(expression);

         ApplyFieldValidationMetadata(htmlHelper, modelMetadata, modelName);
      }

      /// <summary>
      /// Displays a validation message if an error exists for the specified entry in the
      /// <see cref="ModelStateDictionary"/> object.
      /// </summary>
      /// <param name="htmlHelper">The HTML helper instance that this method operates on.</param>
      /// <param name="modelName">The name of the model object being validated.</param>
      /// <param name="validationMessage">The message to display if the specified entry contains an error.</param>
      /// <param name="htmlAttributes">An <see cref="IDictionary{TKey,TValue}"/> that contains the HTML attributes
      /// for the element.</param>
      /// <param name="tag">The tag to be set for the wrapping HTML element of the validation message.</param>
      /// <returns>null if the model object is valid and client-side validation is disabled.
      /// Otherwise, a <paramref name="tag"/> element that contains an error message.</returns>
      [SuppressMessage("Microsoft.Naming", "CA1719:ParameterNamesShouldNotMatchMemberNames", Justification = "'validationMessage' refers to the message that will be rendered by the ValidationMessage helper.")]
      public static void ValidationMessage(this HtmlHelper htmlHelper,
                                           DynamicContext context,
                                           string modelName,
                                           string validationMessage = null,
                                           IDictionary<string, object> htmlAttributes = null,
                                           string tag = null) {

         if (modelName == null) throw new ArgumentNullException(nameof(modelName));

         ModelMetadata metadata = ModelMetadata.FromStringExpression(modelName, htmlHelper.ViewData);

         ValidationMessageHelper(htmlHelper, context, metadata, modelName, validationMessage, htmlAttributes, tag);
      }

      /// <summary>
      /// Returns the HTML markup for a validation-error message for the specified expression.
      /// </summary>
      /// <typeparam name="TModel">The type of the model.</typeparam>
      /// <typeparam name="TProperty">The type of the property.</typeparam>
      /// <param name="htmlHelper">The HTML helper instance that this method operates on.</param>
      /// <param name="expression">An expression that identifies the object that contains the properties to render.
      /// </param>
      /// <param name="validationMessage">The message to display if a validation error occurs.</param>
      /// <param name="htmlAttributes">An <see cref="IDictionary{TKey,TValue}"/> that contains the HTML attributes
      /// for the element.</param>
      /// <param name="tag">The tag to be set for the wrapping HTML element of the validation message.</param>
      /// <returns>null if the model object is valid and client-side validation is disabled.
      /// Otherwise, a <paramref name="tag"/> element that contains an error message.</returns>
      [SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures", Justification = "This is an appropriate nesting of generic types")]
      public static void ValidationMessageFor<TModel, TProperty>(this HtmlHelper<TModel> htmlHelper,
                                                                 DynamicContext context,
                                                                 Expression<Func<TModel, TProperty>> expression,
                                                                 string validationMessage = null,
                                                                 IDictionary<string, object> htmlAttributes = null,
                                                                 string tag = null) {

         ModelMetadata metadata = ModelMetadata.FromLambdaExpression(expression, htmlHelper.ViewData);
         string expressionString = ExpressionHelper.GetExpressionText(expression);

         ValidationMessageHelper(htmlHelper, context, metadata, expressionString, validationMessage, htmlAttributes, tag);
      }

      [SuppressMessage("Microsoft.Globalization", "CA1308:NormalizeStringsToUppercase", Justification = "Normalization to lowercase is a common requirement for JavaScript and HTML values")]
      internal static void ValidationMessageHelper(this HtmlHelper htmlHelper,
                                                   DynamicContext context,
                                                   ModelMetadata modelMetadata,
                                                   string expression,
                                                   string validationMessage,
                                                   IDictionary<string, object> htmlAttributes,
                                                   string tag) {

         string modelName = htmlHelper.ViewData.TemplateInfo.GetFullHtmlFieldName(expression);
         FormContext formContext = htmlHelper.ViewContext.GetFormContextForClientValidation();

         if (!htmlHelper.ViewData.ModelState.ContainsKey(modelName)
            && formContext == null) {

            return;
         }

         ModelState modelState = htmlHelper.ViewData.ModelState[modelName];
         ModelErrorCollection modelErrors = (modelState == null) ? null : modelState.Errors;
         ModelError modelError = (((modelErrors == null) || (modelErrors.Count == 0)) ? null : modelErrors.FirstOrDefault(m => !String.IsNullOrEmpty(m.ErrorMessage)) ?? modelErrors[0]);

         if (modelError == null
            && formContext == null) {

            return;
         }

         if (String.IsNullOrEmpty(tag)) {
            tag = htmlHelper.ViewContext.ValidationMessageElement;
         }

         XcstWriter output = context.Output;

         output.WriteStartElement(tag);

         var attribs = HtmlAttributesMerger.Create(htmlAttributes)
            .AddCssClass((modelError != null) ? HtmlHelper.ValidationMessageCssClassName : HtmlHelper.ValidationMessageValidCssClassName);

         if (formContext != null) {

            bool replaceValidationMessageContents = String.IsNullOrEmpty(validationMessage);

            if (htmlHelper.ViewContext.UnobtrusiveJavaScriptEnabled) {
               attribs.AddDontReplace("data-valmsg-for", modelName);
               attribs.AddDontReplace("data-valmsg-replace", replaceValidationMessageContents.ToString().ToLowerInvariant());
            } else {
               FieldValidationMetadata fieldMetadata = ApplyFieldValidationMetadata(htmlHelper, modelMetadata, modelName);
               // rules will already have been written to the metadata object
               fieldMetadata.ReplaceValidationMessageContents = replaceValidationMessageContents; // only replace contents if no explicit message was specified

               // client validation always requires an ID
               attribs.GenerateId(modelName + "_validationMessage");
               fieldMetadata.ValidationMessageId = attribs.Attributes["id"].ToString();
            }
         }

         attribs.WriteTo(output);

         if (!String.IsNullOrEmpty(validationMessage)) {
            output.WriteString(validationMessage);
         } else if (modelError != null) {
            output.WriteString(GetUserErrorMessageOrDefault(htmlHelper.ViewContext.HttpContext, modelError, modelState));
         }

         output.WriteEndElement();
      }

      public static void ValidationSummary(this HtmlHelper htmlHelper,
                                           DynamicContext context,
                                           bool excludePropertyErrors = false,
                                           string message = null,
                                           IDictionary<string, object> htmlAttributes = null,
                                           string headingTag = null) {

         if (htmlHelper == null) throw new ArgumentNullException(nameof(htmlHelper));

         FormContext formContext = htmlHelper.ViewContext.GetFormContextForClientValidation();

         if (htmlHelper.ViewData.ModelState.IsValid) {

            if (formContext == null) {
               // No client side validation
               return;
            }

            // TODO: This isn't really about unobtrusive; can we fix up non-unobtrusive to get rid of this, too?
            if (htmlHelper.ViewContext.UnobtrusiveJavaScriptEnabled
               && excludePropertyErrors) {

               // No client-side updates
               return;
            }
         }

         XcstWriter output = context.Output;

         output.WriteStartElement("div");

         var divAttribs = HtmlAttributesMerger.Create(htmlAttributes)
            .AddCssClass((htmlHelper.ViewData.ModelState.IsValid) ? HtmlHelper.ValidationSummaryValidCssClassName : HtmlHelper.ValidationSummaryCssClassName);

         if (formContext != null) {

            if (htmlHelper.ViewContext.UnobtrusiveJavaScriptEnabled) {

               if (!excludePropertyErrors) {
                  // Only put errors in the validation summary if they're supposed to be included there
                  divAttribs.AddDontReplace("data-valmsg-summary", "true");
               }

            } else {
               // client val summaries need an ID
               divAttribs.GenerateId("validationSummary");
               formContext.ValidationSummaryId = divAttribs.Attributes["id"].ToString();
               formContext.ReplaceValidationSummary = !excludePropertyErrors;
            }
         }

         divAttribs.WriteTo(output);

         if (!String.IsNullOrEmpty(message)) {

            if (String.IsNullOrEmpty(headingTag)) {
               headingTag = htmlHelper.ViewContext.ValidationSummaryMessageElement;
            }

            output.WriteStartElement(headingTag);
            output.WriteString(message);
            output.WriteEndElement();
         }

         output.WriteStartElement("ul");

         bool empty = true;

         IEnumerable<ModelState> modelStates = GetModelStateList(htmlHelper, excludePropertyErrors);

         foreach (ModelState modelState in modelStates) {

            foreach (ModelError modelError in modelState.Errors) {

               string errorText = GetUserErrorMessageOrDefault(htmlHelper.ViewContext.HttpContext, modelError, null /* modelState */);

               if (!String.IsNullOrEmpty(errorText)) {

                  empty = false;

                  output.WriteStartElement("li");
                  output.WriteString(errorText);
                  output.WriteEndElement();
               }
            }
         }

         if (empty) {
            output.WriteStartElement("li");
            output.WriteAttributeString("style", "display:none");
            output.WriteEndElement();
         }

         output.WriteEndElement(); // </ul>
         output.WriteEndElement(); // </div>
      }

      // Returns non-null list of model states, which caller will render in order provided.
      static IEnumerable<ModelState> GetModelStateList(HtmlHelper htmlHelper, bool excludePropertyErrors) {

         if (excludePropertyErrors) {

            ModelState ms;
            htmlHelper.ViewData.ModelState.TryGetValue(htmlHelper.ViewData.TemplateInfo.HtmlFieldPrefix, out ms);

            if (ms != null) {
               return new ModelState[] { ms };
            }

            return new ModelState[0];

         } else {

            // Sort modelStates to respect the ordering in the metadata.                 
            // ModelState doesn't refer to ModelMetadata, but we can correlate via the property name.
            Dictionary<string, int> ordering = new Dictionary<string, int>();

            var metadata = htmlHelper.ViewData.ModelMetadata;

            if (metadata != null) {
               foreach (ModelMetadata m in metadata.Properties) {
                  ordering[m.PropertyName] = m.Order;
               }
            }

            return from kv in htmlHelper.ViewData.ModelState
                   let name = kv.Key
                   orderby ordering.GetOrDefault(name, ModelMetadata.DefaultOrder)
                   select kv.Value;
         }
      }
   }
}
