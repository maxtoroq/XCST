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
using System.IO;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;

namespace Xcst.Web.Mvc {

   public class XcstViewPageHttpHandler : XcstPageHttpHandler
#if !ASPNETLIB
      , IView 
#endif
      {

      readonly XcstViewPage page;

      public bool DisableRequestValidation { get; set; }

      public XcstViewPageHttpHandler(XcstViewPage page)
         : base(page) {

         this.page = page;
      }

      internal static XcstViewPageHttpHandler Create(object instance) {

         XcstViewPage page = instance as XcstViewPage;

         if (page != null) {
            return new XcstViewPageHttpHandler(page);
         }

         return null;
      }

      protected override void InitializePage(XcstPage page, HttpContextBase context) {

         base.InitializePage(page, context);

         RequestContext requestContext = context.Request.RequestContext
            ?? new RequestContext(context, new RouteData());

         if (!requestContext.RouteData.Values.ContainsKey("controller")) {

            // routeData must contain an item named 'controller' with a non-empty string value
            // required by view engine

            string controllerTypeName = nameof(XcstViewPageController);

            requestContext.RouteData.Values["controller"] =
               controllerTypeName.Substring(0, controllerTypeName.Length - "Controller".Length);
         }

         var controller = new XcstViewPageController();

         ControllerContext controllerContext;
         TempDataDictionary tempData;

#if ASPNETLIB
         controllerContext = new ControllerContext(requestContext);
         controllerContext.ValidateRequest = !this.DisableRequestValidation;

         tempData = new TempDataDictionary();
#else
         // page's ViewData can depend on runtime type (TModel)
         // since data is not coming from controller we can let page create it
         controller.ViewData = this.page.ViewData;
         controller.ValidateRequest = !this.DisableRequestValidation;

         controller.Init(requestContext);

         controllerContext = controller.ControllerContext;
         tempData = controller.TempData;
#endif

         this.page.ViewContext = new ViewContext(
            controllerContext,
#if !ASPNETLIB
            this,
#endif
            this.page.ViewData, tempData, context.Response.Output);
      }

      protected override void RenderPage(XcstPage page, HttpContextBase context) {

         ITempDataProvider tempDataProvider =
#if ASPNETLIB
            this.page.ViewContext.TempDataProvider;
#else
            (this.page.ViewContext.Controller as Controller)?.TempDataProvider;
#endif

         PossiblyLoadTempData(tempDataProvider);

         try {
            base.RenderPage(page, context);
         } finally {
            PossiblySaveTempData(tempDataProvider);
         }
      }

      void PossiblyLoadTempData(ITempDataProvider tempDataProvider) {

         if (!this.page.ViewContext.IsChildAction) {
            this.page.TempData.Load(this.page.ViewContext, tempDataProvider);
         }
      }

      void PossiblySaveTempData(ITempDataProvider tempDataProvider) {

         if (!this.page.ViewContext.IsChildAction) {
            this.page.TempData.Save(this.page.ViewContext, tempDataProvider);
         }
      }

#if !ASPNETLIB
      void IView.Render(ViewContext viewContext, TextWriter writer) {

         ViewContext oldViewContext = this.page.ViewContext;

         this.page.ViewContext = viewContext;

         try {
            XcstView.RenderPage(viewContext, writer, this.page);

         } finally {
            this.page.ViewContext = oldViewContext;
         }
      }
#endif

      class XcstViewPageController
#if !ASPNETLIB
         : Controller
#endif
      {

#if !ASPNETLIB
         internal void Init(RequestContext requestContext) {
            Initialize(requestContext);
         }
#endif
      }
   }
}
