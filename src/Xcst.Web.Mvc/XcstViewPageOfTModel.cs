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

   public abstract class XcstViewPage<TModel> : XcstViewPage {

      ViewDataDictionary<TModel> _ViewData;
      HtmlHelper<TModel> _Html;

      public new ViewDataDictionary<TModel> ViewData {
         get {
            if (_ViewData == null) {
               SetViewData(new ViewDataDictionary<TModel>());
            }
            return _ViewData;
         }
         set { SetViewData(value); }
      }

      public new TModel Model => ViewData.Model;

      public new HtmlHelper<TModel> Html {
         get {
            if (_Html == null
               && ViewContext != null) {
               _Html = new HtmlHelper<TModel>(ViewContext, this);
            }
            return _Html;
         }
         set { _Html = value; }
      }

      internal override void SetViewData(ViewDataDictionary viewData) {

         _ViewData = new ViewDataDictionary<TModel>(viewData);

         // HtmlHelper<TModel> creates a copy of ViewData
         this.Html = null;

         base.SetViewData(_ViewData);
      }
   }
}
