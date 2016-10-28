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
using System.Web;
using System.Web.Compilation;
using System.Web.SessionState;

namespace Xcst.Web {

   public class XcstPageHttpHandler : IHttpHandler, IRequiresSessionState {

      readonly XcstPage page;

      public bool IsReusable => false;

      internal static XcstPageHttpHandler Create(object instance) {

         XcstPage page = instance as XcstPage;

         if (page != null) {
            return new XcstPageHttpHandler(page);
         }

         return null;
      }

      public static IHttpHandler CreateFromVirtualPath(string virtualPath) {

         var instance = BuildManager.CreateInstanceFromVirtualPath(virtualPath, typeof(object));

         if (instance == null) {
            return null;
         }

         XcstPage page = instance as XcstPage;

         if (page != null) {
            page.VirtualPath = virtualPath;
         }

         return XcstWebConfiguration.Instance
            .HttpHandlerFactories
            .Select(f => f(instance))
            .Where(p => p != null)
            .FirstOrDefault()
            ?? instance as IHttpHandler;
      }

      public XcstPageHttpHandler(XcstPage page) {

         if (page == null) throw new ArgumentNullException(nameof(page));

         this.page = page;
      }

      public virtual void ProcessRequest(HttpContext context) {

         InitializePage(this.page, new HttpContextWrapper(context));
         AddFileDependencies(this.page, this.page.Response);
         RenderPage(this.page, this.page.Context);
      }

      protected virtual void InitializePage(XcstPage page, HttpContextBase context) {
         page.Context = context;
      }

      protected virtual void RenderPage(XcstPage page, HttpContextBase context) {

         XcstEvaluator.Using(page)
            .CallInitialTemplate()
            .OutputTo(context.Response.Output)
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
