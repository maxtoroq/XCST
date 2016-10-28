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

#region SelectExtensions is based on code from ASP.NET Web Stack
// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.
#endregion

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Web.Mvc;
using Xcst.Runtime;
using EnumHelper = System.Web.Mvc.Html.EnumHelper;

namespace Xcst.Web.Mvc.Html {

   /// <exclude/>
   public static class SelectExtensions {

      // DropDownList

      public static void DropDownList(this HtmlHelper htmlHelper,
                                      DynamicContext context,
                                      string name,
                                      IEnumerable<SelectListItem> selectList = null,
                                      string optionLabel = null,
                                      IDictionary<string, object> htmlAttributes = null) {

         DropDownListHelper(htmlHelper, context, default(ModelMetadata), name, selectList, optionLabel, htmlAttributes);
      }

      [SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters", Justification = "Users cannot use anonymous methods with the LambdaExpression type")]
      [SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures", Justification = "This is an appropriate nesting of generic types")]
      public static void DropDownListFor<TModel, TProperty>(this HtmlHelper<TModel> htmlHelper,
                                                            DynamicContext context,
                                                            Expression<Func<TModel, TProperty>> expression,
                                                            IEnumerable<SelectListItem> selectList = null,
                                                            string optionLabel = null,
                                                            IDictionary<string, object> htmlAttributes = null) {

         if (expression == null) throw new ArgumentNullException(nameof(expression));

         ModelMetadata metadata = ModelMetadata.FromLambdaExpression(expression, htmlHelper.ViewData);
         string expressionString = ExpressionHelper.GetExpressionText(expression);

         DropDownListHelper(htmlHelper, context, metadata, expressionString, selectList, optionLabel, htmlAttributes);
      }

      // Unable to constrain TEnum.  Cannot include IComparable, IConvertible, IFormattable because Nullable<T> does
      // not implement those interfaces (and Int32 does).  Enum alone is not compatible with expression restrictions
      // because that requires a cast from all enum types.  And the struct generic constraint disallows passing a
      // Nullable<T> expression.

      [SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures", Justification = "This is an appropriate nesting of generic types")]
      public static void EnumDropDownListFor<TModel, TEnum>(this HtmlHelper<TModel> htmlHelper,
                                                            DynamicContext context,
                                                            Expression<Func<TModel, TEnum>> expression,
                                                            string optionLabel = null,
                                                            IDictionary<string, object> htmlAttributes = null) {

         if (expression == null) throw new ArgumentNullException(nameof(expression));

         ModelMetadata metadata = ModelMetadata.FromLambdaExpression(expression, htmlHelper.ViewData);

         if (metadata == null) {
            throw new ArgumentException($"Unable to determine ModelMetadata for expression '{expression.ToString()}'.", nameof(expression));
         }

         if (metadata.ModelType == null) {
            throw new ArgumentException($"Unable to determine type of expression '{expression.ToString()}'.", nameof(expression));
         }

         if (!EnumHelper.IsValidForEnumHelper(metadata.ModelType)) {

            string formatString = HasFlags(metadata.ModelType) ?
               "Return type '{0}' is not supported. Type must not have a '{1}' attribute."
               : "Return type '{0}' is not supported.";

            throw new ArgumentException(String.Format(CultureInfo.InvariantCulture, formatString, metadata.ModelType.FullName, "Flags"));
         }

         // Run through same processing as SelectInternal() to determine selected value and ensure it is included
         // in the select list.
         string expressionName = ExpressionHelper.GetExpressionText(expression);
         string expressionFullName =
             htmlHelper.ViewData.TemplateInfo.GetFullHtmlFieldName(expressionName);

         Enum currentValue = null;

         if (!String.IsNullOrEmpty(expressionFullName)) {
            currentValue = htmlHelper.GetModelStateValue(expressionFullName, metadata.ModelType) as Enum;
         }

         if (currentValue == null && !String.IsNullOrEmpty(expressionName)) {
            // Ignore any select list (enumerable with this name) in the view data
            currentValue = htmlHelper.ViewData.Eval(expressionName) as Enum;
         }

         if (currentValue == null) {
            currentValue = metadata.Model as Enum;
         }

         IList<SelectListItem> selectList = EnumHelper.GetSelectList(metadata.ModelType, currentValue);

         if (!String.IsNullOrEmpty(optionLabel)
            && selectList.Count != 0
            && String.IsNullOrEmpty(selectList[0].Text)) {

            // Were given an optionLabel and the select list has a blank initial slot.  Combine.
            selectList[0].Text = optionLabel;

            // Use the option label just once; don't pass it down the lower-level helpers.
            optionLabel = null;
         }

         DropDownListHelper(htmlHelper, context, metadata, expressionName, selectList, optionLabel, htmlAttributes);
      }

      static bool HasFlags(Type type) {

         Type checkedType = Nullable.GetUnderlyingType(type) ?? type;

         return HasFlagsInternal(checkedType);
      }

      static bool HasFlagsInternal(Type type) {

         FlagsAttribute attribute = type.GetCustomAttribute<FlagsAttribute>(inherit: false);
         return attribute != null;
      }

      internal static void DropDownListHelper(HtmlHelper htmlHelper,
                                              DynamicContext context,
                                              ModelMetadata metadata,
                                              string expression,
                                              IEnumerable<SelectListItem> selectList,
                                              string optionLabel,
                                              IDictionary<string, object> htmlAttributes) {

         SelectInternal(htmlHelper, context, metadata, optionLabel, expression, selectList, allowMultiple: false, htmlAttributes: htmlAttributes);
      }

      // ListBox

      public static void ListBox(this HtmlHelper htmlHelper,
                                 DynamicContext context,
                                 string name,
                                 IEnumerable<SelectListItem> selectList = null,
                                 IDictionary<string, object> htmlAttributes = null) {

         ListBoxHelper(htmlHelper, context, default(ModelMetadata), name, selectList, htmlAttributes);
      }

      [SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters", Justification = "Users cannot use anonymous methods with the LambdaExpression type")]
      [SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures", Justification = "This is an appropriate nesting of generic types")]
      public static void ListBoxFor<TModel, TProperty>(this HtmlHelper<TModel> htmlHelper,
                                                       DynamicContext context,
                                                       Expression<Func<TModel, TProperty>> expression,
                                                       IEnumerable<SelectListItem> selectList,
                                                       IDictionary<string, object> htmlAttributes) {

         if (expression == null) throw new ArgumentNullException(nameof(expression));

         ModelMetadata metadata = ModelMetadata.FromLambdaExpression(expression, htmlHelper.ViewData);
         string expressionString = ExpressionHelper.GetExpressionText(expression);

         ListBoxHelper(htmlHelper, context, metadata, expressionString, selectList, htmlAttributes);
      }

      static void ListBoxHelper(HtmlHelper htmlHelper,
                                DynamicContext context,
                                ModelMetadata metadata,
                                string name,
                                IEnumerable<SelectListItem> selectList,
                                IDictionary<string, object> htmlAttributes) {

         SelectInternal(htmlHelper, context, metadata, optionLabel: null, name: name, selectList: selectList, allowMultiple: true, htmlAttributes: htmlAttributes);
      }

      // Helper methods

      static IEnumerable<SelectListItem> GetSelectData(this HtmlHelper htmlHelper, string name) {

         object o = null;

         if (htmlHelper.ViewData != null) {
            o = htmlHelper.ViewData.Eval(name);
         }

         if (o == null) {
            throw new InvalidOperationException($"There is no ViewData item of type 'IEnumerable<SelectListItem>' that has the key '{name}'.");
         }

         IEnumerable<SelectListItem> selectList = o as IEnumerable<SelectListItem>;

         if (selectList == null) {
            throw new InvalidOperationException($"The ViewData item that has the key '{name}' is of type '{o.GetType().FullName}' but must be of type 'IEnumerable<SelectListItem>'.");
         }

         return selectList;
      }

      static IEnumerable<SelectListItem> GetSelectListWithDefaultValue(IEnumerable<SelectListItem> selectList, object defaultValue, bool allowMultiple) {

         IEnumerable defaultValues;

         if (allowMultiple) {

            defaultValues = defaultValue as IEnumerable;

            if (defaultValues == null || defaultValues is string) {
               throw new InvalidOperationException("The parameter 'expression' must evaluate to an IEnumerable when multiple selection is allowed.");
            }
         } else {
            defaultValues = new[] { defaultValue };
         }

         IEnumerable<string> values = from object value in defaultValues
                                      select Convert.ToString(value, CultureInfo.CurrentCulture);

         // ToString() by default returns an enum value's name.  But selectList may use numeric values.
         IEnumerable<string> enumValues = from Enum value in defaultValues.OfType<Enum>()
                                          select value.ToString("d");

         values = values.Concat(enumValues);

         var selectedValues = new HashSet<string>(values, StringComparer.OrdinalIgnoreCase);
         var newSelectList = new List<SelectListItem>();

         foreach (SelectListItem item in selectList) {
            item.Selected = (item.Value != null) ? selectedValues.Contains(item.Value) : selectedValues.Contains(item.Text);
            newSelectList.Add(item);
         }

         return newSelectList;
      }

      static void SelectInternal(this HtmlHelper htmlHelper,
                                 DynamicContext context,
                                 ModelMetadata metadata,
                                 string optionLabel,
                                 string name,
                                 IEnumerable<SelectListItem> selectList,
                                 bool allowMultiple,
                                 IDictionary<string, object> htmlAttributes) {

         string fullName = htmlHelper.ViewData.TemplateInfo.GetFullHtmlFieldName(name);

         if (String.IsNullOrEmpty(fullName)) {
            throw new ArgumentNullException(nameof(name));
         }

         bool usedViewData = false;

         // If we got a null selectList, try to use ViewData to get the list of items.

         if (selectList == null) {
            selectList = htmlHelper.GetSelectData(name);
            usedViewData = true;
         }

         object defaultValue = (allowMultiple) ?
            htmlHelper.GetModelStateValue(fullName, typeof(string[]))
            : htmlHelper.GetModelStateValue(fullName, typeof(string));

         // If we haven't already used ViewData to get the entire list of items then we need to
         // use the ViewData-supplied value before using the parameter-supplied value.

         if (defaultValue == null) {

            if (metadata == null) {

               if (!usedViewData
                  && !String.IsNullOrEmpty(name)) {

                  defaultValue = htmlHelper.ViewData.Eval(name);
               }

            } else {
               defaultValue = metadata.Model;
            }
         }

         if (defaultValue != null) {
            selectList = GetSelectListWithDefaultValue(selectList, defaultValue, allowMultiple);
         }

         XcstWriter output = context.Output;

         output.WriteStartElement("select");

         var attribs = HtmlAttributesMerger.Create(htmlAttributes)
            .AddReplace("name", fullName)
            .GenerateId(fullName);

         if (allowMultiple) {
            attribs.AddDontReplace("multiple", "multiple");
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

         // Convert each ListItem to an <option> tag and wrap them with <optgroup> if requested.
         BuildItems(output, optionLabel, selectList);

         output.WriteEndElement(); // </select>
      }

      static void BuildItems(XcstWriter output, string optionLabel, IEnumerable<SelectListItem> selectList) {

         // Make optionLabel the first item that gets rendered.
         if (optionLabel != null) {
            ListItemToOption(output, new SelectListItem() {
               Text = optionLabel,
               Value = String.Empty,
               Selected = false
            });
         }

         // Group items in the SelectList if requested.
         // Treat each item with Group == null as a member of a unique group
         // so they are added according to the original order.
         IEnumerable<IGrouping<int, SelectListItem>> groupedSelectList = selectList.GroupBy<SelectListItem, int>(
             i => (i.Group == null) ? i.GetHashCode() : i.Group.GetHashCode());

         foreach (IGrouping<int, SelectListItem> group in groupedSelectList) {

            SelectListGroup optGroup = group.First().Group;

            if (optGroup != null) {

               output.WriteStartElement("optgroup");

               if (optGroup.Name != null) {
                  output.WriteAttributeString("label", optGroup.Name);
               }

               if (optGroup.Disabled) {
                  output.WriteAttributeString("disabled", "disabled");
               }
            }

            foreach (SelectListItem item in group) {
               ListItemToOption(output, item);
            }

            if (optGroup != null) {
               output.WriteEndElement(); // </optgroup>
            }
         }
      }

      internal static void ListItemToOption(XcstWriter output, SelectListItem item) {

         output.WriteStartElement("option");

         if (item.Value != null) {
            output.WriteAttributeString("value", item.Value);
         }

         if (item.Selected) {
            output.WriteAttributeString("selected", "selected");
         }

         if (item.Disabled) {
            output.WriteAttributeString("disabled", "disabled");
         }

         output.WriteString(item.Text);
         output.WriteEndElement();
      }
   }
}
