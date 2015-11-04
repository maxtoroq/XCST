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

      public dynamic ViewBag => ViewContext?.ViewBag;

      public object Model => ViewData.Model;

      public virtual UrlHelper Url {
         get {
            if (_Url == null) {
               _Url = (ViewContext?.Controller as Controller)?.Url
                  ?? new UrlHelper(ViewContext?.RequestContext ?? Request.RequestContext);
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

      public ModelStateDictionary ModelState => ViewData.ModelState;

      public TempDataDictionary TempData => ViewContext?.TempData;

      internal virtual void SetViewData(ViewDataDictionary viewData) {

         _ViewData = viewData;

         // HtmlHelper<TModel> creates a copy of ViewData
         this.Html = null;
      }
   }
}
