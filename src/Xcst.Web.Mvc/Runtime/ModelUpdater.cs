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
using System.Linq;
using System.Web.Mvc;

namespace Xcst.Web.Mvc.Runtime {

   /// <exclude/>
   public class ModelUpdater {

      readonly ControllerContext controllerContext;
      readonly IViewDataContainer viewDataContainer;

      private ModelStateDictionary ModelState => viewDataContainer.ViewData.ModelState;

      public static ModelUpdater Create<TModel>(HtmlHelper<TModel> htmlHelper, bool createModelIfNull = false) {

         if (htmlHelper == null) throw new ArgumentNullException(nameof(htmlHelper));

         if (createModelIfNull
            && htmlHelper.ViewData.Model == null) {

            SetModel(htmlHelper, Activator.CreateInstance<TModel>());
         }

         return new ModelUpdater(htmlHelper.ViewContext, htmlHelper.ViewDataContainer);
      }

      public static void SetModel<TModel>(HtmlHelper<TModel> htmlHelper, TModel value) {

         if (htmlHelper == null) throw new ArgumentNullException(nameof(htmlHelper));

         // HtmlHelper<TModel> creates a copy of ViewData, but keeps original on ViewDataContainer.ViewData
         htmlHelper.ViewDataContainer.ViewData.Model =

            // Setting ViewDataDictionary.Model resets ModelMetadata back to null
            // ViewDataDictionary<TModel>.Model doesn't, that's why we cast it
            ((ViewDataDictionary)htmlHelper.ViewData).Model =

               // HtmlHelper<TModel>.ViewData provides type safety
               htmlHelper.ViewData.Model = value;
      }

      public ModelUpdater(ControllerContext controllerContext, IViewDataContainer viewDataContainer) {

         if (controllerContext == null) throw new ArgumentNullException(nameof(controllerContext));
         if (viewDataContainer == null) throw new ArgumentNullException(nameof(viewDataContainer));

         this.controllerContext = controllerContext;
         this.viewDataContainer = viewDataContainer;
      }

      public bool TryUpdate(object value, Type type = null, string prefix = null, string[] includeProperties = null, string[] excludeProperties = null, IValueProvider valueProvider = null) {

         if (value == null) throw new ArgumentNullException(nameof(value));

         if (type == null) {
            type = value.GetType();
         }

         if (valueProvider == null) {
            valueProvider = this.controllerContext.Controller?.ValueProvider;
         }

         var bindingContext = new ModelBindingContext {
            ModelMetadata = ModelMetadataProviders.Current.GetMetadataForType(() => value, type),
            ModelName = prefix,
            ModelState = this.ModelState,
            PropertyFilter = p => IsPropertyAllowed(p, includeProperties, excludeProperties),
            ValueProvider = valueProvider
         };

         IModelBinder binder = ModelBinders.Binders.GetBinder(type);

         binder.BindModel(this.controllerContext, bindingContext);

         return this.ModelState.IsValid;
      }

      public bool TryValidate(object value, string prefix = null) {

         if (value == null) throw new ArgumentNullException(nameof(value));

         ModelMetadata metadata = ModelMetadataProviders.Current.GetMetadataForType(() => value, value.GetType());

         foreach (ModelValidationResult validationResult in ModelValidator.GetModelValidator(metadata, this.controllerContext).Validate(null)) {
            this.ModelState.AddModelError(CreateSubPropertyName(prefix, validationResult.MemberName), validationResult.Message);
         }

         return this.ModelState.IsValid;
      }

      static bool IsPropertyAllowed(string propertyName, string[] includeProperties, string[] excludeProperties) {
         // We allow a property to be bound if its both in the include list AND not in the exclude list.
         // An empty include list implies all properties are allowed.
         // An empty exclude list implies no properties are disallowed.
         bool includeProperty = (includeProperties == null) || (includeProperties.Length == 0) || includeProperties.Contains(propertyName, StringComparer.OrdinalIgnoreCase);
         bool excludeProperty = (excludeProperties != null) && excludeProperties.Contains(propertyName, StringComparer.OrdinalIgnoreCase);
         return includeProperty && !excludeProperty;
      }

      static string CreateSubPropertyName(string prefix, string propertyName) {

         if (String.IsNullOrEmpty(prefix)) {
            return propertyName;
         }

         if (String.IsNullOrEmpty(propertyName)) {
            return prefix;
         }

         return (prefix + "." + propertyName);
      }
   }
}
