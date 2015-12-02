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

#region DisplayTextExtensions is based on code from ASP.NET Web Stack
// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.
#endregion

using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Web.Mvc;
using System.Xml;

namespace Xcst.Web.Mvc.Html {

   /// <exclude/>
   public static class DisplayTextExtensions {

      public static void DisplayText(this HtmlHelper html, XmlWriter output, string name) {
         DisplayTextHelper(html, output, ModelMetadata.FromStringExpression(name, html.ViewContext.ViewData));
      }

      [SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures", Justification = "This is an appropriate nesting of generic types")]
      public static void DisplayTextFor<TModel, TResult>(this HtmlHelper<TModel> html, XmlWriter output, Expression<Func<TModel, TResult>> expression) {
         DisplayTextHelper(html, output, ModelMetadata.FromLambdaExpression(expression, html.ViewData));
      }

      internal static void DisplayTextHelper(HtmlHelper html, XmlWriter output, ModelMetadata metadata) {

         string text = metadata.SimpleDisplayText;

         if (metadata.HtmlEncode) {
            output.WriteString(text);
         } else {
            output.WriteRaw(text);
         }
      }

      public static string DisplayString(this HtmlHelper html, string name) {
         return DisplayStringHelper(html, ModelMetadata.FromStringExpression(name, html.ViewContext.ViewData));
      }

      [SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures", Justification = "This is an appropriate nesting of generic types")]
      public static string DisplayStringFor<TModel, TResult>(this HtmlHelper<TModel> html, Expression<Func<TModel, TResult>> expression) {
         return DisplayStringHelper(html, ModelMetadata.FromLambdaExpression(expression, html.ViewData));
      }

      internal static string DisplayStringHelper(HtmlHelper html, ModelMetadata metadata) {

         string text = metadata.SimpleDisplayText;

         return text;
      }
   }
}
