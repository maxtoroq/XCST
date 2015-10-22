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

namespace Xcst.Web.Mvc {

   class XcstView : BuildManagerCompiledView {

      public XcstView(ControllerContext controllerContext, string viewPath)
         : base(controllerContext, viewPath) { }

      protected override void RenderView(ViewContext viewContext, TextWriter writer, object instance) {

         if (viewContext == null) throw new ArgumentNullException(nameof(viewContext));
         if (writer == null) throw new ArgumentNullException(nameof(writer));
         if (instance == null) throw new ArgumentNullException(nameof(instance));

         XcstViewPage viewPage = instance as XcstViewPage;

         if (viewPage == null) {
            throw new InvalidOperationException($"The view at '{ViewPath}' must derive from {nameof(XcstViewPage)}, or {nameof(XcstViewPage)}<TModel>.");
         }

         viewPage.ViewContext = viewContext;

         AddFileDependencies(instance, viewContext.HttpContext.Response);
         RenderPage(viewPage, viewContext, writer);
      }

      internal static void RenderPage(XcstViewPage viewPage, ViewContext viewContext, TextWriter writer) {

         XcstEvaluator evaluator = XcstEvaluator.Using(viewPage);

         foreach (var item in viewContext.ViewData) {
            evaluator.WithParam(item.Key, item.Value);
         }

         evaluator.CallInitialTemplate()
            .OutputTo(writer)
            .Run();
      }

      static void AddFileDependencies(object instance, HttpResponseBase response) {

         IFileDependent fileDependent = instance as IFileDependent;

         if (fileDependent == null) {
            return;
         }

         string[] files = fileDependent.FileDependencies;

         if (files != null) {
            response.AddFileDependencies(files);
         }
      }
   }
}
