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
using System.ComponentModel;
using System.Linq;
using System.Web.Mvc;

namespace Xcst.Web.Mvc {

   public abstract class XcstViewPage : XcstPage, IViewDataContainer {

      ViewContext _ViewContext;
      ViewDataDictionary _ViewData;
      UrlHelper _Url;
      HtmlHelper<object> _Html;

      public virtual ViewContext ViewContext {
         get { return _ViewContext; }
         set {
            _ViewContext = value;

            Context = value?.HttpContext;
            ViewData = value?.ViewData;
            Url = null;
            Html = null;
         }
      }

      public ViewDataDictionary ViewData {
         get {
            if (_ViewData == null) {
               SetViewData(new ViewDataDictionary());
            }
            return _ViewData;
         }
         set { SetViewData(value); }
      }

      public object Model {
         get { return ViewData.Model; }
         set { ViewData.Model = value; }
      }

      public virtual UrlHelper Url {
         get {
            if (_Url == null
               && ViewContext != null) {

               _Url = (ViewContext.Controller as Controller)?.Url
                  ?? new UrlHelper(ViewContext.RequestContext);
            }
            return _Url;
         }
         set { _Url = value; }
      }

      public HtmlHelper<object> Html {
         get {
            if (_Html == null
               && ViewContext != null) {
               _Html = new HtmlHelper<object>(ViewContext, this);
            }
            return _Html;
         }
         set { _Html = value; }
      }

      public dynamic ViewBag => ViewContext?.ViewBag;

      public ModelStateDictionary ModelState => ViewData.ModelState;

      public TempDataDictionary TempData => ViewContext?.TempData;

      internal IValueProvider ValueProvider => ViewContext?.Controller.ValueProvider;

      [EditorBrowsable(EditorBrowsableState.Never)]
      internal virtual void SetViewData(ViewDataDictionary viewData) {
         _ViewData = viewData;
      }

      public bool TryUpdate(object value, Type type = null, string prefix = null, string[] includeProperties = null, string[] excludeProperties = null, IValueProvider valueProvider = null) {

         if (value == null) throw new ArgumentNullException(nameof(value));

         if (type == null) {
            type = value.GetType();
         }

         var bindingContext = new ModelBindingContext {
            ModelMetadata = ModelMetadataProviders.Current.GetMetadataForType(() => value, type),
            ModelName = prefix,
            ModelState = this.ModelState,
            PropertyFilter = p => IsPropertyAllowed(p, includeProperties, excludeProperties),
            ValueProvider = valueProvider ?? this.ValueProvider
         };

         IModelBinder binder = ModelBinders.Binders.GetBinder(type);

         binder.BindModel(this.ViewContext, bindingContext);

         return this.ModelState.IsValid;
      }

      public bool TryUpdateModel(Type type = null, string prefix = null, string[] includeProperties = null, string[] excludeProperties = null, IValueProvider valueProvider = null) {

         Type modelType = EnsureModel();

         return TryUpdate(
            this.Model,
            type: type ?? modelType,
            prefix: prefix,
            includeProperties: includeProperties,
            excludeProperties: excludeProperties,
            valueProvider: valueProvider
         );
      }

      public bool TryValidate(object value, string prefix = null) {

         if (value == null) throw new ArgumentNullException(nameof(value));

         ModelMetadata metadata = ModelMetadataProviders.Current.GetMetadataForType(() => value, value.GetType());

         foreach (ModelValidationResult validationResult in ModelValidator.GetModelValidator(metadata, this.ViewContext).Validate(null)) {
            this.ModelState.AddModelError(CreateSubPropertyName(prefix, validationResult.MemberName), validationResult.Message);
         }

         return this.ModelState.IsValid;
      }

      public bool TryValidateModel(string prefix = null) {

         EnsureModel();

         return TryValidate(this.Model, prefix: prefix);
      }

      internal virtual Type EnsureModel() {

         object model = this.Model;

         if (model == null) {
            throw new InvalidOperationException($"{nameof(Model)} cannot be null.");
         }

         return model.GetType();
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
