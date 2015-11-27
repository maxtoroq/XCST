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

namespace Xcst.Web.Mvc.Runtime {

   /// <exclude/>
   public static class ModelUpdater {

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
   }
}
