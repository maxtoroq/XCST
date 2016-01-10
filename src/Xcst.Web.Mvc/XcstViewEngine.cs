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

   public class XcstViewEngine : BuildManagerViewEngine {

      public XcstViewEngine() {

         const string fileExtension = XcstWebConfiguration.FileExtension;

         this.ViewLocationFormats = new[] { "~/Views/{1}/{0}." + fileExtension, "~/Views/Shared/{0}." + fileExtension };
         this.PartialViewLocationFormats = this.ViewLocationFormats;
      }

      protected override IView CreatePartialView(ControllerContext controllerContext, string partialPath) {
         return new XcstView(controllerContext, partialPath);
      }

      protected override IView CreateView(ControllerContext controllerContext, string viewPath, string masterPath) {
         return new XcstView(controllerContext, viewPath);
      }
   }
}
