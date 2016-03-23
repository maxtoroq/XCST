// Copyright 2016 Max Toro Q.
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
using System.Linq.Expressions;
using System.Web.Mvc;

namespace Xcst.Web.Mvc {

   public class ModelHelper<TModel> : ModelHelper {

      public new HtmlHelper<TModel> Html { get; }

      public new TModel Model => Html.ViewData.Model;

      public ModelHelper(HtmlHelper<TModel> htmlHelper)
         : base(htmlHelper) {

         if (htmlHelper == null) throw new ArgumentNullException(nameof(htmlHelper));

         this.Html = htmlHelper;
      }

      public string DisplayName<TProperty>(Expression<Func<TModel, TProperty>> expression) {

         ModelMetadata metadata;

         if (typeof(IEnumerable<TModel>).IsAssignableFrom(typeof(TModel))) {
            metadata = ModelMetadata.FromLambdaExpression(expression, new ViewDataDictionary<TModel>());
         } else {
            metadata = ModelMetadata.FromLambdaExpression(expression, this.Html.ViewData);
         }

         string expressionString = ExpressionHelper.GetExpressionText(expression);

         return DisplayNameHelper(metadata, expressionString);
      }

      public string FieldId<TProperty>(Expression<Func<TModel, TProperty>> expression) {
         return FieldId(ExpressionHelper.GetExpressionText(expression));
      }

      public string FieldName<TProperty>(Expression<Func<TModel, TProperty>> expression) {
         return FieldName(ExpressionHelper.GetExpressionText(expression));
      }

      public string FieldValue<TProperty>(Expression<Func<TModel, TProperty>> expression) {

         ModelMetadata metadata = ModelMetadata.FromLambdaExpression(expression, this.Html.ViewData);

         string expressionString = ExpressionHelper.GetExpressionText(expression);

         return FieldValueHelper(expressionString, metadata.Model, format: metadata.EditFormatString, useViewData: false);
      }

      public string FieldValue<TProperty>(Expression<Func<TModel, TProperty>> expression, string format) {

         ModelMetadata metadata = ModelMetadata.FromLambdaExpression(expression, this.Html.ViewData);

         string expressionString = ExpressionHelper.GetExpressionText(expression);

         return FieldValueHelper(expressionString, metadata.Model, format, useViewData: false);
      }
   }
}
